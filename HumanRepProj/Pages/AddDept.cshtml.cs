using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using HumanRepProj.Data;
using HumanRepProj.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HumanRepProj.Pages
{
    public class AddDeptModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddDeptModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public SelectList Managers { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(50, ErrorMessage = "Department name cannot exceed 50 characters.")]
            public string Name { get; set; }

            [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
            public string Description { get; set; }

            [Range(0, 100, ErrorMessage = "Performance must be between 0 and 100.")]
            public decimal Performance { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive number.")]
            public decimal Budget { get; set; }

            [DataType(DataType.Date)]
            public DateTime DateCreated { get; set; } = DateTime.Now;

            [Required]
            public string Status { get; set; } = "Active";

            public int? ManagerID { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Input ??= new InputModel
            {
                DateCreated = DateTime.Today,
                Status = "Active"
            };

            // Get all employees who could be managers
            var employees = await _context.Employees
                .Where(e => e.Status == "Active")
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .Select(e => new
                {
                    EmployeeID = e.EmployeeID,
                    FullName = e.FirstName + " " + e.LastName
                })
                .ToListAsync();

            Managers = new SelectList(employees, "EmployeeID", "FullName");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input ??= new InputModel();
            Input.Name = (Input.Name ?? string.Empty).Trim();
            Input.Status = string.IsNullOrWhiteSpace(Input.Status) ? "Active" : Input.Status.Trim();

            if (Input.DateCreated == default)
            {
                Input.DateCreated = DateTime.Today;
            }

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Department name is required.");
            }

            if (!ModelState.IsValid)
            {
                // Reload the manager list if validation fails
                await OnGetAsync();
                return Page();
            }

            var duplicateNameExists = await _context.Departments
                .AnyAsync(d => d.Name != null && d.Name.ToLower() == Input.Name.ToLower());

            if (duplicateNameExists)
            {
                ModelState.AddModelError("Input.Name", "A department with this name already exists.");
                await OnGetAsync();
                return Page();
            }

            var department = new Department
            {
                Name = Input.Name,
                Description = Input.Description,
                Performance = Input.Performance,
                Budget = Input.Budget,
                Status = Input.Status,
                DateCreated = Input.DateCreated,
                ManagerID = Input.ManagerID
            };

            try
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Could not save the department. Please verify the values and try again.");
                await OnGetAsync();
                return Page();
            }

            TempData["SuccessMessage"] = "Department created successfully.";
            return RedirectToPage("/Departments");
        }
    }
}