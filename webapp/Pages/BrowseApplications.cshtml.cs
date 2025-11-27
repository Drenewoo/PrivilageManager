using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;
using webapp.Services;

namespace webapp.Pages;

public class BrowseApplicationsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ActionLoggerService _actionLoggerService;
    
    public BrowseApplicationsModel(AppDbContext context)
    {
        _context = context;
        _actionLoggerService = new ActionLoggerService(context);
    }

    public List<ApplicationViewModel> Applications { get; set; } = new();
    public List<Device> devices { get; set; } = new();
    public List<UserPermission> userPermissions { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<ActionHistory> ActionHistories { get; set; } = new();

    public List<ApplicationDetails> ApplicationDetails { get; set; } = new();
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public bool isLastPage { get; set; } = false;

    public void OnGet(int pageNumber = 1)
    {
        PageNumber = pageNumber;
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");

        if (loggedInUserId == null)
        {
            Applications = new List<ApplicationViewModel>();
            return;
        }

        devices = _context.Devices.ToList();
        userPermissions = _context.UserPermissions.ToList();
        Users = _context.Users.ToList();
        ActionHistories = _context.ActionHistories.ToList();
        ApplicationDetails = _context.ApplicationDetails.ToList();

        var userMessages = _context.Messages
            .Where(m => m.UserId == loggedInUserId)
            .ToList();

        var messageLinks = _context.MessageLinks
            .Where(ml => userMessages.Select(m => m.Id).Contains(ml.MessageId))
            .GroupBy(ml => ml.ApplicationId)
            .ToList();

        var applicationsQuery = messageLinks.Select(group => new ApplicationViewModel
        {
            ApplicationId = group.Key,
            messageText = ApplicationDetails.Where(a => a.ApplicationId == group.Key)
                .Select(b => b.Message)
                .FirstOrDefault() ?? string.Empty,
            expirationDate = ApplicationDetails.Where(a => a.ApplicationId == group.Key)
                .Select(b => b.ExpireDate).First(),
            Messages = userMessages
                .Where(m => group.Select(ml => ml.MessageId).Contains(m.Id))
                .ToList(),
            IsRejected = group.Any(ml => ml.DegreeId == -1),
            IsApproved = group.Any(ml => ml.DegreeId == -6 || ml.DegreeId == -7),
            IsDone = group.Any(ml => ml.DegreeId == -10 || ml.DegreeId == -11)
        });

        var totalApplications = applicationsQuery.Count();
        Applications = applicationsQuery
            .OrderByDescending(app => app.ApplicationId)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        isLastPage = PageNumber * PageSize >= totalApplications;
    }

    public class ApplicationViewModel
    {
        public int ApplicationId { get; set; }
        public List<Message> Messages { get; set; } = new();
        public string messageText { get; set; } = string.Empty;
        public DateTime expirationDate { get; set; } = DateTime.UnixEpoch;
        public bool IsRejected { get; set; } // Flaga oznaczaj�ca, czy wniosek jest odrzucony
        public bool IsApproved { get; set; } // Flaga oznaczaj�ca, czy wniosek jest zaakceptowany
        public bool IsDone { get; set; }
    }

    public string GetDeviceName(int deviceId)
    {
        var device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);
        return device?.Name ?? "Nieznane urządzenie";
    }
    public string GetDeviceSerial(int deviceId)
    {
        var device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);
        return device?.Serial ?? string.Empty;
    }

    public string GetProgramName(int programId)
    {
        var program = _context.Programs.FirstOrDefault(p => p.Id == programId);
        return program?.Name ?? "Nieznany program / zasób";
    }

    public string GetPermissionName(int permissionId)
    {
        var permission = _context.Permissions.FirstOrDefault(p => p.Id == permissionId);
        return permission?.Name ?? "Nieznane uprawnienie";
    }
    public IActionResult OnPostReject(int applicationId)
    {
        var loggedInUserId = HttpContext.Session.GetInt32("UserId");
        if (loggedInUserId == null)
            return Page();
        var excludedDegrees = new List<int> { -7, -11, -6, -10 }; // Stopnie do wykluczenia
        // Pobierz MessageLinks powiązane z applicationId
        var messageLinksGood = _context.MessageLinks
            .Where(ml => ml.ApplicationId == applicationId)
            .Any(m => excludedDegrees.Contains(m.DegreeId));

        if (messageLinksGood)
        {
            return RedirectToPage("/BrowseApplications");
        }

        var messageLinks = _context.MessageLinks
            .Where(ml => ml.ApplicationId == applicationId).ToList();

        int newActionId = 1;
        if(_context.ActionHistories.Count() > 0)
        {
            newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
        }
        foreach (var messageLink in messageLinks)
        {
            // Pobierz wiadomość powiązaną z MessageId
            var message = _context.Messages.FirstOrDefault(m => m.Id == messageLink.MessageId);
            if (message == null) continue;

            // Zaktualizuj status urządzeń
            if (message.DeviceId.HasValue)
            {
                var device = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId.Value);
                if (device != null)
                {
                    device.StatusId = _context.Statuses.Min(d => d.Id) + 5; // Status "Odrzucono"
                    device.StatusUpdate = DateTime.Now;
                    _actionLoggerService.Log(
                        loggedInUserId ?? 0,
                        null,
                        device.Id,
                        applicationId,
                        3,
                        newActionId
                    );
                }
            }

            // Zaktualizuj status uprawnień
            if (message.ProgramId.HasValue)
            {
                var userPermission = _context.UserPermissions.FirstOrDefault(up => up.Id == message.UPermissionsId);
                if (userPermission != null)
                {
                    userPermission.StatusId = _context.Statuses.Min(d => d.Id) + 5; // Status "Odrzucono"
                    userPermission.ResponseDate = DateTime.Now;
                    _actionLoggerService.Log(
                        loggedInUserId ?? 0,
                        userPermission.Id,
                        null,
                        applicationId,
                        3,
                        newActionId
                    );
                }
            }
            messageLink.DegreeId = -1; // Oznacz jako odrzucone
        }

        _context.SaveChanges();
        return RedirectToPage();
    }
    public int GetStatus(Message message)
    {
        if (message != null)
        {
            if (message.DeviceId != null)
            {
                try
                {
                    return devices.FirstOrDefault(d => d.Id == message.DeviceId).StatusId;
                }
                catch
                {
                    return 5;
                }
            }
            else if (message.UPermissionsId != null)
            {
                try
                {
                    return userPermissions.FirstOrDefault(d => d.Id == message.UPermissionsId).StatusId;
                }
                catch
                {
                    return 5;
                }
            }
        }

        return 0;
    }
}
