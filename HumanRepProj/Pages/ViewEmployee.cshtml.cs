using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class ViewEmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public Employee Employee { get; set; }
        public decimal CurrentSalary { get; set; }
        public int LeaveBalance { get; set; }
        public List<Employee> Subordinates { get; set; } = new List<Employee>();

        public ViewEmployeeModel(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                // First get the employee by the provided id parameter
                Employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .FirstOrDefaultAsync(e => e.EmployeeID == id);

                if (Employee == null)
                {
                    return NotFound();
                }

                // Get current salary
                CurrentSalary = Employee.Salary;

                // Get subordinates if this employee is a manager
                if (Employee.IsManager)
                {
                    Subordinates = await _context.Employees
                        .Where(e => e.ManagerID == id)
                        .OrderBy(e => e.LastName)
                        .ThenBy(e => e.FirstName)
                        .ToListAsync();
                }

                return Page();
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error in ViewEmployeeModel: {ex.Message}");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}