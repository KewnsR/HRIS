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

            var normalizedUsernameOrEmail = LoginInput.Username.Trim().ToLower();
            var hashedInputPassword = HashPassword(LoginInput.Password);
            var user = await _context.ApplicationUsers
                .Include(u => u.Employee)
                .SingleOrDefaultAsync(u =>
                    u.Username.ToLower() == normalizedUsernameOrEmail ||
                    (u.Employee != null && u.Employee.Email != null && u.Employee.Email.ToLower() == normalizedUsernameOrEmail));

            if (user != null)
            {
                var passwordMatchesHashed = user.Password == hashedInputPassword;
                var passwordMatchesPlainText = user.Password == LoginInput.Password;

                if (passwordMatchesHashed || passwordMatchesPlainText)
                {
                    // Automatically migrate old plaintext passwords to hashed values after a successful login.
                    if (passwordMatchesPlainText)
                    {
                        user.Password = hashedInputPassword;
                    }

                    if (string.Equals(user.Username, "admin", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = "Administrator account detected. Please use Admin Login.";
                        return Page();
                    }

                    // Replace any existing session (including admin) with the current employee session.
                    HttpContext.Session.Clear();

                    HttpContext.Session.SetString("UserName", user.Username);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetInt32("EmployeeID", user.EmployeeID);
                    HttpContext.Session.SetString("FullName", user.Employee?.FullName ?? user.Username);

                    user.LastLogin = System.DateTime.UtcNow;
                    user.FailedAttempts = 0;
                    await _context.SaveChangesAsync();

                    return RedirectToPage("/UserDashboard");
                }
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
