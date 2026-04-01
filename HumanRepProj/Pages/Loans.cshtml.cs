using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using HumanRepProj.Data;
using System;
using System.Linq;
using HumanRepProj.Security;

namespace HumanRepProj.Pages
{
    public class LoansModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoansModel(ApplicationDbContext context)
        {
            _context = context;
            Loan = new Loans();
            LoansList = new List<Loans>();
        }

        [BindProperty]
        public Loans Loan { get; set; }

        [TempData]
        public string? Message { get; set; }

        public IList<Loans> LoansList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            await LoadLoansAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            Loan ??= new Loans();

            if (!ModelState.IsValid)
            {
                await LoadLoansAsync();
                return Page();
            }

            // Set default values for new loans
            Loan.PaidLoan = 0; // New loans start with $0 paid
            Loan.LoanStatus = "Pending Approval"; // Admin sets status later

            _context.Loans.Add(Loan);
            await _context.SaveChangesAsync();

            Message = "Loan request created successfully.";

            return RedirectToPage("./Loans"); // Refresh page
        }

        public async Task<IActionResult> OnPostApproveAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan != null)
            {
                loan.LoanStatus = "Approved";
                loan.ReviewedBy = AdminSessionGuard.GetUsername(HttpContext) ?? "admin";
                loan.ReviewedAt = DateTime.UtcNow;
                loan.DecisionNote = "Approved by admin.";
                await _context.SaveChangesAsync();
                Message = "Loan request approved.";
            }

            return RedirectToPage("./Loans");
        }

        public async Task<IActionResult> OnPostRejectAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan != null)
            {
                loan.LoanStatus = "Rejected";
                loan.ReviewedBy = AdminSessionGuard.GetUsername(HttpContext) ?? "admin";
                loan.ReviewedAt = DateTime.UtcNow;
                loan.DecisionNote = "Rejected by admin.";
                await _context.SaveChangesAsync();
                Message = "Loan request rejected.";
            }

            return RedirectToPage("./Loans");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            var loan = await _context.Loans.FirstOrDefaultAsync(l => l.LoanID == id);
            if (loan != null)
            {
                _context.Loans.Remove(loan);
                await _context.SaveChangesAsync();
                Message = "Loan request deleted.";
            }

            return RedirectToPage("./Loans");
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Login");
        }

        private async Task LoadLoansAsync()
        {
            LoansList = await _context.Loans
                .OrderByDescending(l => l.DateIssued)
                .ToListAsync();
        }
    }
}