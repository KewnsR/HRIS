using HumanRepProj.Data;
using HumanRepProj.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace HumanRepProj.Pages
{
    public class UserProfileModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public UserProfileModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        public string UserName { get; private set; } = "User";
        public string FullName { get; private set; } = "Employee";
        public string Email { get; private set; } = string.Empty;
        public string Role { get; private set; } = "Employee";
        public string DepartmentName { get; private set; } = "Unassigned";
        public string Position { get; private set; } = "Employee";
        public string EmployeeCode { get; private set; } = "-";
        public string JoinDateText { get; private set; } = "-";
        public string Initials { get; private set; } = "EM";
        public string EmployeeQrCodeUrl { get; private set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var employee = await GetCurrentEmployeeAsync();
            if (employee == null)
            {
                return RedirectToPage("/UserLogin");
            }

            PopulateFromEmployee(employee);
            Input.FullName = employee.FullName;
            Input.Email = employee.Email ?? string.Empty;
            Input.PhoneNumber = employee.PhoneNumber ?? string.Empty;
            Input.Address = employee.Address ?? string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var employee = await GetCurrentEmployeeAsync(trackChanges: true);
            if (employee == null)
            {
                return RedirectToPage("/UserLogin");
            }

            if (!ModelState.IsValid)
            {
                PopulateFromEmployee(employee);
                return Page();
            }

            var nameParts = (Input.FullName ?? string.Empty).Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            employee.FirstName = nameParts.Length > 0 ? nameParts[0] : employee.FirstName;
            employee.LastName = nameParts.Length > 1 ? nameParts[1] : employee.LastName;
            employee.Email = (Input.Email ?? string.Empty).Trim();
            employee.PhoneNumber = (Input.PhoneNumber ?? string.Empty).Trim();
            employee.Address = (Input.Address ?? string.Empty).Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToPage();
        }

        private async Task<HumanRepProj.Models.Employee?> GetCurrentEmployeeAsync(bool trackChanges = false)
        {
            var sessionUser = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrWhiteSpace(sessionUser))
            {
                return null;
            }

            UserName = sessionUser;

            var employeeId = HttpContext.Session.GetInt32("EmployeeID");
            IQueryable<HumanRepProj.Models.Employee> employeeQuery = _context.Employees
                .Include(e => e.Department);

            if (!trackChanges)
            {
                employeeQuery = employeeQuery.AsNoTracking();
            }

            if (employeeId.HasValue)
            {
                return await employeeQuery.FirstOrDefaultAsync(e => e.EmployeeID == employeeId.Value);
            }

            var user = await _context.ApplicationUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == sessionUser);

            if (user == null)
            {
                return null;
            }

            return await employeeQuery.FirstOrDefaultAsync(e => e.EmployeeID == user.EmployeeID);
        }

        private void PopulateFromEmployee(HumanRepProj.Models.Employee employee)
        {
            FullName = employee.FullName;
            Email = employee.Email ?? string.Empty;
            Position = string.IsNullOrWhiteSpace(employee.Position) ? "Employee" : employee.Position;
            Role = Position;
            DepartmentName = employee.Department?.Name ?? "Unassigned";
            EmployeeCode = $"EMP-{employee.EmployeeID:D5}";
            JoinDateText = employee.DateHired == default ? "-" : employee.DateHired.ToString("MMMM dd, yyyy");
            Initials = BuildInitials(employee.FullName);
            EmployeeQrCodeUrl = EmployeeQrCodeService.EnsureQrCodeForEmployee(employee.EmployeeID, _environment.WebRootPath);
        }

        private static string BuildInitials(string fullName)
        {
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "EM";
            }

            if (parts.Length == 1)
            {
                return parts[0][0].ToString().ToUpper();
            }

            return string.Concat(parts[0][0], parts[1][0]).ToUpper();
        }
    }

    public class ProfileInputModel
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}