using HumanRepProj.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HumanRepProj.Pages
{
    public class UserDashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserDashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string UserName { get; private set; } = "User";
        public string FullName { get; private set; } = "Employee";
        public string Role { get; private set; } = "Employee";
        public int AvailableLeaveDays { get; private set; } = 12;
        public int PendingTasks { get; private set; }
        public int DocumentUpdates { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var currentUser = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrWhiteSpace(currentUser))
            {
                return RedirectToPage("/UserLogin");
            }

            UserName = currentUser;
            FullName = HttpContext.Session.GetString("FullName") ?? currentUser;

            var employeeId = HttpContext.Session.GetInt32("EmployeeID");
            if (employeeId.HasValue)
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeID == employeeId.Value);

                if (employee != null)
                {
                    FullName = employee.FullName;
                    Role = string.IsNullOrWhiteSpace(employee.Position) ? "Employee" : employee.Position;

                    DocumentUpdates = string.IsNullOrWhiteSpace(employee.Address) || string.IsNullOrWhiteSpace(employee.PhoneNumber)
                        ? 1
                        : 0;
                }

                PendingTasks = await _context.Loans
                    .AsNoTracking()
                    .CountAsync(l => l.EmployeeID == employeeId.Value && l.LoanStatus == "Pending");
            }

            return Page();
        }
    }
}