using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using System.Linq;
using System.Collections.Generic;

namespace SmartEdu.RazorWeb.Pages.Chat
{

    public class IndexModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly IPermissionService _permissionService;

        public IndexModel(IChatService chatService, ISubjectService subjectService, IPermissionService permissionService)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _permissionService = permissionService;
        }

        public IEnumerable<ChatSessionDto> Sessions { get; set; } = new List<ChatSessionDto>();
        public IEnumerable<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            Sessions = await _chatService.GetSessionsByUserIdAsync(userId.Value.ToString());

            // Load subjects and filter by permission for non-admin users
            var allSubjects = await _subjectService.GetAllAsync();
            var role = HttpContext.Session.GetString("Role") ?? string.Empty;
            if (string.Equals(role, "Admin", System.StringComparison.OrdinalIgnoreCase))
            {
                Subjects = allSubjects;
            }
            else
            {
                var allowed = new List<SubjectDto>();
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
                Subjects = allowed;
            }
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAskAsync([FromBody] ChatAskRequest req)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return new JsonResult(new { success = false, error = "Not authenticated" });

            // If subject specified, ensure user has access
            if (req.SubjectId.HasValue)
            {
                var ok = await _permissionService.CanUserAccessSubject(userId.Value, req.SubjectId.Value);
                if (!ok)
                {
                    return new ObjectResult(new { success = false, message = "Bạn không có quyền truy cập môn học này." }) { StatusCode = 403 };
                }
            }

            var request = new ChatRequestDto
            {
                SessionId = req.SessionId,
                Question = req.Question,
                SubjectId = req.SubjectId,
                UserId = userId.Value
            };

            var response = await _chatService.AskAsync(request);

            return new JsonResult(new { success = true, answer = response.Answer, sources = response.Sources, sessionId = response.SessionId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostLoadHistoryAsync([FromBody] string sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return new JsonResult(new { success = false, error = "Not authenticated" });

            // Ensure the session belongs to this user and check subject permission if any
            var sessions = await _chatService.GetSessionsByUserIdAsync(userId.Value.ToString());
            var session = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (session == null)
            {
                return new ObjectResult(new { success = false, message = "Bạn không có quyền truy cập session này." }) { StatusCode = 403 };
            }

            if (session.SubjectId.HasValue)
            {
                var ok = await _permissionService.CanUserAccessSubject(userId.Value, session.SubjectId.Value);
                if (!ok)
                {
                    return new ObjectResult(new { success = false, message = "Bạn không có quyền truy cập môn học này." }) { StatusCode = 403 };
                }
            }

            var history = await _chatService.GetHistoryAsync(sessionId, userId.Value.ToString());
            return new JsonResult(new { success = true, messages = history });
        }

        [ValidateAntiForgeryToken]
        public async Task<JsonResult> OnPostDeleteSessionAsync([FromBody] string sessionId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return new JsonResult(new { success = false, error = "Not authenticated" });

            await _chatService.DeleteSessionAsync(sessionId, userId.Value.ToString());
            return new JsonResult(new { success = true });
        }
    }

    public class ChatAskRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
    }
}
