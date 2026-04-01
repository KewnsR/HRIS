using HumanRepProj.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HumanRepProj.Pages
{
    public class UserLeaveRequestModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserLeaveRequestModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LeaveRequestInput Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public List<LeaveRequest> RecentRequests { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToPage("/UserLogin");
            }

            await LoadRecentRequestsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToPage("/UserLogin");
            }

            await LoadRecentRequestsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Input.EndDate < Input.StartDate)
            {
                ModelState.AddModelError("Input.EndDate", "End date must be on or after start date.");
                return Page();
            }

            var employeeId = HttpContext.Session.GetInt32("EmployeeID") ?? 0;
            var fullName = HttpContext.Session.GetString("FullName")
                ?? HttpContext.Session.GetString("UserName")
                ?? "Employee";

            var request = new LeaveRequest
            {
                EmployeeID = employeeId,
                EmployeeName = fullName,
                LeaveType = Input.LeaveType,
                StartDate = Input.StartDate,
                EndDate = Input.EndDate,
                Reason = Input.Reason,
                ContactDuringLeave = Input.ContactDuringLeave,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            _context.LeaveRequests.Add(request);
            await _context.SaveChangesAsync();

            SuccessMessage = "Your leave request was submitted successfully.";
            return RedirectToPage();
        }

        private async Task LoadRecentRequestsAsync()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (!employeeId.HasValue)
            {
                RecentRequests = new List<LeaveRequest>();
                return;
            }

            RecentRequests = await _context.LeaveRequests
                .AsNoTracking()
                .Where(r => r.EmployeeID == employeeId.Value)
                .OrderByDescending(r => r.SubmittedAt)
                .Take(8)
                .ToListAsync();
        }

        public class LeaveRequestInput
        {
            [Required(ErrorMessage = "Please select a leave type.")]
            public string LeaveType { get; set; } = string.Empty;

            [DataType(DataType.Date)]
            [Required(ErrorMessage = "Please select a start date.")]
            public DateTime StartDate { get; set; }

            [DataType(DataType.Date)]
            [Required(ErrorMessage = "Please select an end date.")]
            public DateTime EndDate { get; set; }

            [Required(ErrorMessage = "Please provide a reason.")]
            [StringLength(300, MinimumLength = 8, ErrorMessage = "Reason must be between 8 and 300 characters.")]
            public string Reason { get; set; } = string.Empty;

            [Required(ErrorMessage = "Please provide a contact number.")]
            [StringLength(30)]
            public string ContactDuringLeave { get; set; } = string.Empty;
        }
    }
}
