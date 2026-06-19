using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using SmartEdu.Shared.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartEdu.RazorWeb.Pages.Account
{
    public class ChangePasswordModel : PageModel
    {
        private readonly IAccountService _accountService;

        public ChangePasswordModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public ChangePasswordDto Input { get; set; } = new ChangePasswordDto();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // Ensure user is authenticated and session contains UserId
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _accountService.ChangePasswordAsync(userId.Value, Input);

                // Update session marker
                HttpContext.Session.SetString("RequirePasswordChange", "false");

                // Recreate authentication cookie so RequirePasswordChange claim is updated
                var username = HttpContext.Session.GetString("Username") ?? string.Empty;
                var role = HttpContext.Session.GetString("Role") ?? string.Empty;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("RequirePasswordChange", "false")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return RedirectToPage("/Index");
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }
    }
}
