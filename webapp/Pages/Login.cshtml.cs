using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;
using Microsoft.AspNetCore.Identity;

namespace webapp.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string? Log { get; set; }

        [BindProperty]
        public string? Pass { get; set; }

        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnPost()
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == Log);
            if (user != null && Pass != string.Empty)
            {
                var passwordHasher = new PasswordHasher<User>();
                var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Pass);

                if (result == PasswordVerificationResult.Success)
                {
                    HttpContext.Session.SetString("IsLoggedIn", "true");
                    HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    return RedirectToPage("/Index");
                }
            }

            ModelState.AddModelError(string.Empty, "Nieprawid³owy login lub has³o.");
            return Page();
        }
        public IActionResult OnGetLogout()
        {
            if(HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return RedirectToPage("/Login");
            }
            // Usuñ stan zalogowania z sesji
            HttpContext.Session.Remove("IsLoggedIn");
            HttpContext.Session.Remove("IsAdmin");
            HttpContext.Session.Remove("UserId");
            return RedirectToPage("/Index");
        }
    }

}