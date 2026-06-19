using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartEdu.Business.Interfaces;
using SmartEdu.DataAccess.EntityModels;

namespace SmartEdu.RazorWeb.Filters
{
    public class SessionAuthorizeFilter : IAsyncPageFilter
    {
        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
        {
            return Task.CompletedTask;
        }

        public Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            // Allow access to /Account/Login and static assets
            var path = context.HttpContext.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/Account/Login", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/css", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/js", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/lib", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/favicon.ico", System.StringComparison.OrdinalIgnoreCase))
            {
                return next();
            }

            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                context.Result = new RedirectToPageResult("/Account/Login");
                return Task.CompletedTask;
            }

            // If the request is to ChangePassword or Logout, allow
            if (path.StartsWith("/Account/ChangePassword", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/Logout", System.StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("/Account/AccessDenied", System.StringComparison.OrdinalIgnoreCase))
            {
                return next();
            }

            // Check session flag to short-circuit DB lookups. If not present, we may query DB once and set session flag.
            var requireChange = context.HttpContext.Session.GetString("RequirePasswordChange");
            if (string.Equals(requireChange, "true", System.StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new RedirectToPageResult("/Account/ChangePassword");
                return Task.CompletedTask;
            }

            // No session marker: query DB once and set session value to avoid repeated DB hits
            try
            {
                var unitOfWork = context.HttpContext.RequestServices.GetService(typeof(IUnitOfWork)) as IUnitOfWork;
                if (unitOfWork != null)
                {
                    var user = unitOfWork.Users.GetByIdAsync(userId.Value).GetAwaiter().GetResult();
                    if (user != null && user.RequirePasswordChange)
                    {
                        context.HttpContext.Session.SetString("RequirePasswordChange", "true");
                        context.Result = new RedirectToPageResult("/Account/ChangePassword");
                        return Task.CompletedTask;
                    }
                    else
                    {
                        context.HttpContext.Session.SetString("RequirePasswordChange", "false");
                    }
                }
            }
            catch
            {
                // ignore errors; allow request to proceed
            }

            return next();
        }
    }
}
