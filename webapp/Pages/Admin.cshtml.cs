using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;

namespace webapp.Pages;

public class AdminModel : PageModel
{
    private readonly AppDbContext _context;

    public AdminModel(AppDbContext context)
    {
        _context = context;
    }

    public string ActiveTab { get; set; } = "Users";
    public List<User> Users { get; set; } = new();
    public List<Degree> Degrees { get; set; } = new();
    public List<Programs> Programs { get; set; } = new();
    public List<Permission> Permissions { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public int? SelectedDepartmentId { get; set; }
    public IEnumerable<SelectListItem> DepartmentSelectList => Departments.Select(d => new SelectListItem
    {
        Value = d.Id.ToString(),
        Text = d.Name
    });

    public void OnPostShowUsers()
    {
        ActiveTab = "Users";
        Departments = _context.Departments.ToList();
        Degrees = _context.Degrees.ToList();

        // Filtrowanie u¿ytkowników po placówkach
        if (SelectedDepartmentId.HasValue)
        {
            Users = _context.Users.Where(u => Degrees.Any(d => d.Id == u.DegreeId && d.DepartmentId == SelectedDepartmentId)).ToList();
        }
        else
        {
            Users = _context.Users.ToList();
        }

        SelectedDepartmentId = SelectedDepartmentId;
    }

    public void OnPostShowDegrees()
    {
        ActiveTab = "Degrees";
        Degrees = _context.Degrees.ToList();
        Departments = _context.Departments.ToList();
        Users = _context.Users.ToList();
    }
    public void OnPostShowDepartments()
    {
        ActiveTab = "Departments";
        Departments = _context.Departments.ToList();
    }

    public void OnPostShowPrograms()
    {
        ActiveTab = "Programs";
        Programs = _context.Programs.ToList();
    }

    public void OnPostShowPermissions()
    {
        ActiveTab = "Permissions";
        Permissions = _context.Permissions.ToList();
        Programs = _context.Programs.ToList();
    }
}