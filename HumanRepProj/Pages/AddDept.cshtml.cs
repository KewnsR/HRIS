using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
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

        public AddDeptModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<SelectListItem> DepartmentTemplates { get; set; } = new();

        public class InputModel
        {
            public string? TemplateKey { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "Department name cannot exceed 50 characters.")]
            public string Name { get; set; }

            [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters.")]
            public string Description { get; set; }
        }

        public IActionResult OnGet()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            Input ??= new InputModel();
            PopulateTemplates();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            Input ??= new InputModel();

            ApplyTemplateValues();
            Input.Name = (Input.Name ?? string.Empty).Trim();
            Input.Description = (Input.Description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Department name is required.");
            }

            if (!ModelState.IsValid)
            {
                OnGet();
                return Page();
            }

            var duplicateNameExists = await _context.Departments
                .AnyAsync(d => d.Name != null && d.Name.ToLower() == Input.Name.ToLower());

            if (duplicateNameExists)
            {
                ModelState.AddModelError("Input.Name", "A department with this name already exists.");
                OnGet();
                return Page();
            }

            var department = new Department
            {
                Name = Input.Name,
                Description = Input.Description,
                Performance = 0,
                Budget = 0,
                Status = "Active",
                DateCreated = DateTime.UtcNow,
                ManagerID = null
            };

            try
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Could not save the department. Please verify the values and try again.");
                OnGet();
                return Page();
            }

            TempData["SuccessMessage"] = "Department created successfully.";
            return RedirectToPage("/Departments");
        }

        private void PopulateTemplates()
        {
            DepartmentTemplates = TemplateDefinitions
                .Select(t => new SelectListItem
                {
                    Value = t.Key,
                    Text = t.Value.Name,
                    Selected = string.Equals(Input?.TemplateKey, t.Key, StringComparison.OrdinalIgnoreCase)
                })
                .ToList();
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

        public class DepartmentTemplateDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}