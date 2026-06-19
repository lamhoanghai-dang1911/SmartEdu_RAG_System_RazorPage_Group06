using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;

namespace SmartEdu.RazorWeb.Pages.Subjects
{
    public class ManageModel : PageModel
    {
        private readonly ISubjectService _subjectService;

        public ManageModel(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        [BindProperty(SupportsGet = true)]
        public int SubjectId { get; set; }

        public IEnumerable<UserDto> AssignedLecturers { get; set; } = new List<UserDto>();
        public IEnumerable<UserDto> NotAssignedLecturers { get; set; } = new List<UserDto>();

        public async Task<IActionResult> OnGet()
        {
            if (SubjectId > 0)
            {
                var result = await _subjectService.GetLecturerAssignmentStatus(SubjectId);
                AssignedLecturers = result.Assigned;
                NotAssignedLecturers = result.NotAssigned;
            }

            return Page();
        }

        public async Task<IActionResult> OnGetLoadAsync(int subjectId)
        {
            SubjectId = subjectId;
            var result = await _subjectService.GetLecturerAssignmentStatus(subjectId);
            AssignedLecturers = result.Assigned;
            NotAssignedLecturers = result.NotAssigned;
            return Partial("_LecturersPartial", this);
        }

        public async Task<IActionResult> OnPostAssignLecturerAsync(int lecturerId, bool isLeader = false)
        {
            if (SubjectId <= 0)
            {
                return BadRequest("SubjectId is required.");
            }

            try
            {
                if (isLeader)
                    await _subjectService.SetLeaderAsync(SubjectId, lecturerId);
                else
                    await _subjectService.AssignLecturerToSubject(new AssignLecturerDto { LecturerId = lecturerId, SubjectId = SubjectId, IsLeader = false });

                return RedirectToPage(new { SubjectId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message; // Lưu lỗi để hiển thị
                return RedirectToPage(new { SubjectId });
            }
        }

        public async Task<IActionResult> OnPostRemoveLecturerAsync(int lecturerId)
        {
            await _subjectService.RemoveLecturerFromSubject(lecturerId, SubjectId);
            return RedirectToPage(new { SubjectId });
        }

        public async Task<IActionResult> OnPostImportStudentsAsync()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn file Excel để import.";
                return RedirectToPage(new { SubjectId });
            }

            var students = new List<StudentImportDto>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1); // sheet đầu tiên

                // Giả định dòng 1 là header, dữ liệu bắt đầu từ dòng 2
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
                if (rows == null)
                {
                    TempData["ErrorMessage"] = "File Excel không có dữ liệu.";
                    return RedirectToPage(new { SubjectId });
                }

                foreach (var row in rows)
                {
                    var studentCode = row.Cell(1).GetString().Trim(); // Cột A
                    var fullName = row.Cell(2).GetString().Trim();    // Cột B
                    var email = row.Cell(3).GetString().Trim();       // Cột C

                    // Bỏ qua dòng trống
                    if (string.IsNullOrWhiteSpace(studentCode) &&
                        string.IsNullOrWhiteSpace(fullName) &&
                        string.IsNullOrWhiteSpace(email))
                    {
                        continue;
                    }

                    // Validate dữ liệu tối thiểu
                    if (string.IsNullOrWhiteSpace(studentCode) ||
                        string.IsNullOrWhiteSpace(fullName) ||
                        string.IsNullOrWhiteSpace(email))
                    {
                        TempData["ErrorMessage"] = $"Dòng dữ liệu thiếu thông tin (StudentCode/FullName/Email): {studentCode} - {fullName} - {email}";
                        return RedirectToPage(new { SubjectId });
                    }

                    students.Add(new StudentImportDto
                    {
                        StudentCode = studentCode,
                        FullName = fullName,
                        Email = email
                    });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi đọc file Excel: {ex.Message}";
                return RedirectToPage(new { SubjectId });
            }

            if (!students.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy dữ liệu sinh viên hợp lệ trong file.";
                return RedirectToPage(new { SubjectId });
            }

            try
            {
                await _subjectService.ImportStudentsAsync(SubjectId, students);
                TempData["SuccessMessage"] = $"Import thành công {students.Count} sinh viên.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi import sinh viên: {ex.Message}";
            }

            return RedirectToPage(new { SubjectId });
        }

        public bool HasLeader => AssignedLecturers.Any(l => l.IsLeader);
    }
}
