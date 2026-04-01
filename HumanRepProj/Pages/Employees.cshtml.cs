using HumanRepProj.Data;
using HumanRepProj.Models;
using HumanRepProj.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class EmployeesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int DefaultPageSize = 10;
        private const int MaxPageSize = 50;

        public EmployeesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public PaginatedList<Employee> Employees { get; private set; } = null!;
        public int TotalPages { get; private set; }
        public bool IsAdmin { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        [Range(1, MaxPageSize)]
        public int PageSize { get; set; } = DefaultPageSize;

        [BindProperty(SupportsGet = true)]
        [StringLength(100)]
        public string SearchTerm { get; set; } = string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var guardResult = AdminSessionGuard.EnsureAdmin(this);
            if (guardResult != null)
            {
                return guardResult;
            }

            IsAdmin = true;
            var employeesQuery = BuildQuery();
            await LoadEmployeesAsync(employeesQuery);
            return Page();
        }

        private IQueryable<Employee> BuildQuery()
        {
            IQueryable<Employee> query = _context.Employees
                .Include(e => e.Department)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(e =>
                    EF.Functions.Like(e.FirstName, $"%{SearchTerm}%") ||
                    EF.Functions.Like(e.LastName, $"%{SearchTerm}%") ||
                    (e.Email != null && EF.Functions.Like(e.Email, $"%{SearchTerm}%")) ||
                    EF.Functions.Like(e.Position, $"%{SearchTerm}%") ||
                    (e.Department != null && EF.Functions.Like(e.Department.Name, $"%{SearchTerm}%")));
            }

            return query.OrderBy(e => e.LastName)
                      .ThenBy(e => e.FirstName);
        }

        private async Task LoadEmployeesAsync(IQueryable<Employee> query)
        {
            var totalItems = await query.CountAsync();
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Ensure CurrentPage is within valid range
            CurrentPage = Math.Clamp(CurrentPage, 1, TotalPages > 0 ? TotalPages : 1);

            Employees = await PaginatedList<Employee>.CreateAsync(
                query,
                CurrentPage,
                PageSize);
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                StatusMessage = "Only administrators can change employee status.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.Status = employee.Status == "Active" ? "Inactive" : "Active";
            await _context.SaveChangesAsync();

            StatusMessage = $"Employee {employee.FullName} status updated to {employee.Status}";
            return RedirectToPage(new { CurrentPage, SearchTerm });
        }

        // Handles logout from the form
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Clear session and sign out
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();

            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                StatusMessage = "Only administrators can delete employees.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            StatusMessage = $"Employee {employee.FullName} has been deleted";
            return RedirectToPage(new { CurrentPage, SearchTerm });
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(int id)
        {
            if (!IsCurrentUserAdmin())
            {
                StatusMessage = "Only administrators can reset employee passwords.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                StatusMessage = "Employee not found.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            var user = await _context.ApplicationUsers
                .FirstOrDefaultAsync(u => u.EmployeeID == id);

            if (user == null)
            {
                var baseUsername = BuildBaseUsername(employee);
                var username = await GenerateUniqueUsernameAsync(baseUsername);

                user = new ApplicationUser
                {
                    EmployeeID = employee.EmployeeID,
                    Username = username,
                    Password = string.Empty,
                    FailedAttempts = 0,
                    IsLocked = false
                };

                _context.ApplicationUsers.Add(user);
            }

            const string temporaryPassword = "Password123!";
            user.Password = HashPassword(temporaryPassword);
            user.FailedAttempts = 0;
            user.IsLocked = false;

            await _context.SaveChangesAsync();

            StatusMessage = $"Password reset for {employee.FullName} (username: {user.Username}). Temporary password: {temporaryPassword}";
            return RedirectToPage(new { CurrentPage, SearchTerm });
        }

        private static string BuildBaseUsername(Employee employee)
        {
            if (!string.IsNullOrWhiteSpace(employee.Email) && employee.Email.Contains('@'))
            {
                return employee.Email.Split('@')[0].ToLowerInvariant();
            }

            var first = (employee.FirstName ?? string.Empty).Trim().ToLowerInvariant();
            var last = (employee.LastName ?? string.Empty).Trim().ToLowerInvariant();
            var baseName = $"{first}.{last}".Trim('.');

            return string.IsNullOrWhiteSpace(baseName) ? $"emp{employee.EmployeeID}" : baseName;
        }

        private async Task<string> GenerateUniqueUsernameAsync(string baseUsername)
        {
            var sanitizedBase = string.IsNullOrWhiteSpace(baseUsername) ? "employee" : baseUsername;
            var candidate = sanitizedBase;
            var suffix = 1;

            while (await _context.ApplicationUsers.AnyAsync(u => u.Username.ToLower() == candidate.ToLower()))
            {
                candidate = $"{sanitizedBase}{suffix}";
                suffix++;
            }

            return candidate;
        }

        public IActionResult OnPostAddEmployee()
        {
            if (!IsCurrentUserAdmin())
            {
                StatusMessage = "Only administrators can add employees.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            return RedirectToPage("/UserRegister");
        }

        private bool IsCurrentUserAdmin()
        {
            return AdminSessionGuard.IsAdmin(HttpContext);
        }

            private static string HashPassword(string plainTextPassword)
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextPassword));
                return Convert.ToHexString(bytes);
            }

        public async Task<IActionResult> OnPostDeletePlaceholderEmployeesAsync()
        {
            if (!IsCurrentUserAdmin())
            {
                StatusMessage = "Only administrators can delete placeholder employees.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            var fakeFirstNames = new[] { "john", "jane", "test", "demo", "sample", "dummy" };
            var fakeLastNames = new[] { "doe", "smith", "user", "employee", "test", "sample" };

            var employeesToDelete = await _context.Employees
                .Where(e =>
                    (e.Email != null && (
                        EF.Functions.Like(e.Email, "%@example.com") ||
                        EF.Functions.Like(e.Email, "%test%") ||
                        EF.Functions.Like(e.Email, "%demo%") ||
                        EF.Functions.Like(e.Email, "%fake%") ||
                        EF.Functions.Like(e.Email, "%sample%")
                    )) ||
                    fakeFirstNames.Contains(e.FirstName.ToLower()) ||
                    fakeLastNames.Contains(e.LastName.ToLower()))
                .ToListAsync();

            if (employeesToDelete.Count == 0)
            {
                StatusMessage = "No placeholder employees were found.";
                return RedirectToPage(new { CurrentPage, SearchTerm });
            }

            _context.Employees.RemoveRange(employeesToDelete);
            await _context.SaveChangesAsync();

            StatusMessage = $"Removed {employeesToDelete.Count} placeholder employee(s).";
            return RedirectToPage(new { CurrentPage, SearchTerm });
        }
    }

    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; }
        public int TotalPages { get; }
        public int PageSize { get; }
        public int TotalCount { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            PageSize = pageSize;
            TotalCount = count;
            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(
            IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}