using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;

namespace webapp.Pages
{
    public class AdminRestoreModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminRestoreModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            OnPostAddUser("admin", "password");
        }
        public IActionResult OnPostAddUser(string username, string password)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError(string.Empty, "U¿ytkownik o podanej nazwie ju¿ istnieje i/lub istnieje inny administrator");
                return Page();
            }

            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = username,
                PasswordHash = passwordHasher.HashPassword(null, password),
                FirstName = string.Empty, LastName = string.Empty,
                Email = string.Empty,
                DegreeId = 1,
                IsAdmin = true,
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();
            ModelState.AddModelError(string.Empty, "U¿ytkownik zosta³ dodany pomyœlnie.");

            return Page();
        }
    }
}
