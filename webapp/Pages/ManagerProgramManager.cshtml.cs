using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using System.Linq;

namespace webapp.Pages;

public class ManagerProgramManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public ManagerProgramManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<MUserPermissionViewModel> UserPermissions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? UserPermissionId { get; set; }

    public IEnumerable<SelectListItem> UsersSelectList;

    [BindProperty]
    public int? SelectedUserId { get; set; } // Wybrany użytkownik

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public bool isLastPage { get; set; }

    private const int PageSize = 10;

    public void OnGet()
    {
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            UserPermissions = new List<MUserPermissionViewModel>();
            return;
        }
        
        List<int> managedDegrees = _context.Degrees
            .Where(d => d.ManagerId == loggedInUserId)
            .Select(d => d.Id)
            .ToList();
        
        UsersSelectList = _context.Users.Where(u => managedDegrees.Contains(u.DegreeId)).Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = d.FirstName + " " + d.LastName + " " + d.Email
        });

        var selectedUserId = HttpContext.Session.GetInt32("SelectedUserId");
        if (selectedUserId.HasValue)
        {
            SelectedUserId = selectedUserId;
        }

        LoadData();
    }

    public IActionResult OnPostFilter()
    {
        if (SelectedUserId.HasValue)
        {
            HttpContext.Session.SetInt32("SelectedUserId", SelectedUserId.Value);
        }
        else
        {
            HttpContext.Session.Remove("SelectedUserId");
        }

        return RedirectToPage();
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
        
        var selectedId = HttpContext.Session.GetInt32("SelectedId");
        HttpContext.Session.Remove("SelectedId");
        
        // Pobieranie listy program�w, w kt�rych u�ytkownik ma przypisane uprawnienia
        var userProgramIds = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId)
            .Select(up => up.PermissionId)
            .Join(_context.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.ProgramId)
            .Distinct()
            .ToList();


        var id = 0;
        if (selectedId != null)
        {
            id = selectedId ?? 0;
        }
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
                ProducerName = _context.Departments.FirstOrDefault(r => r.Id == _context.Degrees.First(de => de.Id == uppp.up.u.DegreeId).DepartmentId).Name ?? "", // Pobieranie nazwy producenta
                RequestDate = uppp.up.dup.RequestDate,
                ResponseDate = uppp.up.dup.ResponseDate,
                StatusName = s.Name
            });

        
        
        // Sortowanie po dacie zg�oszenia (od najnowszych do najstarszych)
            if (SelectedUserId.HasValue)
        {
            query = query.Where(up => up.UserId == SelectedUserId.Value);
        }

        var totalItems = query.Count();
        isLastPage = PageNumber * PageSize >= totalItems;

        UserPermissions = query
            .OrderByDescending(up => up.RequestDate)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
            if (selectedId != null)
            {
                UserPermissions = UserPermissions.Where(up => up.Id == selectedId).ToList();
                return;
            } 
        var degreeId = _context.Degrees.FirstOrDefault(d => d.Id == _context.Users.First(u => u.Id == loggedInUserId).DegreeId)?.Id ?? 0;

        UserPermissions = UserPermissions.Where(u => u.DegreeId == degreeId).ToList();
  
        if (SelectedUserId.HasValue)
        {
            UserPermissions = UserPermissions.Where(u => u.UserId == SelectedUserId).ToList();

        }

    }
}

public class MUserPermissionViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int DegreeId { get; set; }
    public string ProducerName { get; set; }
    public string ProgramName { get; set; }
    public string PermissionName { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ResponseDate { get; set; }
    public string StatusName { get; set; }
}
