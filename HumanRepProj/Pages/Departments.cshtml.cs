using HumanRepProj.Data;
using HumanRepProj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HumanRepProj.Pages
{
    public class DepartmentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DepartmentsModel> _logger;

        public DepartmentsModel(ApplicationDbContext context, ILogger<DepartmentsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<DepartmentViewModel> Departments { get; set; } = new List<DepartmentViewModel>();
        public List<Employee> DepartmentEmployees { get; set; } = new List<Employee>();
        public List<Employee> PotentialManagers { get; set; } = new List<Employee>();

        [BindProperty]
        public DepartmentInputModel Input { get; set; }

        [BindProperty]
        public int? SelectedDepartmentId { get; set; }

        [BindProperty]
        public int? SelectedManagerId { get; set; }

        public class DepartmentViewModel
        {
            public int DepartmentID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int EmployeeCount { get; set; }
            public decimal Performance { get; set; }
            public DateTime DateCreated { get; set; }
            public string FormattedDate => DateCreated.ToString("dd MMM yyyy");
            public decimal Budget { get; set; }
            public string FormattedBudget => Budget.ToString("C");
            public string Status { get; set; }
            public string StatusClass => Status == "Active" ? "bg-success" : "bg-secondary";
            public int? ManagerID { get; set; }
            public string ManagerName { get; set; }
        }

        public class DepartmentInputModel
        {
            public int? DepartmentID { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "Department name cannot exceed 50 characters.")]
            public string Name { get; set; }

            [StringLength(100, ErrorMessage = "Description cannot exceed 100 characters.")]
            public string Description { get; set; }

            [Range(0, 100, ErrorMessage = "Performance must be between 0 and 100.")]
            public decimal Performance { get; set; }

            [DataType(DataType.Date)]
            public DateTime DateCreated { get; set; } = DateTime.Now;

            [Range(0, double.MaxValue, ErrorMessage = "Budget must be a positive value.")]
            public decimal Budget { get; set; }

            [Required]
            public string Status { get; set; } = "Active";

            public int? ManagerID { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? departmentId = null)
        {
            var userEmail = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Session expired or user not logged in.");
                return RedirectToPage("/Login");
            }
            _logger.LogInformation($"User {userEmail} accessed the Departments page.");

            // Load all departments for display
            await LoadDepartments();

            // If a department ID is provided, load its context
            if (departmentId.HasValue)
            {
                SelectedDepartmentId = departmentId;

                // Fetch the department entity
                var department = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentID == departmentId);

                if (department != null)
                {
                    // Populate Input model for server-bound forms
                    Input = new DepartmentInputModel
                    {
                        DepartmentID = department.DepartmentID,
                        Name = department.Name,
                        Description = department.Description ?? string.Empty,
                        Performance = department.Performance,
                        DateCreated = department.DateCreated,
                        Budget = department.Budget,
                        Status = department.Status,
                        ManagerID = department.ManagerID
                    };
                }

                // Always load employees and potential managers if department is selected
                await LoadDepartmentEmployees(departmentId.Value);
                await LoadPotentialManagers(departmentId.Value);
            }

            Input ??= new DepartmentInputModel
            {
                DateCreated = DateTime.Today,
                Status = "Active"
            };

            return Page();
        }

        private async Task LoadDepartments()
        {
            Departments = await _context.Departments
                .Select(d => new DepartmentViewModel
                {
                    DepartmentID = d.DepartmentID,
                    Name = d.Name,
                    Description = d.Description ?? string.Empty,
                    EmployeeCount = _context.Employees.Count(e => e.DepartmentID == d.DepartmentID),
                    Performance = d.Performance,
                    DateCreated = d.DateCreated,
                    Budget = d.Budget,
                    Status = d.Status,
                    ManagerID = d.ManagerID,
                    ManagerName = d.ManagerID.HasValue ?
                        _context.Employees
                            .Where(e => e.EmployeeID == d.ManagerID)
                            .Select(e => $"{e.FirstName} {e.LastName}")
                            .FirstOrDefault() : null
                })
                .ToListAsync();
        }

        private async Task LoadDepartmentEmployees(int departmentId)
        {
            DepartmentEmployees = await _context.Employees
                .Where(e => e.DepartmentID == departmentId)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        private async Task LoadPotentialManagers(int departmentId)
        {
            // Get current department's employees who aren't already managers elsewhere
            PotentialManagers = await _context.Employees
                .Where(e => e.DepartmentID == departmentId &&
                           !_context.Departments.Any(d => d.ManagerID == e.EmployeeID))
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            Input ??= new DepartmentInputModel();
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
                await LoadDepartments();
                return Page();
            }

            var duplicateNameExists = await _context.Departments
                .AnyAsync(d => d.Name != null && d.Name.ToLower() == Input.Name.ToLower());

            if (duplicateNameExists)
            {
                ModelState.AddModelError("Input.Name", "A department with this name already exists.");
                await LoadDepartments();
                return Page();
            }

            var department = new Department
            {
                Name = Input.Name,
                Description = Input.Description,
                Performance = Input.Performance,
                DateCreated = Input.DateCreated,
                Budget = Input.Budget,
                Status = Input.Status,
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
                await LoadDepartments();
                return Page();
            }

            // If a manager was assigned, update their status
            if (Input.ManagerID.HasValue)
            {
                await UpdateEmployeeManagerStatus(Input.ManagerID.Value, true);
            }

            TempData["SuccessMessage"] = "Department created successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDepartments();
                return Page();
            }

            if (!Input.DepartmentID.HasValue)
            {
                ModelState.AddModelError(string.Empty, "Department ID is required.");
                await LoadDepartments();
                return Page();
            }

            var department = await _context.Departments.FindAsync(Input.DepartmentID.Value);
            if (department == null)
            {
                ModelState.AddModelError(string.Empty, "Department not found.");
                await LoadDepartments();
                return Page();
            }

            // Update properties
            department.Name = Input.Name;
            department.Description = Input.Description;
            department.Performance = Input.Performance;
            department.Budget = Input.Budget;
            department.Status = Input.Status;
            department.ManagerID = Input.ManagerID;

            _context.Departments.Update(department);
            await _context.SaveChangesAsync();

            // Optional: Update employee manager status
            var previousManagerId = department.ManagerID; // Already updated above
            if (previousManagerId.HasValue)
            {
                await UpdateEmployeeManagerStatus(previousManagerId.Value, true);
            }

            return RedirectToPage(); // Or use TempData["SuccessMessage"]
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department != null)
            {
                // Check if there are employees in this department
                var hasEmployees = await _context.Employees.AnyAsync(e => e.DepartmentID == id);

                if (hasEmployees)
                {
                    TempData["ErrorMessage"] = "Cannot delete department with assigned employees. Please reassign employees first.";
                    return RedirectToPage();
                }

                // If this department had a manager, update their status
                if (department.ManagerID.HasValue)
                {
                    await UpdateEmployeeManagerStatus(department.ManagerID.Value, false);
                }

                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignManagerAsync()
        {
            if (!SelectedDepartmentId.HasValue || !SelectedManagerId.HasValue)
            {
                TempData["ErrorMessage"] = "Both department and manager must be selected.";
                return RedirectToPage();
            }

            var department = await _context.Departments.FindAsync(SelectedDepartmentId.Value);
            if (department == null)
            {
                TempData["ErrorMessage"] = "Department not found.";
                return RedirectToPage();
            }

            var employee = await _context.Employees.FindAsync(SelectedManagerId.Value);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToPage();
            }

            // Check if the employee belongs to this department
            if (employee.DepartmentID != SelectedDepartmentId.Value)
            {
                TempData["ErrorMessage"] = "Selected employee doesn't belong to this department.";
                return RedirectToPage(new { departmentId = SelectedDepartmentId.Value });
            }

            // Get previous manager before updating
            var previousManagerId = department.ManagerID;

            // Update department with new manager
            department.ManagerID = SelectedManagerId.Value;
            _context.Departments.Update(department);

            // Update employee's manager status
            employee.IsManager = true;
            _context.Employees.Update(employee);

            // If there was a previous manager, update their status
            if (previousManagerId.HasValue && previousManagerId != SelectedManagerId.Value)
            {
                var previousManager = await _context.Employees.FindAsync(previousManagerId.Value);
                if (previousManager != null)
                {
                    previousManager.IsManager = false;
                    _context.Employees.Update(previousManager);
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Manager assigned successfully!";
            return RedirectToPage(new { departmentId = SelectedDepartmentId.Value });
        }

        private async Task UpdateEmployeeManagerStatus(int? employeeId, bool isManager)
        {
            if (employeeId.HasValue)
            {
                var employee = await _context.Employees.FindAsync(employeeId.Value);
                if (employee != null)
                {
                    employee.IsManager = isManager;
                    _context.Employees.Update(employee);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<JsonResult> OnGetDepartmentDetailsAsync(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return new JsonResult(new { success = false, message = "Department not found." });
            }

            return new JsonResult(new
            {
                success = true,
                department = new
                {
                    department.DepartmentID,
                    department.Name,
                    Description = department.Description ?? string.Empty,
                    department.Performance,
                    department.DateCreated,
                    department.Budget,
                    department.Status,
                    department.ManagerID
                }
            });
        }

        public async Task<JsonResult> OnGetDepartmentEmployeesAsync(int departmentId)
        {
            var employees = await _context.Employees
                .Where(e => e.DepartmentID == departmentId)
                .Select(e => new
                {
                    e.EmployeeID,
                    e.FirstName,
                    e.LastName,
                    e.Position,
                    e.IsManager
                })
                .ToListAsync();

            return new JsonResult(new { success = true, employees });
        }

        public async Task<JsonResult> OnGetPotentialManagersAsync(int departmentId)
        {
            var managers = await _context.Employees
                .Where(e => e.DepartmentID == departmentId && !e.IsManager)
                .Select(e => new
                {
                    e.EmployeeID,
                    FullName = $"{e.FirstName} {e.LastName}"
                })
                .ToListAsync();

            return new JsonResult(new { success = true, managers });
        }

        public IActionResult OnPostLogout()
        {
            _logger.LogInformation("User logged out.");
            HttpContext.Session.Clear(); // Clear session
            return RedirectToPage("/Login");
        }
    }
}