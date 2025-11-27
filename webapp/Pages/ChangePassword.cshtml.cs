using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using webapp.Data;
using webapp.Models;
using System.ComponentModel.DataAnnotations;

namespace webapp.Pages
{
    public class ChangePasswordModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public ChangePasswordModel(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [BindProperty]
        [Required(ErrorMessage = "Obecne hasło jest wymagane")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Nowe hasło jest wymagane")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} i maksymalnie {1} znaków", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Hasła nie są zgodne")]
        public string ConfirmPassword { get; set; }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("IsLoggedIn") != "true")
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                ModelState.AddModelError(string.Empty, "Nie można pobrać danych użytkownika.");
                return Page();
            }

            var user = _context.Users.Find(userId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Użytkownik nie istnieje.");
                return Page();
            }

            // Walidacja obecnego hasła
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Obecne hasło jest nieprawidłowe.");
                return Page();
            }

            // Zmiana hasła
            user.PasswordHash = _passwordHasher.HashPassword(user, NewPassword);
            _context.SaveChanges();

            ModelState.AddModelError(string.Empty, "Hasło zostało zmienione pomyślnie.");
            return RedirectToPage("/Index");
        }
    }
}
