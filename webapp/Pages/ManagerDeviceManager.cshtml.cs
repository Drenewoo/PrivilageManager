using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace webapp.Pages;

public class ManagerDeviceManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public ManagerDeviceManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<MDeviceViewModel> Devices { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public int? DeviceId { get; set; }

    public IEnumerable<SelectListItem> UsersSelectList;

    [BindProperty]
    public int? SelectedUserId { get; set; } // Wybrany użytkownik

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool isLastPage { get; set; } = false;

    public void OnGet(int pageNumber = 1)
    {
        PageNumber = pageNumber;
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            Devices = new List<MDeviceViewModel>();
            return;
        }
        
        List<int> managedDegrees2 = _context.Degrees
            .Where(d => d.ManagerId == loggedInUserId)
            .Select(d => d.Id)
            .ToList();
        
        UsersSelectList = _context.Users.Where(u => managedDegrees2.Contains(u.DegreeId)).Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = d.FirstName + " " + d.LastName + " " + d.Email
        });

        var selectedUserId = HttpContext.Session.GetInt32("SelectedUserId");
        if (selectedUserId.HasValue)
        {
            SelectedUserId = selectedUserId;
        }

        var selectedId = HttpContext.Session.GetInt32("SelectedId");
        HttpContext.Session.Remove("SelectedId");

        // Pobierz degreeId managera
        var managerUser = _context.Users.FirstOrDefault(u => u.Id == loggedInUserId);
        if (managerUser == null)
        {
            Devices = new List<MDeviceViewModel>();
            return;
        }
        var managerDegreeId = managerUser.DegreeId;

        // Pobierz stopnie zarządzane przez managera
        var managedDegrees = _context.Degrees.Where(d => d.ManagerId == managerDegreeId).Select(de => de.Id).ToList();
        // Pobierz użytkowników z tych stopni
        var users = _context.Users.Where(u => managedDegrees.Contains(u.DegreeId)).Select(u => u.Id).ToList();

        if (SelectedUserId.HasValue)
        {
            users = users.Where(u => u == SelectedUserId.Value).ToList();
        }

        // Pobieranie urządzeń użytkownika z wniosków lub oczekujących
        var userDevices = _context.Messages
            .Where(m => users.Contains(m.UserId) && m.DeviceId.HasValue)
            .ToList();
        var userDeviceIds = userDevices
            .Select(m => m.DeviceId.Value)
            .ToList();

        if (selectedId != null)
        {
            userDeviceIds = _context.Devices.Where(d => d.Id == selectedId).Select(m => m.Id).ToList();
        }
        // Poprawka: filtruj tylko urządzenia, które nie mają statusu zatwierdzono/wykonano/odrzucono
        var statusApproved = _context.Statuses.Min(s => s.Id) + 1; // Zatwierdzono
        var query = _context.Devices
            .Where(de => userDeviceIds.Contains(de.Id) && de.StatusId < statusApproved)
            .Join(_context.DeviceTypes, dt => dt.DeviceTypeId, dtt => dtt.Id, (dt, dtt) => new { dt, dtt })
            .Join(_context.Statuses, ddt => ddt.dt.StatusId, s => s.Id, (ddt, s) => new MDeviceViewModel
            {
                Id = ddt.dt.Id, 
                DeviceName = ddt.dt.Name,
                DeviceTypeName = ddt.dtt.Name,
                Serial = ddt.dt.Serial,
                Status = s.Name, // Pobieranie nazwy statusu
                StatusUpdate = ddt.dt.StatusUpdate
            });
        Devices = query
                    .OrderBy(d => d.DeviceTypeName)
                    .ThenBy(d => d.DeviceName)
                    .ToList();
        var query2 = _context.Devices
            .Where(de => userDeviceIds.Contains(de.Id))
            .Join(_context.Messages, d => d.Id, ud => ud.DeviceId, (device, message) => new MDeviceViewModel
            {
                Id = device.Id,
                FirstName = _context.Users.First(u => u.Id == message.UserId).FirstName,
                LastName = _context.Users.First(u => u.Id == message.UserId).LastName,
            })
            .ToList();

        foreach (var device in Devices)
        {
            device.FirstName = query2.FirstOrDefault(d => d.Id == device.Id)?.FirstName ?? String.Empty;
            device.LastName = query2.FirstOrDefault(d => d.Id == device.Id)?.LastName ??  String.Empty;
        }

        var totalDevices = Devices.Count;
        Devices = Devices
            .OrderBy(d => d.StatusUpdate)
            .ThenBy(d => d.DeviceName)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        isLastPage = PageNumber * PageSize >= totalDevices;
    }
    public IActionResult OnPostFilter()
    {
        // Ustawienie wybranego użytkownika na podstawie przesłanych danych
        if (SelectedUserId.HasValue)
        {
            HttpContext.Session.SetInt32("SelectedUserId", SelectedUserId.Value);
        }
        else
        {
            HttpContext.Session.Remove("SelectedUserId");
        }

        // Odświeżenie strony z uwzględnieniem filtrowania
        return RedirectToPage();
    }
}

public class MDeviceViewModel
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DeviceTypeName { get; set; }
    public string DeviceName { get; set; }
    public string Serial { get; set; }
    public string Status { get; set; }
    public DateTime StatusUpdate { get; set; }
}