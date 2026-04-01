using HumanRepProj.Data;
using HumanRepProj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HumanRepProj.Pages
{
    public class UserLoanRequestModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserLoanRequestModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LoanRequestInput Input { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public List<Loans> RecentLoans { get; private set; } = new();
        public decimal MaxEligibility { get; private set; } = 50000m;
        public decimal UsedEligibility { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToPage("/UserLogin");
            }

            await LoadLoanDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("UserName")))
            {
                return RedirectToPage("/UserLogin");
            }

            await LoadLoanDataAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Input.LoanAmount > MaxEligibility)
            {
                ModelState.AddModelError("Input.LoanAmount", $"Loan amount cannot exceed {MaxEligibility:N2} PHP.");
                return Page();
            }

            var employeeId = await ResolveEmployeeIdAsync();
            if (employeeId == null)
            {
                ModelState.AddModelError(string.Empty, "Unable to identify your employee account.");
                return Page();
            }

            var now = DateTime.UtcNow;
            var loan = new Loans
            {
                EmployeeID = employeeId.Value,
                LoanType = Input.LoanType,
                LoanAmount = Input.LoanAmount,
                LoanStatus = "Pending",
                DateIssued = now,
                DueDate = now.AddMonths(Input.InstallmentMonths),
                PaidLoan = 0m,
                LoanTerm = Input.InstallmentMonths
            };

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            SuccessMessage = "Your loan request was submitted successfully.";
            return RedirectToPage();
        }

        private async Task LoadLoanDataAsync()
        {
            var employeeId = await ResolveEmployeeIdAsync();
            if (!employeeId.HasValue)
            {
                RecentLoans = new List<Loans>();
                UsedEligibility = 0m;
                return;
            }

            RecentLoans = await _context.Loans
                .AsNoTracking()
                .Where(l => l.EmployeeID == employeeId.Value)
                .OrderByDescending(l => l.DateIssued)
                .Take(8)
                .ToListAsync();

            UsedEligibility = RecentLoans
                .Where(l => !string.Equals(l.LoanStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                .Sum(l => Math.Max(0m, l.LoanAmount - l.PaidLoan));
        }

        private async Task<int?> ResolveEmployeeIdAsync()
        {
            var employeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (employeeId.HasValue)
            {
                return employeeId.Value;
            }

            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            var login = await _context.ApplicationUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);

            return login?.EmployeeID;
        }

        public class LoanRequestInput
        {
            [Required(ErrorMessage = "Please select a loan type.")]
            public string LoanType { get; set; } = string.Empty;

            [Range(1000, 50000, ErrorMessage = "Loan amount must be between ₱1,000 and ₱50,000.")]
            public decimal LoanAmount { get; set; }

            [Range(3, 24, ErrorMessage = "Installment period must be between 3 and 24 months.")]
            public int InstallmentMonths { get; set; }

            [Required(ErrorMessage = "Please enter a reason.")]
            [StringLength(300, MinimumLength = 8, ErrorMessage = "Reason must be between 8 and 300 characters.")]
            public string Reason { get; set; } = string.Empty;
        }
    }
}
