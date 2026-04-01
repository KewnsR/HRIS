using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HumanRepProj.Data;
using HumanRepProj.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class UserRegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserRegisterModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public RegisterInputModel Input { get; set; } = new();

        public List<SelectListItem> DepartmentOptions { get; private set; } = new();

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Only administrators can create employee accounts.";
                return RedirectToPage("/UserLogin");
            }

            await LoadDepartmentsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!IsCurrentUserAdmin())
            {
                TempData["ErrorMessage"] = "Only administrators can create employee accounts.";
                return RedirectToPage("/UserLogin");
            }

            await LoadDepartmentsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var usernameExists = await _context.ApplicationUsers
                .AnyAsync(u => u.Username.ToLower() == Input.Username.ToLower());

            if (usernameExists)
            {
                ModelState.AddModelError("Input.Username", "Username is already taken.");
                return Page();
            }

            var emailExists = await _context.Employees
                .AnyAsync(e => e.Email != null && e.Email.ToLower() == Input.Email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError("Input.Email", "Email is already registered.");
                return Page();
            }

            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentID == Input.DepartmentID);
            if (!departmentExists)
            {
                ModelState.AddModelError("Input.DepartmentID", "Please select a valid department.");
                return Page();
            }

            var employee = new Employee
            {
                FirstName = Input.FirstName.Trim(),
                LastName = Input.LastName.Trim(),
                Email = Input.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(),
                Address = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim(),
                DateOfBirth = Input.DateOfBirth,
                Gender = Input.Gender,
                DepartmentID = Input.DepartmentID,
                Position = Input.Position.Trim(),
                Salary = Input.Salary,
                DateHired = Input.DateHired,
                EmploymentType = Input.EmploymentType,
                Status = "Active"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                EmployeeID = employee.EmployeeID,
                Username = Input.Username.Trim(),
                Password = HashPassword(Input.Password)
            };

            _context.ApplicationUsers.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Account created successfully. You can now log in.";
            return RedirectToPage("/UserLogin");
        }

        private async Task LoadDepartmentsAsync()
        {
            DepartmentOptions = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.DepartmentID.ToString(),
                    Text = d.Name
                })
                .ToListAsync();
        }

        private bool IsCurrentUserAdmin()
        {
            var username = HttpContext.Session.GetString("Username")
                ?? HttpContext.Session.GetString("UserName");

            return !string.IsNullOrWhiteSpace(username)
                && string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase);
        }

            private static string HashPassword(string plainTextPassword)
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextPassword));
                return Convert.ToHexString(bytes);
            }

        public class RegisterInputModel
        {
            [Required(ErrorMessage = "First name is required")]
            [StringLength(100)]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Last name is required")]
            [StringLength(100)]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress]
            [StringLength(255)]
            public string Email { get; set; } = string.Empty;

            [StringLength(20)]
            public string? PhoneNumber { get; set; }

            [StringLength(500)]
            public string? Address { get; set; }

            [Required(ErrorMessage = "Username is required")]
            [StringLength(50)]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirm password is required")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Date of birth is required")]
            [DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            [Required(ErrorMessage = "Gender is required")]
            [StringLength(10)]
            public string Gender { get; set; } = "Male";

            [Required(ErrorMessage = "Department is required")]
            public int DepartmentID { get; set; }

            [Required(ErrorMessage = "Position is required")]
            [StringLength(100)]
            public string Position { get; set; } = string.Empty;

            [Range(0, double.MaxValue, ErrorMessage = "Salary must be positive")]
            public decimal Salary { get; set; } = 0;

            [Required(ErrorMessage = "Hire date is required")]
            [DataType(DataType.Date)]
            public DateTime DateHired { get; set; } = DateTime.Today;

            [StringLength(20)]
            public string EmploymentType { get; set; } = "Full-time";
        }
    }
}