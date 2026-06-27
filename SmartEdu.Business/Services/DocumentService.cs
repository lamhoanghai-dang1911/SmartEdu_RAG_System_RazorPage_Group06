using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2013.Word;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess.EntityModels;
using SmartEdu.DataAccess.Repositories;
using SmartEdu.RazorWeb.Data;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using EntityDocument = SmartEdu.DataAccess.EntityModels.Document;

namespace SmartEdu.Business.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IRepository<EntityDocument> _docRepo;
        private readonly IRepository<DocumentChunk> _chunkRepo;
        private readonly IRepository<StudentSubject> _studentSubjectRepo;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _uow;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDocumentNotificationService _notification;

        public DocumentService(
        IRepository<EntityDocument> docRepo,
        IRepository<DocumentChunk> chunkRepo,
        IRepository<StudentSubject> studentSubjectRepo,
        IHttpClientFactory httpFactory,
        IConfiguration configuration,
        IUnitOfWork uow,
        IServiceScopeFactory scopeFactory,
        IDocumentNotificationService notification)
        {
            _docRepo = docRepo;
            _chunkRepo = chunkRepo;
            _studentSubjectRepo = studentSubjectRepo;
            _httpFactory = httpFactory;
            _configuration = configuration;
            _uow = uow;
            _scopeFactory = scopeFactory;
            _notification = notification;
        }

        public async Task<IEnumerable<DocumentChunkDto>> GetChunksByDocumentIdAsync(int documentId)
        {
            var chunks = await _chunkRepo.GetAllAsync(c => c.DocumentId == documentId);
            var doc = await _docRepo.GetByIdAsync(documentId);
            var title = doc?.Title ?? string.Empty;

            const int chunkSize = 800;
            const double overlapFraction = 0.1;
            int overlap = (int)Math.Round(chunkSize * overlapFraction);
            int step = chunkSize - overlap;

            var ordered = chunks.OrderBy(c => c.ChunkIndex).ToList();
            int total = ordered.Count;

            return ordered.Select(c => new DocumentChunkDto
            {
                ChunkIndex = c.ChunkIndex,
                Content = c.Content,
                DocumentTitle = title,
                CharLength = c.Content?.Length ?? 0,
                TotalChunks = total,
                CharStart = c.ChunkIndex * step,
                CharEnd = c.ChunkIndex * step + (c.Content?.Length ?? 0),
                OverlapSize = overlap
            });
        }

        public async Task<IEnumerable<DocumentDto>> GetAllAsync(int? subjectId = null)
        {
            var docs = await _docRepo.GetAllWithIncludeAsync(
                d => (!subjectId.HasValue || d.SubjectId == subjectId.Value) && !d.IsDeleted,
                d => d.Subject
            );

            return docs.Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileName = d.FileName,
                FileType = d.FileType,
                FileSize = d.FileSize,
                SubjectId = d.SubjectId,
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                SubjectName = d.Subject?.Name
            });
        }

        public async Task<DocumentDto?> GetByIdAsync(int id)
        {
            var doc = await _docRepo.GetByIdAsync(id);
            if (doc == null || doc.IsDeleted) return null;

            return new DocumentDto
            {
                Id = doc.Id,
                Title = doc.Title,
                FileName = doc.FileName,
                FileType = doc.FileType,
                FileSize = doc.FileSize,
                SubjectId = doc.SubjectId,
                Status = doc.Status,
                CreatedAt = doc.CreatedAt
            };
        }

        public async Task<DocumentDto> UploadAsync(IFormFile file, string title, int subjectId, string webRootPath)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext is not ".pdf" and not ".docx" and not ".pptx")
                throw new InvalidOperationException("Chỉ hỗ trợ PDF, DOCX và PPTX.");

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var uploadRoot = Path.Combine(webRootPath, "uploads");
            Directory.CreateDirectory(uploadRoot);

            var savedName = $"{Guid.NewGuid()}{ext}";
            var savedPath = Path.Combine(uploadRoot, savedName);

            await using var stream = File.Create(savedPath);
            await file.CopyToAsync(stream);

            var doc = new EntityDocument
            {
                Title = title,
                FileName = file.FileName,
                FilePath = savedPath,
                FileType = ext.TrimStart('.'),
                FileSize = file.Length,
                SubjectId = subjectId,
                Status = DocumentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _docRepo.AddAsync(doc);
            await _docRepo.SaveChangesAsync();

            return new DocumentDto
            {
                Id = doc.Id,
                Title = doc.Title,
                FileName = doc.FileName,
                FileType = doc.FileType,
                FileSize = doc.FileSize,
                SubjectId = doc.SubjectId,
                Status = doc.Status,
                CreatedAt = doc.CreatedAt
            };
        }

        public async Task DeleteAsync(int id)
        {
            var doc = await _docRepo.GetByIdAsync(id);
            if (doc is null || doc.IsDeleted) return;

            doc.IsDeleted = true;
            doc.UpdatedAt = DateTime.UtcNow;

            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();
        }

        public async Task TriggerEmbeddingAsync(int documentId)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Bắt đầu xử lý Embedding cho documentId: {documentId}");
            var doc = await _docRepo.GetByIdAsync(documentId);
            if (doc is null || doc.IsDeleted) return;

            doc.Status = DocumentStatus.Processing;
            doc.UpdatedAt = DateTime.UtcNow;
            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();
            await _notification.ProcessingStarted(documentId, doc.SubjectId);
            try
            {
                var ext = Path.GetExtension(doc.FilePath).ToLowerInvariant();
                var fileType = (doc.FileType ?? ext.TrimStart('.')).ToLowerInvariant();

                // List chunk cuối cùng: (text, charStart, charEnd, pageNumber, sectionTitle, sourceType)
                var allChunks = new List<(string Text, int CharStart, int CharEnd, int? PageNumber, string? SectionTitle, string SourceType)>();

                if (fileType == "pdf" || ext == ".pdf")
                {
                    var pages = ExtractPagesFromPdf(doc.FilePath);
                    if (!pages.Any()) throw new InvalidOperationException("Không thể trích xuất văn bản từ PDF.");

                    // Gộp tất cả text để chunk, nhưng track page boundary
                    var fullText = new StringBuilder();
                    var pageBoundaries = new List<(int Start, int End, int PageNumber)>();

                    foreach (var (text, pageNum) in pages)
                    {
                        int start = fullText.Length;
                        fullText.Append(text);
                        fullText.Append('\n');
                        pageBoundaries.Add((start, fullText.Length, pageNum));
                    }

                    var rawText = fullText.ToString();
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\r\n|\r", "\n");
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"[ \t]+", " ");
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\n{3,}", "\n\n");
                    rawText = rawText.Trim();

                    foreach (var (text, charStart, charEnd) in ChunkText(rawText, 800, 0.1))
                    {
                        // Tìm page chứa phần lớn chunk này
                        int midPoint = charStart + (charEnd - charStart) / 2;
                        var page = pageBoundaries.FirstOrDefault(p => p.Start <= midPoint && midPoint < p.End);
                        allChunks.Add((text, charStart, charEnd, page.PageNumber > 0 ? page.PageNumber : (int?)null, null, "pdf"));
                    }
                }
                else if (fileType == "docx" || ext == ".docx")
                {
                    var sections = ExtractSectionsFromDocx(doc.FilePath);
                    if (!sections.Any()) throw new InvalidOperationException("Không thể trích xuất văn bản từ DOCX.");

                    var fullText = new StringBuilder();
                    var sectionBoundaries = new List<(int Start, int End, string? Heading)>();

                    foreach (var (text, heading) in sections)
                    {
                        int start = fullText.Length;
                        fullText.Append(text);
                        fullText.Append('\n');
                        sectionBoundaries.Add((start, fullText.Length, heading));
                    }

                    var rawText = fullText.ToString();
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\r\n|\r", "\n");
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"[ \t]+", " ");
                    rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\n{3,}", "\n\n");
                    rawText = rawText.Trim();

                    foreach (var (text, charStart, charEnd) in ChunkText(rawText, 800, 0.1))
                    {
                        int midPoint = charStart + (charEnd - charStart) / 2;
                        var section = sectionBoundaries.FirstOrDefault(s => s.Start <= midPoint && midPoint < s.End);
                        allChunks.Add((text, charStart, charEnd, null, section.Heading, "docx"));
                    }
                }
                else if (fileType == "pptx" || ext == ".pptx")
                {
                    var slides = ExtractSlidesFromPptx(doc.FilePath);
                    if (!slides.Any()) throw new InvalidOperationException("Không thể trích xuất văn bản từ PPTX.");

                    // PPTX: mỗi slide là 1 chunk tự nhiên, không cần ChunkText
                    int idx2 = 0;
                    foreach (var (text, slideNum) in slides)
                    {
                        allChunks.Add((text.Trim(), idx2 * 1000, idx2 * 1000 + text.Length, slideNum, null, "pptx"));
                        idx2++;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Chỉ hỗ trợ PDF, DOCX và PPTX.");
                }

                int totalChunks = allChunks.Count;
                var hfToken = _configuration["HuggingFace:Token"];
                if (string.IsNullOrWhiteSpace(hfToken))
                    throw new InvalidOperationException("Hugging Face token không được cấu hình.");

                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

                int idx = 0;
                var batchSize = 5;
                var pendingCount = 0;
                foreach (var (text, charStart, charEnd, pageNumber, sectionTitle, sourceType) in allChunks)
                {
                    string formattedText = $"passage: {text}";
                    var payload = new { inputs = formattedText };
                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var resp = await client.PostAsync(modelUrl, content);
                    if (resp.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                        throw new InvalidOperationException("Server AI đang khởi động, vui lòng đợi 20 giây và bấm nút lại!");

                    resp.EnsureSuccessStatusCode();
                    var respJson = await resp.Content.ReadAsStringAsync();

                    using var docJson = JsonDocument.Parse(respJson);
                    var vector = new List<float>();
                    var root = docJson.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var firstElement = root[0];
                        var vectorArray = firstElement.ValueKind == JsonValueKind.Number ? root : firstElement;
                        foreach (var el in vectorArray.EnumerateArray())
                            vector.Add(el.GetSingle());
                    }

                    var chunkEntity = new DocumentChunk
                    {
                        DocumentId = documentId,
                        Content = text,
                        ChunkIndex = idx++,
                        EmbeddingJson = JsonSerializer.Serialize(vector),
                        EmbeddingModel = "multilingual-e5-base",
                        CreatedAt = DateTime.UtcNow,
                        PageNumber = pageNumber,
                        SectionTitle = sectionTitle,
                        SourceType = sourceType
                    };

                    await _chunkRepo.AddAsync(chunkEntity);
                    pendingCount++;
                    if (pendingCount >= batchSize)
                    {
                        await _chunkRepo.SaveChangesAsync();
                        pendingCount = 0;
                    }

                    await _notification.ChunkLogCreated(documentId, chunkEntity.ChunkIndex, totalChunks, chunkEntity.Content, doc.SubjectId);
                    await _notification.ChunkProcessed(documentId, chunkEntity.ChunkIndex + 1, totalChunks, doc.SubjectId);
                    await Task.Delay(300);
                }

                // Save remaining chunks that haven't been saved yet
                if (pendingCount > 0)
                {
                    await _chunkRepo.SaveChangesAsync();
                    pendingCount = 0;
                }

                // Update document status to Ready after successful embedding
                doc.Status = DocumentStatus.Ready;
                doc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(doc);
                await _docRepo.SaveChangesAsync();

                // Notify that processing is complete
                await _notification.ProcessingCompleted(documentId, doc.SubjectId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FATAL ERROR] {ex.ToString()}");

                doc.Status = DocumentStatus.Failed;
                doc.UpdatedAt = DateTime.UtcNow;
                _docRepo.Update(doc);
                await _docRepo.SaveChangesAsync();
                //await SaveLogAsync(documentId, "Document processing failed", "Error");
                await _notification.ProcessingFailed(
    documentId,
    ex.Message,
    doc.SubjectId);
                // Do not rethrow to avoid crashing background workers or leaving callers unhandled.
                // Error already recorded and notification sent.
                return;
            }
        }
        private static string ExtractTextFromDocx(string path)
        {
            var sb = new StringBuilder();
            using (var doc = WordprocessingDocument.Open(path, false))
            {
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null) return string.Empty;
                foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(para.InnerText);
                }
            }
            return sb.ToString();
        }

        // Thay toàn bộ hàm ChunkText cũ bằng hàm này
        private static IEnumerable<(string Text, int CharStart, int CharEnd)> ChunkText(
            string text, int chunkSize = 800, double overlapFraction = 0.1)
        {
            if (string.IsNullOrWhiteSpace(text)) yield break;

            int overlap = (int)Math.Round(chunkSize * overlapFraction);
            int step = Math.Max(1, chunkSize - overlap);
            int pos = 0;
            while (pos < text.Length)
            {
                int len = Math.Min(chunkSize, text.Length - pos);
                var trimmed = text.Substring(pos, len).Trim();
                yield return (trimmed, pos, pos + len);
                pos += step;
            }
        }

        public async Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isStaff, int? subjectId = null)
        {
            IEnumerable<EntityDocument> docs;

            if (isStaff)
            {
                docs = await _docRepo.GetAllWithIncludeAsync(
                    d => (!subjectId.HasValue || d.SubjectId == subjectId.Value) && !d.IsDeleted,
                    d => d.Subject
                );
            }
            else
            {
                var enrollments = await _studentSubjectRepo.GetAllAsync();
                var allowedSubjectIds = enrollments
                    .Where(ss => ss.StudentId == userId && !ss.IsDeleted)
                    .Select(ss => ss.SubjectId)
                    .ToList();

                docs = await _docRepo.GetAllWithIncludeAsync(
                    d => allowedSubjectIds.Contains(d.SubjectId) &&
                         (!subjectId.HasValue || d.SubjectId == subjectId.Value) &&
                         !d.IsDeleted,
                    d => d.Subject
                );
            }

            return docs.Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                FileName = d.FileName,
                FileType = d.FileType,
                FileSize = d.FileSize,
                SubjectId = d.SubjectId,
                Status = d.Status,
                CreatedAt = d.CreatedAt,
                SubjectName = d.Subject?.Name
            });
        }

        public async Task UpdateTitleAsync(int id, string newTitle)
        {
            var doc = await _docRepo.GetByIdAsync(id);
            if (doc is null) throw new InvalidOperationException("Không tìm thấy tài liệu.");

            doc.Title = newTitle;
            doc.UpdatedAt = DateTime.UtcNow;

            _docRepo.Update(doc);
            await _docRepo.SaveChangesAsync();
        }

        public async Task<DocumentDownloadDto?> GetFileForDownloadAsync(int id)
        {
            var doc = await _docRepo.GetByIdAsync(id);
            if (doc == null || doc.IsDeleted) return null;

            if (!System.IO.File.Exists(doc.FilePath))
            {
                throw new FileNotFoundException("File vật lý không tồn tại trên hệ thống.");
            }

            string fileType = (doc.FileType ?? Path.GetExtension(doc.FilePath)).ToLowerInvariant().TrimStart('.');

            string contentType = fileType switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            return new DocumentDownloadDto
            {
                FilePath = doc.FilePath,
                ContentType = contentType,
                FileName = doc.FileName
            };
        }

        public async Task<bool> HasReadyDocumentsAsync(int subjectId)
        {
            var docs = await _docRepo.GetAllAsync(d =>
                d.SubjectId == subjectId &&
                d.Status == SmartEdu.Shared.Enums.DocumentStatus.Ready &&
                !d.IsDeleted);

            return docs.Any();
        }

        // PDF — trả về list (text, pageNumber)
        private static List<(string Text, int PageNumber)> ExtractPagesFromPdf(string path)
        {
            var pages = new List<(string, int)>();
            using var pdf = PdfDocument.Open(path);
            foreach (var page in pdf.GetPages())
            {
                var text = page.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    pages.Add((text, page.Number));
            }
            return pages;
        }

        // DOCX — trả về list (text, headingGanNhat)
        private static List<(string Text, string? SectionTitle)> ExtractSectionsFromDocx(string path)
        {
            var sections = new List<(string, string?)>();
            using var doc = WordprocessingDocument.Open(path, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return sections;

            string? currentHeading = null;
            var buffer = new StringBuilder();

            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var style = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                bool isHeading = style.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
                              || style.StartsWith("Title", StringComparison.OrdinalIgnoreCase);

                if (isHeading)
                {
                    // flush buffer trước khi chuyển heading
                    if (buffer.Length > 0)
                    {
                        sections.Add((buffer.ToString().Trim(), currentHeading));
                        buffer.Clear();
                    }
                    currentHeading = para.InnerText?.Trim();
                }
                else
                {
                    var text = para.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(text))
                        buffer.AppendLine(text);
                }
            }

            if (buffer.Length > 0)
                sections.Add((buffer.ToString().Trim(), currentHeading));

            return sections;
        }

        // PPTX — trả về list (text, slideNumber)
        private static List<(string Text, int SlideNumber)> ExtractSlidesFromPptx(string path)
        {
            var slides = new List<(string, int)>();
            using var prs = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(path, false);
            var slideIds = prs.PresentationPart?.Presentation?.SlideIdList?.Elements<DocumentFormat.OpenXml.Presentation.SlideId>().ToList();
            if (slideIds == null) return slides;

            for (int i = 0; i < slideIds.Count; i++)
            {
                var relId = slideIds[i].RelationshipId?.Value;
                if (relId == null) continue;

                var slidePart = (DocumentFormat.OpenXml.Packaging.SlidePart)prs.PresentationPart!.GetPartById(relId);
                var sb = new StringBuilder();

                foreach (var para in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
                {
                    var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Drawing.Run>()
                        .Select(r => r.Text?.Text ?? "")).Trim();
                    if (!string.IsNullOrEmpty(text))
                        sb.AppendLine(text);
                }

                var content = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(content))
                    slides.Add((content, i + 1));
            }
            return slides;
        }
    }
}
