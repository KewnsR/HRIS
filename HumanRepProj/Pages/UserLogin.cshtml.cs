using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HumanRepProj.Data;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HumanRepProj.Pages
{
    public class UserLoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public UserLoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LoginInputModel LoginInput { get; set; } = new LoginInputModel();

        [TempData]
        public string? ErrorMessage { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            ErrorMessage = null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var normalizedUsername = LoginInput.Username.Trim().ToLower();
            var hashedInputPassword = HashPassword(LoginInput.Password);
            var user = await _context.ApplicationUsers
                .Include(u => u.Employee)
                .SingleOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

            if (user != null && user.Password == hashedInputPassword)
            {
                HttpContext.Session.SetString("UserName", user.Username);
                HttpContext.Session.SetInt32("EmployeeID", user.EmployeeID);
                HttpContext.Session.SetString("FullName", user.Employee?.FullName ?? user.Username);

                user.LastLogin = System.DateTime.UtcNow;
                user.FailedAttempts = 0;
                await _context.SaveChangesAsync();

                return RedirectToPage("/UserDashboard");
            }

            if (user != null)
            {
                user.FailedAttempts += 1;
                await _context.SaveChangesAsync();
            }

            ErrorMessage = "Invalid username or password";
            return Page();
        }

        private static string HashPassword(string plainTextPassword)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextPassword));
            return System.Convert.ToHexString(bytes);
        }
    }

    public class LoginInputModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
