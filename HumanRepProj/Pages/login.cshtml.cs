using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using HumanRepProj.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;


namespace HumanRepProj.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public LoginModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
            ErrorMessage = string.Empty;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }

            var normalized = (Input.Email ?? string.Empty).Trim().ToLower();
            var user = _context.ApplicationUsers
                .Include(u => u.Employee)
                .SingleOrDefault(u =>
                    u.Username.ToLower() == normalized ||
                    (u.Employee != null && u.Employee.Email != null && u.Employee.Email.ToLower() == normalized));

            if (user != null)
            {
                var hashedInputPassword = HashPassword(Input.Password);
                var passwordMatchesHashed = user.Password == hashedInputPassword;
                var passwordMatchesPlainText = user.Password == Input.Password;

                if (passwordMatchesHashed || passwordMatchesPlainText)
                {
                    if (passwordMatchesPlainText)
                    {
                        user.Password = hashedInputPassword;
                        _context.SaveChanges();
                    }

                    // Always replace any previous session so account switching works reliably.
                    HttpContext.Session.Clear();

                    if (!string.Equals(user.Username, "admin", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ErrorMessage = "This login is for administrators only. Please use Employee Login.";
                        return Page();
                    }

                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("UserName", user.Username);
                    return RedirectToPage("/Dashboard");
                }

            }

            ErrorMessage = "Invalid login attempt.";
            return Page();
        }

        private static string HashPassword(string plainTextPassword)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextPassword));
            return System.Convert.ToHexString(bytes);
        }

        public class InputModel
        {
            [Required]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }
    }
}
