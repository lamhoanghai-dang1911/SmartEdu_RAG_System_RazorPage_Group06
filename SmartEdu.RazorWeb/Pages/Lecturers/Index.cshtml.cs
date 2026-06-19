using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using SmartEdu.Shared.Enums;

namespace SmartEdu.RazorWeb.Pages.Lecturers
{
    public class IndexModel : PageModel
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(IAccountService accountService, ILogger<IndexModel> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        public IEnumerable<UserDto> Lecturers { get; set; } = new List<UserDto>();

        [BindProperty]
        public UserCreateDto NewLecturer { get; set; } = new UserCreateDto();

        [BindProperty]
        public UserDto EditLecturer { get; set; } = new UserDto();

        public async Task OnGetAsync()
        {
            var all = await _accountService.GetAllUsersAsync();
            Lecturers = all.Where(u => u.Role == UserRole.Lecturer && !string.IsNullOrEmpty(u.Username));
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                ViewData["OpenCreateModal"] = "true";
                return Page();
            }

            try
            {
                NewLecturer.Role = UserRole.Lecturer;
                await _accountService.CreateUserAsync(NewLecturer);
                TempData["SuccessMessage"] = "Lecturer created.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating lecturer");
                // Surface service exception message to the user and reopen modal
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewData["OpenCreateModal"] = "true";
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                ViewData["OpenEditModal"] = "true";
                return Page();
            }

            try
            {
                await _accountService.UpdateUserAsync(EditLecturer);
                TempData["SuccessMessage"] = "Lecturer updated.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lecturer");
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewData["OpenEditModal"] = "true";
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                await _accountService.DeleteUserAsync(id);
                TempData["SuccessMessage"] = "Lecturer deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting lecturer {Id}", id);
                TempData["ErrorMessage"] = "Unable to delete lecturer.";
            }

            return RedirectToPage();
        }
    }
}
