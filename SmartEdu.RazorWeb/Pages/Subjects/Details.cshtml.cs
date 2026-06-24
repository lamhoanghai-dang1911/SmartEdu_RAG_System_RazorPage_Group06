using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.RazorWeb.Pages.Subjects
{
    public class DetailsModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public DetailsModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [BindProperty(SupportsGet = true)]
        public int SubjectId { get; set; }

        public SubjectDto? Subject { get; set; }

        public IEnumerable<UserDto> EnrolledStudents { get; set; } = Enumerable.Empty<UserDto>();
        public IEnumerable<UserDto> AssignedLecturers { get; set; } = Enumerable.Empty<UserDto>();

        public async Task<IActionResult> OnGetAsync(int subjectId)
        {
            SubjectId = subjectId;
            Subject = await _subjectService.GetByIdAsync(subjectId);
            if (Subject == null) return NotFound();

            var students = await _subjectService.GetStudentEnrollmentStatus(subjectId);
            EnrolledStudents = students.Enrolled;

            var lecturers = await _subjectService.GetLecturerAssignmentStatus(subjectId);
            AssignedLecturers = lecturers.Assigned;

            return Page();
        }

        // Partial JSON endpoints used by client for partial updates via SignalR events
        public async Task<IActionResult> OnGetStudentsAsync(int subjectId)
        {
            var students = await _subjectService.GetStudentEnrollmentStatus(subjectId);
            return new JsonResult(students.Enrolled);
        }

        public async Task<IActionResult> OnGetLecturersAsync(int subjectId)
        {
            var lecturers = await _subjectService.GetLecturerAssignmentStatus(subjectId);
            return new JsonResult(lecturers.Assigned);
        }
    }
}
