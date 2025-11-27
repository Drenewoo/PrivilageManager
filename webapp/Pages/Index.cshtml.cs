using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;

namespace webapp.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public bool IsManager { get; set; } = false;
    public bool IsIOD { get; set; } = false;

    public bool IsAdmin { get; set; } = false;
    
    public void OnGet()
    {
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");
        HttpContext.Session.SetInt32("ApplicationUserId", HttpContext.Session.GetInt32("UserId") ?? 0);

        if (loggedInUserId.HasValue)
        {
            // Pobierz stopie� zalogowanego u�ytkownika
            var userDegreeId = _context.Users
                .Where(u => u.Id == loggedInUserId.Value)
                .Select(u => u.DegreeId)
                .FirstOrDefault();

            // Sprawd�, czy istniej� u�ytkownicy, kt�rych managerem jest zalogowany u�ytkownik
            IsManager = _context.Degrees.Any(d => d.ManagerId == userDegreeId);
            IsIOD = _context.Users.FirstOrDefault(u => u.Id == loggedInUserId.Value)?.IsAdmin == 2 ? true : false;
            IsAdmin = _context.Users.FirstOrDefault(u => u.Id == loggedInUserId.Value)?.IsAdmin == 1 ? true : false;
        }
    }
}