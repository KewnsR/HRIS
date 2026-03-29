using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using HumanRepProj.Data;
using System.Linq;


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
            // Initialize any necessary data here
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Invalid login attempt.";
                return Page();
            }

            var user = _context.ApplicationUsers.SingleOrDefault(u => u.Username == Input.Email); // Assuming Email is used for Username

         

            if (user != null && Input.Password == user.Password) // Compare plaintext passwords
            {
                // Set session or cookie here
                HttpContext.Session.SetString("Username", Input.Email);
                return RedirectToPage("/Dashboard"); // Correct redirect format

            }

            ErrorMessage = "Invalid login attempt.";
            return Page();
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
