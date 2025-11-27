using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;

namespace webapp.Pages;

public class IODDeviceManagerModel : PageModel
{
    private readonly AppDbContext _context;

    public IODDeviceManagerModel(AppDbContext context)
    {
        _context = context;
    }

    public List<MDeviceViewModel> Devices { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public int? DeviceId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public bool isLastPage { get; set; }

    private const int PageSize = 10;

    public void OnGet()
    {
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            Devices = new List<MDeviceViewModel>();
            return;
        }

        // message -> userid -> users -> degreeId -> degrees -> managerId

        var messageLinks = _context.MessageLinks.Where(m => m.DegreeId == -10 || m.DegreeId == -11).Select(m => m.MessageId).ToList();
        // Pobieranie urz�dze� u�ytkownika z wniosk�w lub oczekuj�cych
        var userDevices = _context.Messages
            .Where(m => messageLinks.Contains(m.Id) && m.DeviceId.HasValue)
            .ToList();
        var userDeviceIds = _context.Messages
            .Where(m => messageLinks.Contains(m.Id) && m.DeviceId.HasValue)
            .Select(m => m.DeviceId.Value)
            .ToList();

        var query = _context.Devices
            .Where(de => userDeviceIds.Contains(de.Id))
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

        var totalItems = query.Count();
        isLastPage = PageNumber * PageSize >= totalItems;

        Devices = query
            .OrderBy(d => d.DeviceTypeName)
            .ThenBy(d => d.DeviceName)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
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
    }
}