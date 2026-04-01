using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class EditEmployeeModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditEmployeeModel> _logger;

        [BindProperty]
        public Employee Employee { get; set; }

        // Dropdown lists
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Managers { get; set; } = new List<SelectListItem>();

        // Static options
        public List<SelectListItem> GenderOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Male", Text = "Male" },
            new SelectListItem { Value = "Female", Text = "Female" },
            new SelectListItem { Value = "Other", Text = "Other" }
        };

        public List<SelectListItem> EmploymentTypeOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Full-time", Text = "Full-time" },
            new SelectListItem { Value = "Part-time", Text = "Part-time" },
            new SelectListItem { Value = "Contract", Text = "Contract" }
        };

        public List<SelectListItem> StatusOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Active", Text = "Active" },
            new SelectListItem { Value = "On Leave", Text = "On Leave" },
            new SelectListItem { Value = "Terminated", Text = "Terminated" }
        };

        public EditEmployeeModel(ApplicationDbContext context, ILogger<EditEmployeeModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this, _logger);
            if (guardResult != null)
            {
                return guardResult;
            }

            try
            {
                Employee = await _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Manager)
                    .FirstOrDefaultAsync(e => e.EmployeeID == id);

                if (Employee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToPage("./Index");
                }

                await PopulateDropdowns();

                // Set default department if none selected and departments exist
                if (Employee.DepartmentID == 0 && Departments.Any())
                {
                    Employee.DepartmentID = int.Parse(Departments.First().Value);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee for editing");
                TempData["ErrorMessage"] = "Error loading employee data";
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this, _logger);
            if (guardResult != null)
            {
                return guardResult;
            }

            try
            {
                // Remove unnecessary model state validation
                ModelState.Remove("Employee.Subordinates");
                ModelState.Remove("Employee.Manager");
                ModelState.Remove("Employee.Department");

                // Department validation
                if (!await _context.Departments.AnyAsync())
                {
                    ModelState.AddModelError("", "No departments available in system");
                }
                else if (Employee.DepartmentID == 0)
                {
                    ModelState.AddModelError("Employee.DepartmentID", "Please select a department");
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns();
                    TempData["ErrorMessage"] = "Please correct the errors and try again.";
                    return Page();
                }

                var existingEmployee = await _context.Employees
                    .AsTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeID == Employee.EmployeeID);

                if (existingEmployee == null)
                {
                    TempData["ErrorMessage"] = "Employee not found";
                    return RedirectToPage("./Index");
                }

                // Update employee properties
                existingEmployee.FirstName = Employee.FirstName;
                existingEmployee.LastName = Employee.LastName;
                existingEmployee.Email = Employee.Email;
                existingEmployee.PhoneNumber = Employee.PhoneNumber;
                existingEmployee.Address = Employee.Address;
                existingEmployee.DateOfBirth = Employee.DateOfBirth;
                existingEmployee.Gender = Employee.Gender;
                existingEmployee.DepartmentID = Employee.DepartmentID;
                existingEmployee.Position = Employee.Position;
                existingEmployee.Salary = Employee.Salary;
                existingEmployee.ManagerID = Employee.ManagerID;
                existingEmployee.EmploymentType = Employee.EmploymentType;
                existingEmployee.Status = Employee.Status;
                existingEmployee.IsManager = Employee.IsManager;
                existingEmployee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Employee updated successfully";
                return RedirectToPage("./ViewEmployee", new { id = Employee.EmployeeID });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating employee");
                TempData["ErrorMessage"] = "Error saving to database. Please try again.";
                await PopulateDropdowns();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating employee");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again.";
                await PopulateDropdowns();
                return Page();
            }
        }

        private async Task PopulateDropdowns()
        {
            try
            {
                // Get active departments with employee count
                Departments = await _context.Departments
                    .Where(d => d.Status == "Active")
                    .OrderBy(d => d.Name)
                    .Select(d => new SelectListItem
                    {
                        Value = d.DepartmentID.ToString(),
                        Text = $"{d.Name}",
                        Selected = d.DepartmentID == Employee.DepartmentID
                    })
                    .ToListAsync();

                if (!Departments.Any())
                {
                    _logger.LogWarning("No active departments available");
                    ModelState.AddModelError("", "No active departments available");
                }

                // Get available managers (excluding current employee)
                Managers = await _context.Employees
                    .Where(e => e.IsManager &&
                               e.EmployeeID != Employee.EmployeeID &&
                               e.Status == "Active")
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EmployeeID.ToString(),
                        Text = $"{e.LastName}, {e.FirstName} ({e.Position})",
                        Selected = e.EmployeeID == Employee.ManagerID
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns");
                ModelState.AddModelError("", "Error loading department/manager data");
            }
        }
    }
}