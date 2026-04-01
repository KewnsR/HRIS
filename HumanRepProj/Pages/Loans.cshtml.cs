using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using HumanRepProj.Data;
using System;
using System.Linq;

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

        public IList<Loans> LoansList { get; set; }

        public async Task OnGetAsync()
        {
            await LoadLoansAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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

            return RedirectToPage("./Loans"); // Refresh page
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