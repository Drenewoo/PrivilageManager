using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using System.Linq;

namespace webapp.Pages;

public class IODProgramManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public IODProgramManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<MUserPermissionViewModel> UserPermissions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? UserPermissionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public bool isLastPage { get; set; }

    private const int PageSize = 10;

    public void OnGet()
    {
        LoadData();
    }

    private void LoadData()
    {
        // Pobieranie identyfikatora zalogowanego u�ytkownika z sesji
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            UserPermissions = new List<MUserPermissionViewModel>();
            return;
        }

        // Pobieranie listy program�w, w kt�rych u�ytkownik ma przypisane uprawnienia
        var userProgramIds = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId)
            .Select(up => up.PermissionId)
            .Join(_context.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.ProgramId)
            .Distinct()
            .ToList();
        

        // Pobieranie uprawnie� u�ytkownika z do��czeniem producenta
        var query = _context.UserPermissions
            .Where(up => up.Id != 0)
            .Join(_context.Users, dup => dup.UserId, u => u.Id, (dup, u) => new {dup, u})// Filtrowanie po zalogowanym u�ytkowniku
            .Join(_context.Permissions, up => up.dup.PermissionId, p => p.Id, (up, p) => new { up, p })
            .Join(_context.Programs, upp => upp.p.ProgramId, pr => pr.Id, (upp, pr) => new { upp.up, upp.p, pr })
            //.Join(_context.Producents, uppp => uppp.pr.ProducerId, prod => prod.Id, (uppp, prod) => new { uppp.up, uppp.p, uppp.pr, prod })
            .Join(_context.Statuses, uppp => uppp.up.dup.StatusId, s => s.Id, (uppp, s) => new MUserPermissionViewModel
            {
                Id = uppp.up.dup.Id,
                FirstName = uppp.up.u.FirstName,
                LastName = uppp.up.u.LastName,
                DegreeId = _context.Degrees.First(d => d.Id == _context.Degrees.First(de => de.Id == uppp.up.u.DegreeId).ManagerId).Id,
                ProgramName = uppp.pr.Name,
                PermissionName = uppp.p.Name,
                //ProducerName = uppp.prod.Name, // Pobieranie nazwy producenta
                RequestDate = uppp.up.dup.RequestDate,
                ResponseDate = uppp.up.dup.ResponseDate,
                StatusName = s.Name
            });

        var totalItems = query.Count();
        isLastPage = PageNumber * PageSize >= totalItems;

        // Sortowanie po dacie zg�oszenia (od najnowszych do najstarszych)
        UserPermissions = query
            .OrderByDescending(up => up.RequestDate)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        UserPermissions = UserPermissions.Where(u => u.DegreeId == loggedInUserId).ToList();

    }
}
