using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartEdu.Business.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SmartEdu.RazorWeb.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IAccountService _accountService;

        public LoginModel(
            IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // If session already contains UserId, redirect to homepage
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user =
                await _accountService
                    .AuthenticateAsync(
                        Username,
                        Password);

            if (user == null)
            {
                ErrorMessage =
                    "Sai tên đăng nhập hoặc mật khẩu";

                return Page();
            }

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role.ToString());

            // Check if user requires password change and set session marker
            var requires = false;
            try
            {
                requires = await _accountService.RequiresPasswordChangeAsync(user.Id);
                HttpContext.Session.SetString("RequirePasswordChange", requires ? "true" : "false");
            }
            catch
            {
                // ignore
            }

            // Build claims and sign-in cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("RequirePasswordChange", requires ? "true" : "false")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (requires)
            {
                return RedirectToPage("/Account/ChangePassword");
            }

            return Redirect("/");
        }
    }
}