using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;

namespace webapp.Pages;

public class HistoryView : PageModel
{
    private readonly AppDbContext _context;
    public HistoryView(AppDbContext context)
    {
        _context = context;
    }
    
    public List<ActionHistory> ActionHistories { get; set; } = new();
    public List<ActionHistoryModel> ActionHistoryModels { get; set; } = new();
    public List<User> Users { get; set; } = new();
    public List<Actions> Actions { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool ShowAllRequests { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SelectedUserId { get; set; }

    public List<User> VisibleUsers { get; set; } = new();

    public void OnGet()
    {
        Actions = _context.Actions.ToList();
        Users = _context.Users.ToList();
        var selectedApplicationId = HttpContext.Session.GetInt32("SelectedApplicationId");
        if (selectedApplicationId.HasValue)
        {
            // Tryb wyświetlania jednego wniosku
            ActionHistoryModels = GetActionHistory(selectedApplicationId.Value);
        }
    }
    private List<ActionHistoryModel> GetActionHistory(int applicationId = 0)
    {
        var messageLinks = _context.MessageLinks.Where(a => a.ApplicationId == applicationId).ToList();
        var Devices = _context.Devices;
        var UserPermissions = _context.UserPermissions;
        var messages = _context.Messages.ToList();

        var groupedLinks = messageLinks.GroupBy(ml => ml.ApplicationId);
        List<ActionHistoryModel> tmp = groupedLinks.Select(group => new ActionHistoryModel
        {
            Id = _context.ActionHistories.FirstOrDefault(ah => ah.ApplicationId == group.Key)?.Id ?? 0,
            ApplicantId = messages.FirstOrDefault(m => group.Select(ml => ml.MessageId).Contains(m.Id))?.UserId ?? 0,
            EditorId = _context.ActionHistories
                .Where(ah => ah.ApplicationId == group.Key)
                .OrderByDescending(ah => ah.Date)
                .FirstOrDefault()?.UserId ?? 0,
            ActionId = _context.ActionHistories.FirstOrDefault(ah => ah.ApplicationId == group.Key)?.ActionId ?? 0,
            ActionDate = _context.ActionHistories.FirstOrDefault(ah => ah.ApplicationId == group.Key)?.Date ?? DateTime.MinValue,
            LastUpdatedDate = _context.ActionHistories
                .Where(ah => ah.ApplicationId == group.Key)
                .OrderByDescending(ah => ah.Date)
                .FirstOrDefault()?.Date ?? DateTime.MinValue,
            Messages = messages
                .Where(m => group.Select(ml => ml.MessageId).Contains(m.Id))
                .Where(m => Devices.Any(d => d.Id == m.DeviceId) || UserPermissions.Any(up => up.Id == m.UPermissionsId))
                .ToList(),
            IsRejected = group.Any(ml => ml.DegreeId == -1),
            IsApproved = group.Any(ml => ml.DegreeId == -6 || ml.DegreeId == -7),
            IsDone = group.Any(ml => ml.DegreeId == -10 || ml.DegreeId == -11),
            IsChecked = group.Any(ml => ml.DegreeId == -7 || ml.DegreeId == -11),
            Expand = false,
            ActionHistories = _context.ActionHistories
                .Where(ah => ah.ApplicationId == group.Key).ToList()
                    
        }).ToList();
        foreach (var a in tmp)
        {
            a.ActionHistories = a.ActionHistories.DistinctBy(b => b.ActionId).ToList();
        }

        return tmp;
    }


    public int GetStatus(Message message)
    {
        if (message != null)
        {
            if (message.DeviceId != null)
            {
                try
                {
                    return _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId).StatusId;
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
                    return _context.UserPermissions.FirstOrDefault(d => d.Id == message.UPermissionsId).StatusId;
                }
                catch
                {
                    return 5;
                }
            }
        }

        return 0;
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

    public string GetActionName(int actionId)
    {
        return Actions.FirstOrDefault(a => a.Id == actionId)?.Name ?? "Nieznana akcja";
    }
}

public class ActionHistoryModel
{
    public int Id { get; set; }
    public int ApplicantId { get; set; }
    public int EditorId { get; set; }
    public List<Message> Messages { get; set; } = new();
    public int ActionId { get; set; }
    public DateTime ActionDate { get; set; }
    public DateTime LastUpdatedDate { get; set; }
    public bool IsRejected { get; set; } // Flaga oznaczaj�ca, czy wniosek jest odrzucony
    public bool IsApproved { get; set; } // Flaga oznaczaj�ca, czy wniosek jest zaakceptowany
    public bool IsDone { get; set; }
    public bool Expand { get; set; } = false;
    public bool? IsChecked { get; set; } = false;
    public List<ActionHistory> ActionHistories { get; set; } = new();
}