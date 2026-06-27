using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess.EntityModels;
using SmartEdu.DataAccess.Repositories;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartEdu.Business.Services
{
    public class ChatService : IChatService
    {
        private readonly IRepository<ChatSession> _sessionRepo;
        private readonly IRepository<ChatMessage> _messageRepo;
        private readonly IRepository<DocumentChunk> _chunkRepo;
        private readonly IChatNotificationService _notification;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        //private readonly IServiceScopeFactory _serviceScopeFactory;

        public ChatService(
            IRepository<ChatSession> sessionRepo,
            IRepository<ChatMessage> messageRepo,
            IRepository<DocumentChunk> chunkRepo,
            IHttpClientFactory httpFactory,
            IConfiguration configuration, IChatNotificationService notification
            )
        {
            _sessionRepo = sessionRepo;
            _messageRepo = messageRepo;
            _chunkRepo = chunkRepo;
            _httpFactory = httpFactory;
            _configuration = configuration;
            _notification = notification;
        }

        public async Task<ChatResponseDto> AskAsync(ChatRequestDto request)
        {
            if (!request.SubjectId.HasValue || request.SubjectId.Value <= 0)
            {
                return new ChatResponseDto { SessionId = request.SessionId, Answer = "Vui lòng chọn môn học.", Sources = new List<SourceInfoDto>() };
            }

            var sessions = await _sessionRepo.GetAllAsync(s => s.SessionId == request.SessionId);
            var session = sessions.FirstOrDefault();
            bool isNewSession = session == null;

            if (session == null)
            {
                session = new ChatSession
                {
                    SessionId = request.SessionId,
                    SubjectId = request.SubjectId,
                    Title = FallbackTitle(request.Question), // title tạm, hiển thị ngay
                    UserId = request.UserId
                };
                await _sessionRepo.AddAsync(session);
                await _sessionRepo.SaveChangesAsync();
                string? subjectName = null;
                if (session.SubjectId.HasValue)
                {
                    // Lấy tên subject nếu cần hiển thị badge - có thể bỏ qua nếu muốn đơn giản
                }
                await _notification.SendSessionCreatedAsync(request.UserId, session.SessionId, session.Title, subjectName);
            }
            else if (session.UserId != request.UserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên chat này.");
            }

            var ragTask = RunRagPipelineAsync(request, session);
            var ragResponse = await ragTask;

            // Sinh title "xịn" ở background, chỉ cho session mới, không block response
            if (isNewSession)
            {
                var sessionId = session.Id;
                var sessionSessionId = session.SessionId;
                var question = request.Question;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var betterTitle = await GenerateChatTitleAsync(question);
                        var freshSessions = await _sessionRepo.GetAllAsync(s => s.Id == sessionId);
                        var freshSession = freshSessions.FirstOrDefault();
                        if (freshSession != null && !string.IsNullOrWhiteSpace(betterTitle))
                        {
                            freshSession.Title = betterTitle;
                            _sessionRepo.Update(freshSession);
                            await _sessionRepo.SaveChangesAsync();
                            await _notification.SendTitleUpdatedAsync(sessionSessionId, betterTitle);
                        }
                    }
                    catch
                    {
                        // bỏ qua lỗi, giữ nguyên fallback title
                    }
                });
            }

            return ragResponse;
        }

        private async Task<string> GenerateChatTitleAsync(string question)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "myKey")
                    return FallbackTitle(question);

                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey.Trim()}";
                string prompt = $"Tóm tắt câu hỏi sau thành một tiêu đề ngắn gọn bằng tiếng Việt (tối đa 6 từ), không dùng dấu ngoặc kép, không kết thúc bằng dấu chấm, không giải thích gì thêm, chỉ trả về tiêu đề:\n\"{question}\"";

                var payload = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new { temperature = 0.3, maxOutputTokens = 30 }
                };

                var client = _httpFactory.CreateClient();
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(url, content);
                if (!resp.IsSuccessStatusCode) return FallbackTitle(question);

                var respJson = await resp.Content.ReadAsStringAsync();
                using var docJson = JsonDocument.Parse(respJson);
                var candidates = docJson.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) return FallbackTitle(question);

                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrWhiteSpace(text)) return FallbackTitle(question);

                var title = text.Trim().Trim('"', '“', '”', '.');
                return title.Length > 60 ? title[..60] : title;
            }
            catch
            {
                return FallbackTitle(question);
            }
        }

        private static string FallbackTitle(string question)
        {
            var trimmed = question?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmed)) return "Cuộc trò chuyện mới";

            trimmed = char.ToUpper(trimmed[0]) + trimmed[1..];

            const int maxLen = 40;
            if (trimmed.Length <= maxLen) return trimmed;

            var words = trimmed[..maxLen].Split(' ');
            return string.Join(' ', words[..^1]) + "...";
        }

        private async Task<ChatResponseDto> RunRagPipelineAsync(ChatRequestDto request, ChatSession session)
        {
            var userMessage = new ChatMessage { ChatSessionId = session.Id, Role = "user", Content = request.Question };
            await _messageRepo.AddAsync(userMessage);
            await _messageRepo.SaveChangesAsync();

            float[] queryVector = await GetHuggingFaceEmbeddingAsync(request.Question);
            var chunks = await _chunkRepo.GetAllWithIncludeAsync(
                c => c.Document != null && c.Document.Status == DocumentStatus.Ready && c.Document.SubjectId == request.SubjectId.Value,
                c => c.Document
            );

            // Filter out chunks with invalid embeddings and calculate similarity scores
            var topChunks = chunks
                .Where(c => !string.IsNullOrWhiteSpace(c.EmbeddingJson))  // Lọc chunks có embedding hợp lệ
                .Select(c => 
                {
                    try
                    {
                        var embeddingVector = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson);
                        if (embeddingVector == null || embeddingVector.Length == 0)
                            return null;

                        return new { Chunk = c, Score = CosineSimilarity(queryVector, embeddingVector) };
                    }
                    catch
                    {
                        // Bỏ qua chunks có embedding invalid
                        return null;
                    }
                })
                .Where(x => x != null)  // Loại bỏ các entries null
                .OrderByDescending(s => s.Score)
                .Take(3)
                .ToList();

            var contextBuilder = new StringBuilder();
            var sources = new List<SourceInfoDto>();

            // Nếu không có chunks liên quan, thông báo cho user
            if (!topChunks.Any())
            {
                string noContextAnswer = await GenerateGeminiResponseAsync("Không có tài liệu liên quan trong hệ thống.", request.Question);
                var noContextMessage = new ChatMessage { ChatSessionId = session.Id, Role = "assistant", Content = noContextAnswer };
                await _messageRepo.AddAsync(noContextMessage);
                await _messageRepo.SaveChangesAsync();
                await _notification.SendAnswerAsync(request.SessionId, noContextAnswer);
                return new ChatResponseDto { SessionId = request.SessionId, Answer = noContextAnswer, Sources = new List<SourceInfoDto>() };
            }

            foreach (var item in topChunks)
            {
                contextBuilder.AppendLine($"[Nguồn: {item.Chunk.Document.Title}]\n{item.Chunk.Content}\n---\n");

                // Tạo SourceInfoDto từ thông tin chunk
                var sourceInfo = new SourceInfoDto
                {
                    DocumentTitle = item.Chunk.Document.Title,
                    SourceType = item.Chunk.SourceType ?? "pdf",  // Default to pdf if not set
                    PageNumber = item.Chunk.PageNumber,
                    SectionTitle = item.Chunk.SectionTitle
                };

                // Chỉ thêm nếu chưa có source này trong danh sách (tránh trùng lặp)
                if (!sources.Any(s => s.DocumentTitle == sourceInfo.DocumentTitle && 
                                      s.SourceType == sourceInfo.SourceType && 
                                      s.PageNumber == sourceInfo.PageNumber &&
                                      s.SectionTitle == sourceInfo.SectionTitle))
                {
                    sources.Add(sourceInfo);
                }
            }

            string answer = await GenerateGeminiResponseAsync(contextBuilder.ToString(), request.Question);
            var assistantMessage = new ChatMessage { ChatSessionId = session.Id, Role = "assistant", Content = answer };
            await _messageRepo.AddAsync(assistantMessage);
            await _messageRepo.SaveChangesAsync();

            // NOTE: Removed SendAnswerAsync - response is sent via HTTP return below
            // Keeping it would cause duplicate messages on frontend

            return new ChatResponseDto { SessionId = request.SessionId, Answer = answer, Sources = sources };
        }
        public async Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionId, string userId)
        {
            int uid = int.Parse(userId);

            var sessions = await _sessionRepo.GetAllAsync(s => s.SessionId == sessionId && s.UserId == uid);
            var session = sessions.FirstOrDefault();
            if (session is null) return Enumerable.Empty<ChatMessageDto>();

            var messages = await _messageRepo.GetAllAsync(m => m.ChatSessionId == session.Id);

            return messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList();
        }

        private async Task<float[]> GetHuggingFaceEmbeddingAsync(string question)
        {
            var hfToken = _configuration["HuggingFace:Token"];
            if (string.IsNullOrWhiteSpace(hfToken)) throw new Exception("Thiếu HuggingFace Token.");

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hfToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);  // Set 30 second timeout

            string modelUrl = "https://router.huggingface.co/hf-inference/models/intfloat/multilingual-e5-base/pipeline/feature-extraction";

            // Model E5 yêu cầu prefix 'query: ' cho câu hỏi
            var payload = new { inputs = $"query: {question}" };
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var resp = await client.PostAsync(modelUrl, content);
                resp.EnsureSuccessStatusCode();
                var respJson = await resp.Content.ReadAsStringAsync();

                using var docJson = JsonDocument.Parse(respJson);
                var root = docJson.RootElement;
                var vectorArray = root.ValueKind == JsonValueKind.Array && root[0].ValueKind != JsonValueKind.Number ? root[0] : root;

                return vectorArray.EnumerateArray().Select(x => x.GetSingle()).ToArray();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Lỗi khi gọi HuggingFace API: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"HuggingFace API timeout (>30s): {ex.Message}");
            }
        }

        private async Task<string> GenerateGeminiResponseAsync(string context, string question)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "myKey")
                throw new Exception("Thiếu Gemini API Key hợp lệ trong User Secrets.");

            // Đảm bảo endpoint gọi bản 2.5-flash ổn định
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey.Trim()}";

            string fullPrompt = @"Bạn là trợ lý học tập thông minh (SmartEdu AI).
Nhiệm vụ của bạn là trả lời câu hỏi dựa trên các đoạn ngữ cảnh trích từ tài liệu. Luôn trả lời bằng tiếng Việt tự nhiên, lịch sự. Nếu thông tin không có, hãy nói 'Tôi không tìm thấy thông tin'.

NGỮ CẢNH:
" + context + @"

CÂU HỎI:
" + question;

            // Cấu trúc Payload chuẩn hóa 100% cho Gemini 2.x - Đưa maxOutputTokens vào đúng vị trí
            var payload = new
            {
                contents = new[]
                {
                new { parts = new[] { new { text = fullPrompt } } }
            },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 8192
                }
            };

            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);  // Set 60 second timeout for Gemini
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                var resp = await client.PostAsync(url, content);
                var respJson = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    throw new Exception($"Lỗi báo từ Google: {respJson}");
                }

                using var docJson = JsonDocument.Parse(respJson);

                var root = docJson.RootElement;
                var candidates = root.GetProperty("candidates");

                if (candidates.GetArrayLength() == 0) return "Không có phản hồi từ AI.";

                var contentElement = candidates[0].GetProperty("content");
                var parts = contentElement.GetProperty("parts");

                StringBuilder fullAnswer = new StringBuilder();
                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("text", out var textElement))
                    {
                        fullAnswer.Append(textElement.GetString());
                    }
                }

                string finalResult = fullAnswer.ToString().Trim();
                return string.IsNullOrEmpty(finalResult) ? "Không có phản hồi từ AI." : finalResult;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception($"Gemini API timeout (>60s): {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi gọi Gemini API: {ex.Message}");
            }
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return 0;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            if (na == 0 || nb == 0) return 0;
            return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
        }

        public async Task<IEnumerable<ChatSessionDto>> GetSessionsByUserIdAsync(string userId)
        {
            int uid = int.Parse(userId);
            var sessions = await _sessionRepo.GetAllWithIncludeAsync(
                s => !s.IsDeleted && s.UserId == uid,
                s => s.Subject
            );

            return sessions
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ChatSessionDto
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    SubjectId = s.SubjectId,
                    SubjectName = s.Subject != null ? s.Subject.Name : "Tất cả",
                    MessageCount = 0
                }).ToList();
        }

        public async Task DeleteSessionAsync(string sessionId, string userId)
        {
            int uid = int.Parse(userId);
            var sessions = await _sessionRepo.GetAllAsync(s => s.SessionId == sessionId && s.UserId == uid);
            var session = sessions.FirstOrDefault();

            if (session is null)
            {
                return;
            }

            session.IsDeleted = true;
            session.UpdatedAt = DateTime.UtcNow;

            _sessionRepo.Update(session);
            await _sessionRepo.SaveChangesAsync();
        }
    }
}
