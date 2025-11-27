using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;

namespace webapp.Pages;

public class DeviceManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public DeviceManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<DeviceViewModel> Devices { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public int? DeviceId { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool isLastPage { get; set; } = false;

    public void OnGet(int pageNumber = 1)
    {
        PageNumber = pageNumber;
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            Devices = new List<DeviceViewModel>();
            return;
        }

        var selectedDeviceId = HttpContext.Session.GetInt32("SelectedDeviceId");
        if (selectedDeviceId.HasValue)
        {
            DeviceId = selectedDeviceId;
        }

        // Pobieranie urządzeń użytkownika z wniosków lub oczekujących
        var userDeviceIds = _context.Messages
            .Where(m => m.UserId == loggedInUserId && m.DeviceId.HasValue)
            .Select(m => m.DeviceId.Value)
            .ToList();

        var query = _context.Devices
            .Where(d => userDeviceIds.Contains(d.Id))
            .Join(_context.DeviceTypes, d => d.DeviceTypeId, dt => dt.Id, (d, dt) => new { d, dt })
            .Join(_context.Statuses, ddt => ddt.d.StatusId, s => s.Id, (ddt, s) => new DeviceViewModel
            {
                Id = ddt.d.Id,
                DeviceName = ddt.d.Name,
                DeviceTypeName = ddt.dt.Name,
                Serial = ddt.d.Serial,
                Status = s.Name, // Pobieranie nazwy statusu
                StatusUpdate = ddt.d.StatusUpdate
            });

        if (DeviceId.HasValue)
        {
            query = query.Where(d => d.Id == DeviceId.Value);
        }

        var totalDevices = query.Count();
        Devices = query
                    .OrderBy(d => d.StatusUpdate)
                    .ThenBy(d => d.DeviceName)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

        isLastPage = PageNumber * PageSize >= totalDevices;
    }

    public IActionResult OnPostFilter()
    {
        if (DeviceId.HasValue)
        {
            HttpContext.Session.SetInt32("SelectedDeviceId", DeviceId.Value);
        }
        else
        {
            HttpContext.Session.Remove("SelectedDeviceId");
        }

        return RedirectToPage();
    }
}

public class DeviceViewModel
{
    public int Id { get; set; }
    public string DeviceTypeName { get; set; }
    public string DeviceName { get; set; }
    public string Serial { get; set; }
    public string Status { get; set; }
    public DateTime StatusUpdate { get; set; }
}