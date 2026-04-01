using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using HumanRepProj.Security;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class SettingsModel : PageModel
    {
        [BindProperty]
        public string AdminEmail { get; set; }
        [BindProperty]
        public string PasswordPolicy { get; set; }
        [BindProperty]
        public string Timezone { get; set; }
        [BindProperty]
        public string DateFormat { get; set; }
        [BindProperty]
        public string Language { get; set; }
        [BindProperty]
        public string Currency { get; set; }
        [BindProperty]
        public bool EmailNotifications { get; set; }
        [BindProperty]
        public bool SmsNotifications { get; set; }
        [BindProperty]
        public string NotificationFrequency { get; set; }
        [BindProperty]
        public string CompanyName { get; set; }
        [BindProperty]
        public string CompanyAddress { get; set; }
        [BindProperty]
        public string ContactInfo { get; set; }
        [BindProperty]
        public IFormFile LogoUpload { get; set; }
        [BindProperty]
        public string WorkHours { get; set; }
        [BindProperty]
        public string OvertimePolicy { get; set; }
        [BindProperty]
        public string LeavePolicy { get; set; }
        [BindProperty]
        public string HolidayCalendar { get; set; }
        [BindProperty]
        public string SalaryComponents { get; set; }
        [BindProperty]
        public string TaxSettings { get; set; }
        [BindProperty]
        public string PayrollCycle { get; set; }
        [BindProperty]
        public string DeductionsBenefits { get; set; }
        [BindProperty]
        public bool TwoFactorAuth { get; set; }
        [BindProperty]
        public int SessionTimeout { get; set; }
        [BindProperty]
        public string IpWhitelisting { get; set; }
        [BindProperty]
        public string DataBackup { get; set; }
        [BindProperty]
        public string ThirdPartyIntegration { get; set; }
        [BindProperty]
        public string ApiKeys { get; set; }
        [BindProperty]
        public string Webhooks { get; set; }
        [BindProperty]
        public string Theme { get; set; }
        [BindProperty]
        public string CustomFields { get; set; }
        [BindProperty]
        public string DashboardCustomization { get; set; }


        public IActionResult OnGet()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Save settings logic here

            return RedirectToPage("Settings");
        }

        // ✅ Proper logout logic
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            HttpContext.Session.Clear(); // Clear session
            await HttpContext.SignOutAsync(); // Sign out if using authenti cation
            return RedirectToPage("/Login"); // Redirect to login page

            
        }
    }
}
