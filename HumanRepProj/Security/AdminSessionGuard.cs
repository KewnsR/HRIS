using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HumanRepProj.Security
{
    public static class AdminSessionGuard
    {
        public static bool IsAdmin(HttpContext httpContext)
        {
            var username = GetUsername(httpContext);
            return !string.IsNullOrWhiteSpace(username) &&
                   string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);
        }

        public static string? GetUsername(HttpContext httpContext)
        {
            return httpContext.Session.GetString("Username")
                ?? httpContext.Session.GetString("UserName");
        }

        public static IActionResult? EnsureAdmin(PageModel pageModel, ILogger? logger = null)
        {
            var username = GetUsername(pageModel.HttpContext);
            if (string.IsNullOrWhiteSpace(username))
            {
                logger?.LogWarning("Session expired or user not logged in.");
                return pageModel.RedirectToPage("/Login");
            }

            if (!string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("Non-admin user attempted to access admin page: {Username}", username);
                pageModel.HttpContext.Session.Clear();
                pageModel.TempData["ErrorMessage"] = "Please use the Employee Login page.";
                return pageModel.RedirectToPage("/UserLogin");
            }

            pageModel.HttpContext.Session.SetString("Username", username);
            pageModel.HttpContext.Session.SetString("UserName", username);
            return null;
        }
    }
}
