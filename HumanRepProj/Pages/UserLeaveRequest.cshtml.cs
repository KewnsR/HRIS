using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HumanRepProj.Pages
{
    public class UserLeaveRequestModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToPage("/UserLogin");
            }

            return Page();
        }
    }
}
