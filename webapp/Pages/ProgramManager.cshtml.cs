using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using System.Linq;

namespace webapp.Pages;

public class ProgramManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public ProgramManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<UserPermissionViewModel> UserPermissions { get; set; } = new();
    public IEnumerable<SelectListItem> ProgramSelectList { get; set; } = new List<SelectListItem>();
    [BindProperty(SupportsGet = true)]
    public int? SelectedProgramId { get; set; }

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


    public void OnPostShowPermissions(int? SelectedProgramId)
    {
        this.SelectedProgramId = SelectedProgramId;
        LoadData();
    }

    private void LoadData()
    {
        // Pobieranie identyfikatora zalogowanego użytkownika z sesji
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            ProgramSelectList = new List<SelectListItem>();
            UserPermissions = new List<UserPermissionViewModel>();
            return;
        }

        var userProgramIds = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId)
            .Select(up => up.PermissionId)
            .Join(_context.Permissions, permissionId => permissionId, p => p.Id, (permissionId, p) => p.ProgramId)
            .Distinct()
            .ToList();

        ProgramSelectList = _context.Programs
            .Where(p => userProgramIds.Contains(p.Id))
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToList();

        var query = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId)
            .Join(_context.Permissions, up => up.PermissionId, p => p.Id, (up, p) => new { up, p })
            .Join(_context.Programs, upp => upp.p.ProgramId, pr => pr.Id, (upp, pr) => new { upp.up, upp.p, pr })
            //.Join(_context.Producents, uppp => uppp.pr.ProducerId, prod => prod.Id, (uppp, prod) => new { uppp.up, uppp.p, uppp.pr, prod })
            .Join(_context.Statuses, uppp => uppp.up.StatusId, s => s.Id, (uppp, s) => new UserPermissionViewModel
            {
                Id = uppp.up.Id,
                ProgramName = uppp.pr.Name,
                PermissionName = uppp.p.Name,
                //ProducerName = uppp.prod.Name,
                RequestDate = uppp.up.RequestDate,
                ResponseDate = uppp.up.ResponseDate,
                StatusName = s.Name
            });

        if (UserPermissionId.HasValue)
        {
            query = query.Where(up => up.Id == UserPermissionId.Value);
        }
        else if (SelectedProgramId.HasValue)
        {
            query = query.Where(up => up.ProgramName == _context.Programs.First(p => p.Id == SelectedProgramId).Name);
        }

        var totalItems = query.Count();
        isLastPage = PageNumber * PageSize >= totalItems;

        UserPermissions = query
            .OrderByDescending(up => up.RequestDate)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }
}

public class UserPermissionViewModel
{
    public int Id { get; set; }
    public string ProducerName { get; set; }
    public string ProgramName { get; set; }
    public string PermissionName { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ResponseDate { get; set; }
    public string StatusName { get; set; }
}
