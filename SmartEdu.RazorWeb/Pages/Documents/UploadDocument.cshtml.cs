using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using System.Linq;
using SmartEdu.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace SmartEdu.RazorWeb.Pages.Documents
{
    public class UploadDocumentModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly SmartEdu.Business.Interfaces.IDocumentNotificationService _notification;

        public UploadDocumentModel(IDocumentService documentService, ISubjectService subjectService, SmartEdu.Business.Interfaces.IDocumentNotificationService notification)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _notification = notification;
        }

        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        [BindProperty]
        public string Title { get; set; } = string.Empty;

        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public bool IsLeader { get; set; }
        public string? AccessDeniedMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // check permission: only leader of SubjectId may access
            if (SubjectId.HasValue)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue) return RedirectToPage("/Account/Login");

                var isLeader = await _subjectService.CanUploadDocument(userId.Value, SubjectId.Value);
                IsLeader = isLeader;
                if (isLeader)
                {
                    Documents = await _documentService.GetAllAsync(SubjectId);
                }
                else
                {
                    // If not leader, allow assigned (but non-leader) lecturers to view/download documents.
                    var assignedSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId.Value);
                    var isAssigned = assignedSubjects.Any(s => s.Id == SubjectId.Value);
                    if (isAssigned)
                    {
                        Documents = await _documentService.GetAllAsync(SubjectId);
                        AccessDeniedMessage = "Bạn không có quyền upload/tác vụ khác; bạn chỉ có quyền xem tài liệu.";
                    }
                    else
                    {
                        AccessDeniedMessage = "Bạn không có quyền xem tài liệu cho môn này.";
                        Documents = new List<DocumentDto>();
                    }
                }
            }

            Subjects = await _subjectService.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUploadAsync(IFormFile? File)
        {
            // debug: log incoming files count to help diagnose missing file
            System.Diagnostics.Debug.WriteLine($"OnPostUploadAsync - Request.Form.Files.Count={Request?.Form?.Files?.Count}");

            if (!SubjectId.HasValue) ModelState.AddModelError(string.Empty, "Subject must be selected.");
            if (File == null) ModelState.AddModelError("File", "File is required.");

            // re-check leader permission before processing upload
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            if (SubjectId.HasValue)
            {
                var can = await _subjectService.CanUploadDocument(userId.Value, SubjectId.Value);
                IsLeader = can;
                if (!can)
                {
                    ModelState.AddModelError(string.Empty, "Bạn không có quyền upload tài liệu cho môn này.");
                }
            }

            if (!ModelState.IsValid)
            {
                Subjects = await _subjectService.GetAllAsync();
                Documents = await _documentService.GetAllAsync(SubjectId);
                return Page();
            }

            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var created = await _documentService.UploadAsync(File!, Title, SubjectId!.Value, webRoot);

            // Notify connected clients in the subject group so they can refresh document lists
            try
            {
                await _notification.DocumentAdded(created.Id, SubjectId!.Value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DocumentAdded notification failed: {ex}");
            }

            return RedirectToPage(new { SubjectId });
        }

        public async Task<IActionResult> OnGetDocumentsAsync()
        {
            if (!SubjectId.HasValue) return new JsonResult(new object[0]);
            var docs = await _documentService.GetAllAsync(SubjectId);
            var result = docs.Select(d => new
            {
                id = d.Id,
                title = d.Title,
                fileName = d.FileName,
                status = d.Status.ToString()
            });
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetChunksAsync(int documentId)
        {
            var doc = await _documentService.GetByIdAsync(documentId);
            if (doc == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Unauthorized();

            // allow leader or assigned lecturers to view chunks
            var can = await _subjectService.CanUploadDocument(userId.Value, doc.SubjectId);
            if (!can)
            {
                var assigned = await _subjectService.GetSubjectsByLecturerIdAsync(userId.Value);
                var isAssigned = assigned.Any(s => s.Id == doc.SubjectId);
                if (!isAssigned) return Forbid();
            }

            var chunks = await _documentService.GetChunksByDocumentIdAsync(documentId);
            var result = chunks.Select(c => new { index = c.ChunkIndex, content = c.Content });
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostTriggerEmbeddingAsync(int documentId)
        {
            // only leaders can trigger - re-check
            var doc = await _documentService.GetByIdAsync(documentId);
            if (doc == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Unauthorized();

            var can = await _subjectService.CanUploadDocument(userId.Value, doc.SubjectId);
            if (!can) return Forbid();

            // Start embedding in background to avoid blocking HTTP request.
            // Create a new scope so scoped services are available for background work.
            System.Diagnostics.Debug.WriteLine($"Starting background embedding for document {documentId}");
            var svcProvider = HttpContext.RequestServices;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = svcProvider.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    await svc.TriggerEmbeddingAsync(documentId);
                    System.Diagnostics.Debug.WriteLine($"Background embedding completed for document {documentId}");
                }
                catch (Exception ex)
                {
                    // log unexpected background error
                    System.Diagnostics.Debug.WriteLine($"Background embedding failed for {documentId}: {ex}");
                }
            });

            // Return JSON so AJAX caller does not receive a redirect and SignalR stays connected.
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToPage("/Account/Login");

            var can = await _subjectService.CanUploadDocument(userId.Value, doc.SubjectId);
            if (!can) return Forbid();

            await _documentService.DeleteAsync(id);

            // Notify connected clients in the subject group so they can remove the document row
            try
            {
                await _notification.DocumentDeleted(id, doc.SubjectId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DocumentDeleted notification failed: {ex}");
            }

            return RedirectToPage(new { SubjectId = doc.SubjectId });
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null) return NotFound();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToPage("/Account/Login");
            var isLeader = await _subjectService.CanUploadDocument(userId.Value, doc.SubjectId);
            if (!isLeader)
            {
                var assignedSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId.Value);
                var isAssigned = assignedSubjects.Any(s => s.Id == doc.SubjectId);
                if (!isAssigned) return Forbid();
            }

            var file = await _documentService.GetFileForDownloadAsync(id);
            if (file == null) return NotFound();

            return PhysicalFile(file.FilePath, file.ContentType ?? "application/octet-stream", file.FileName);
        }

        public async Task<IActionResult> OnGetSubjectsAsync()
        {
            var subjects = await _subjectService.GetAllAsync();
            return new JsonResult(subjects.Select(s => new { id = s.Id, name = s.Name }));
        }
    }
}
