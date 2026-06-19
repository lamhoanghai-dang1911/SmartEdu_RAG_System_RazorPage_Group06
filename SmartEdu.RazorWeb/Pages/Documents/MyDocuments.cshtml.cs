using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;
using System.Globalization;

namespace SmartEdu.RazorWeb.Pages.Documents
{
    public class MyDocumentsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IPermissionService _permissionService;

        public MyDocumentsModel(IDocumentService documentService, ISubjectService subjectService, IPermissionService permissionService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _permissionService = permissionService;
        }

        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();
        [BindProperty(SupportsGet = true)]
        public int? SubjectId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            var allSubjects = await _subjectService.GetAllAsync();
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            var allowed = new List<SubjectDto>();
            if (string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                allowed = allSubjects.ToList();
            }
            else
            {
                foreach (var s in allSubjects)
                {
                    try
                    {
                        if (await _permissionService.CanUserAccessSubject(userId.Value, s.Id))
                        {
                            allowed.Add(s);
                        }
                    }
                    catch
                    {
                        // ignore permission check failures
                    }
                }
            }

            Subjects = allowed;

            bool isStaff = string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase)
                           || string.Equals(role, "Lecturer", System.StringComparison.OrdinalIgnoreCase);

            var docs = await _documentService.GetAllByUserIdAsync(userId.Value, isStaff, null);
            Documents = docs.Where(d => d.Status == DocumentStatus.Ready).OrderByDescending(d => d.CreatedAt);

            return Page();
        }

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.##", CultureInfo.InvariantCulture) + " KB";
            double mb = kb / 1024.0;
            return mb.ToString("0.##", CultureInfo.InvariantCulture) + " MB";
        }

        public async Task<IActionResult> OnGetFilterAsync(int? subjectId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            bool isStaff = string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase)
                           || string.Equals(role, "Lecturer", System.StringComparison.OrdinalIgnoreCase);

            var docs = await _documentService.GetAllByUserIdAsync(userId.Value, isStaff, subjectId);
            var ready = docs.Where(d => d.Status == DocumentStatus.Ready)
                            .OrderByDescending(d => d.CreatedAt)
                            .Select(d => new
                            {
                                d.Id,
                                d.Title,
                                d.SubjectName,
                                d.FileName,
                                FileType = (d.FileType ?? string.Empty).ToUpperInvariant(),
                                FileSize = FormatFileSize(d.FileSize),
                                CreatedAt = d.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy")
                            });

            return new JsonResult(ready);
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            var doc = await _documentService.GetByIdAsync(id);
            if (doc == null) return NotFound();

            var ok = await _permissionService.CanUserAccessSubject(userId.Value, doc.SubjectId);
            if (!ok) return Forbid();

            var file = await _documentService.GetFileForDownloadAsync(id);
            if (file == null) return NotFound();

            return PhysicalFile(file.FilePath, file.ContentType, file.FileName);
        }
    }
}
