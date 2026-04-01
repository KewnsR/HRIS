using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HumanRepProj.Pages
{
    public class UserLoanRequestModel : PageModel
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
