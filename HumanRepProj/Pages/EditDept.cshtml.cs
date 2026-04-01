using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System;

namespace HumanRepProj.Pages
{
    public class EditDeptModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private static readonly Dictionary<string, DepartmentTemplateDefinition> TemplateDefinitions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["it"] = new DepartmentTemplateDefinition
                {
                    Name = "IT Department",
                    Description = "Handles systems, infrastructure, software support, and cybersecurity operations."
                },
                ["hr"] = new DepartmentTemplateDefinition
                {
                    Name = "Human Resources",
                    Description = "Manages recruitment, employee records, performance processes, and workforce development."
                },
                ["finance"] = new DepartmentTemplateDefinition
                {
                    Name = "Finance",
                    Description = "Responsible for budgeting, payroll oversight, reporting, and financial controls."
                },
                ["operations"] = new DepartmentTemplateDefinition
                {
                    Name = "Operations",
                    Description = "Oversees day-to-day workflows, process execution, and service delivery."
                },
                ["sales"] = new DepartmentTemplateDefinition
                {
                    Name = "Sales",
                    Description = "Drives revenue generation, client acquisition, and account relationship management."
                }
            };

        public EditDeptModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public DepartmentInputModel Input { get; set; }

        public List<SelectListItem> Managers { get; set; } = new();
        public List<SelectListItem> DepartmentTemplates { get; set; } = new();
        public IReadOnlyDictionary<string, DepartmentTemplateDefinition> TemplateMap => TemplateDefinitions;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            var existingDepartment = await _context.Departments
                .AsTracking()
                .Include(d => d.Manager)
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (existingDepartment == null)
            {
                return NotFound();
            }

            Input = new DepartmentInputModel
            {
                DepartmentID = existingDepartment.DepartmentID,
                Name = existingDepartment.Name,
                Description = existingDepartment.Description,
                Performance = existingDepartment.Performance,
                DateCreated = existingDepartment.DateCreated,
                Budget = existingDepartment.Budget,
                Status = existingDepartment.Status,
                ManagerID = existingDepartment.ManagerID
            };

            ApplyMatchingTemplateKey();
            await PopulateDropdowns(existingDepartment.DepartmentID);
            return Page();
        }

        private async Task PopulateDropdowns(int departmentId)
        {
            DepartmentTemplates = TemplateDefinitions
                .Select(t => new SelectListItem
                {
                    Value = t.Key,
                    Text = t.Value.Name,
                    Selected = string.Equals(Input.TemplateKey, t.Key, StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            Managers = await _context.Employees
                .Where(e => e.IsManager && e.Status == "Active")
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeID.ToString(),
                    Text = $"{e.FirstName} {e.LastName}",
                    Selected = e.EmployeeID == Input.ManagerID
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            ApplyTemplateValues();
            Input.Name = (Input.Name ?? string.Empty).Trim();
            Input.Description = (Input.Description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Department name is required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(Input.DepartmentID);
                return Page();
            }

            var duplicateNameExists = await _context.Departments
                .AnyAsync(d => d.DepartmentID != Input.DepartmentID && d.Name != null && d.Name.ToLower() == Input.Name.ToLower());

            if (duplicateNameExists)
            {
                ModelState.AddModelError("Input.Name", "A department with this name already exists.");
                await PopulateDropdowns(Input.DepartmentID);
                return Page();
            }

            var department = await _context.Departments.FindAsync(Input.DepartmentID);
            if (department == null)
            {
                return NotFound();
            }

            // Update department properties
            department.Name = Input.Name;
            department.Description = Input.Description;
            department.Performance = Input.Performance;
            department.DateCreated = ToUtcDateTime(Input.DateCreated);
            department.Budget = Input.Budget;
            department.Status = Input.Status;
            department.ManagerID = Input.ManagerID;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("/Departments");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Could not save changes. Please check duplicate values and try again.");
                await PopulateDropdowns(Input.DepartmentID);
                return Page();
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "An error occurred while saving changes.");
                await PopulateDropdowns(Input.DepartmentID);
                return Page();
            }
        }

        private static DateTime ToUtcDateTime(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private void ApplyTemplateValues()
        {
            if (!string.IsNullOrWhiteSpace(Input.TemplateKey) &&
                TemplateDefinitions.TryGetValue(Input.TemplateKey, out var template))
            {
                Input.Name = template.Name;
                Input.Description = template.Description;
            }
        }

        private void ApplyMatchingTemplateKey()
        {
            var match = TemplateDefinitions.FirstOrDefault(t =>
                string.Equals((Input.Name ?? string.Empty).Trim(), t.Value.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals((Input.Description ?? string.Empty).Trim(), t.Value.Description, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(match.Key))
            {
                Input.TemplateKey = match.Key;
            }
        }

        public class DepartmentInputModel
        {
            public int DepartmentID { get; set; }

            public string? TemplateKey { get; set; }

            [Required(ErrorMessage = "Department name is required.")]
            public string Name { get; set; }

            public string Description { get; set; }

            [Range(0, 100)]
            public decimal Performance { get; set; }

            [DataType(DataType.Date)]
            public DateTime DateCreated { get; set; }

            [Range(0, double.MaxValue)]
            public decimal Budget { get; set; }

            [Required]
            public string Status { get; set; }

            public int? ManagerID { get; set; }
        }

        public class DepartmentTemplateDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}