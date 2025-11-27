using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using System.Linq;
using System.Text.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using webapp.Services;

namespace webapp.Pages;

public class EditApplicationModel : PageModel
{
    private readonly AppDbContext _context;

    public EditApplicationModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public List<Message> PendingMessages { get; set; } = new();

    [BindProperty]
    public int ApplicationId { get; set; }

    [BindProperty]
    public int? ProgramId { get; set; }

    [BindProperty]
    public int? DeviceId { get; set; }

    [BindProperty]
    public int? PermissionId { get; set; }

    [BindProperty]
    public string? MessageText { get; set; }
    [BindProperty]
    [DataType(DataType.Date)]
    public DateTime? ExpireDate { get; set; }

    public IEnumerable<SelectListItem> ProgramSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> PermissionSelectList { get; set; } = new List<SelectListItem>();
    [BindProperty]
    public int? ProducerId { get; set; }
    public IEnumerable<SelectListItem> ProducerSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceTypeSelectList { get; set; } = new List<SelectListItem>();
    public List<Device> DeviceList = new List<Device>();
    public List<UserPermission> UPList = new List<UserPermission>();

    public List<Message> DeletedMessages { get; set; } = new();

    public List<User> users = new();
    
    private ActionLoggerService _actionLoggerService;
    
    public string LoggedInUserDepartmentName { get; set; } = string.Empty;
    
    public IActionResult OnGet()
    {

        ApplicationId = HttpContext.Session.GetInt32("EditApplicationId") ?? -1;
        if (ApplicationId == -1)
        {
            RedirectToPage("/ManagerBrowseApplication");
        }
        HttpContext.Session.SetInt32("ApplicationId", ApplicationId);
        HttpContext.Session.SetInt32("ApplicationUserId", _context.Messages.FirstOrDefault(m => m.Id == _context.MessageLinks.FirstOrDefault(ml => ml.ApplicationId == ApplicationId).MessageId).UserId);
        // Pobierz wiadomo�ci powi�zane z wnioskiem
        MessageText = _context.ApplicationDetails.FirstOrDefault(a => a.ApplicationId == ApplicationId).Message ?? String.Empty;
        ExpireDate = _context.ApplicationDetails.FirstOrDefault(a => a.ApplicationId == ApplicationId).ExpireDate;
        LoadPendingMessagesFromSession();
        if (PendingMessages == null || PendingMessages.Count == 0)
        {
            var messageLinks = _context.MessageLinks
                .Where(ml => ml.ApplicationId == ApplicationId)
                .ToList();
            
            var excludedDeviced = _context.Devices.Where(d => d.StatusId == 6).Select(d => d.Id).ToList();
            var excludedUP = _context.UserPermissions.Where(u => u.StatusId == 6).Select(u => u.Id).ToList();
            
            PendingMessages = _context.Messages
                .Where(m => messageLinks.Select(ml => ml.MessageId).Contains(m.Id))
                .ToList();

            var MessagesToDelete = new List<Message>();
            foreach (var message in PendingMessages)
            {
                if (excludedDeviced.Contains(message.DeviceId ?? 0) || excludedUP.Contains(message.UPermissionsId ?? 0))
                {
                    MessagesToDelete.Add(message);
                }
            }

            foreach (var message in MessagesToDelete)
            {
                PendingMessages.Remove(message);
            }
            
            
        }
        SavePendingMessagesToSession();
        //LoadPrograms();
        LoadDevices();
        LoadPermissions();
        LoadDeviceTypes();

        DeviceList = _context.Devices.Where(d => d.StatusId != _context.Statuses.Min(e => e.Id) + 5).ToList();
        UPList = _context.UserPermissions.Where(d => d.StatusId != _context.Statuses.Min(e => e.Id) + 5).ToList();

        users = _context.Users.ToList();
        return Page();
    }
    public IActionResult OnPostAddMessage(string type)
    {
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;

        LoadPendingMessagesFromSession();
        
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");
        
        if (type == "Program" && PermissionId.HasValue)
        {
            foreach (var pid in _context.PermissionGroups.First(p => p.Id == PermissionId.Value).PermissionIds)
            {
                if (!_context.UserPermissions.Any(d =>
                        (d.StatusId == 1 || d.StatusId == 2 || d.StatusId == 7) && d.UserId == loggedInUserId &&
                        d.PermissionId == pid))
                {
                    if (PendingMessages.All(m => m.PermissionId != pid))
                    {
                        PendingMessages.Add(new Message
                        {
                            Id = 0,
                            ProgramId = _context.Permissions.FirstOrDefault(p => p.Id == pid).ProgramId,
                            PermissionId = pid,
                            UserId = HttpContext.Session.GetInt32("ApplicationUserId") ?? 0
                        });
                    }
                }
            }
        }
        else if (type == "Device" && DeviceId.HasValue)
        {
            PendingMessages.Add(new Message
            {
                Id = 0,
                DeviceId = DeviceId,
                MessageText = MessageText,
                RequestDate = DateTime.Now,
                UserId = HttpContext.Session.GetInt32("ApplicationUserId") ?? 0
            });
        }
        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        //LoadPrograms();
        LoadDevices();
        LoadPermissions();
        LoadDeviceTypes();
        DeviceList = _context.Devices.Where(d => d.StatusId != 6).ToList();
        UPList = _context.UserPermissions.Where(d => d.StatusId != 6).ToList();

        users = _context.Users.ToList();
        return Page();
    }

    public IActionResult OnPostRemoveMessage(int messageId)
    {
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;
        LoadPendingMessagesFromSession();
        var message = PendingMessages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            // Ustaw status na "Odrzucono" w bazie danych
            DeletedMessages.Add(message);
            PendingMessages.Remove(message);
        }
        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        //LoadPrograms();
        LoadDevices();
        LoadPermissions();
        LoadDeviceTypes();
        DeviceList = _context.Devices.Where(d => d.StatusId != 6).ToList();
        UPList = _context.UserPermissions.Where(d => d.StatusId != 6).ToList();
        users = _context.Users.ToList();
        return Page();
    }

    public IActionResult OnPostSubmitEdit()
    {
        _actionLoggerService = new ActionLoggerService(_context);
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");
        LoadPendingMessagesFromSession();
        LoadMessageTextFromSession();
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;
        
        int newActionId = 1;
        if(_context.ActionHistories.Count() > 0)
        {
            newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
        }
        foreach (var message in PendingMessages)
        {
            if (message.Id == 0)
            {
                // Nowa wiadomo��
                _context.Messages.Add(message);
                _context.SaveChanges();

                if (message.DeviceId.HasValue)
                {
                    var existingDevice = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId.Value);
                    if (existingDevice != null)
                    {
                        var newDevice = new Device
                        {
                            Name = existingDevice.Name,
                            DeviceTypeId = existingDevice.DeviceTypeId,
                            Serial = existingDevice.Serial,
                            StatusId = 1, // Ustawienie statusu na "oczekuj�cy"
                            StatusUpdate = DateTime.Now
                        };
                        _context.Devices.Add(newDevice);
                        _context.SaveChanges();

                        // Aktualizacja wiadomo�ci, aby wskazywa�a na nowe urz�dzenie
                        message.DeviceId = newDevice.Id;
                        _context.Messages.Update(message);
                    }
                }

                if (message.ProgramId.HasValue && message.PermissionId.HasValue)
                {
                    var userPermission = new UserPermission
                    {
                        UserId = loggedInUserId.Value,
                        PermissionId = message.PermissionId.Value,
                        StatusId = 1, // Status "oczekuj�cy"
                        RequestDate = DateTime.Now
                    };
                    _context.UserPermissions.Add(userPermission);
                    _context.SaveChanges();

                    // Przypisanie UPermissionId do wiadomo�ci
                    message.UPermissionsId = userPermission.Id;
                    _context.Messages.Update(message);
                }

                var isAdmin = _context.Users.FirstOrDefault(u => u.Id == HttpContext.Session.GetInt32("UserId")).IsAdmin == 1;
                var messageLink = new MessageLink
                {
                    ApplicationId = HttpContext.Session.GetInt32("ApplicationId") ?? 0,
                    MessageId = message.Id,
                    DegreeId = isAdmin ? -6 : _context.Degrees.First(d => d.Id == _context.Users.First(u => u.Id == loggedInUserId.Value).DegreeId)?.ManagerId ?? 0
                };
                _context.MessageLinks.Add(messageLink);
                _actionLoggerService.Log(
                    HttpContext.Session.GetInt32("UserId") ?? 0,
                    message.UPermissionsId,
                    message.DeviceId,
                    messageLink.ApplicationId,
                    2,
                    newActionId
                );
            }
            else
            {
                var isAdmin = _context.Users.FirstOrDefault(u => u.Id == HttpContext.Session.GetInt32("UserId")).IsAdmin == 1;
                var ml = _context.MessageLinks.FirstOrDefault(l => l.MessageId == message.Id);
                if (ml != null)
                {
                    ml.DegreeId = isAdmin ? -6 : ml.DegreeId;
                }
            }
        }
        foreach (var message in DeletedMessages)
        {
            if (message.DeviceId.HasValue)
            {
                var device = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId.Value);
                if (device != null)
                {
                    device.StatusId = 6; // Odrzucono
                    _context.Devices.Update(device);
                }

            }

            if (message.ProgramId.HasValue)
            {
                var userPermission = _context.UserPermissions.FirstOrDefault(up => up.Id == message.UPermissionsId);
                if (userPermission != null)
                {
                    userPermission.StatusId = 6; // Odrzucono
                    _context.UserPermissions.Update(userPermission);
                }

            }
            var ml = _context.MessageLinks.FirstOrDefault(m => m.MessageId == message.Id);
            ml.DegreeId = -10;
            _context.MessageLinks.Update(ml);
            _actionLoggerService.Log(
                HttpContext.Session.GetInt32("UserId") ?? 0,
                message.UPermissionsId,
                message.DeviceId,
                ml.ApplicationId,
                2,
                newActionId
            );
        }

        ApplicationId = HttpContext.Session.GetInt32("ApplicationId") ?? 0;
        var appDetails = _context.ApplicationDetails.FirstOrDefault(ad => ad.ApplicationId == ApplicationId);
        if (appDetails != null)
        {
            if (appDetails.Message != MessageText || appDetails.ExpireDate != ExpireDate)
            {
                appDetails.Message = MessageText;
                appDetails.ExpireDate = ExpireDate ?? DateTime.UnixEpoch;
                _actionLoggerService.Log(
                    HttpContext.Session.GetInt32("UserId") ?? 0,
                    null,
                    null,
                    ApplicationId,
                    2,
                    newActionId
                );
            }
        }

        _context.SaveChanges();
        PendingMessages.Clear();
        MessageText = string.Empty;
        ExpireDate = null;
        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        return RedirectToPage("/ManagerBrowseApplications");
    }

    private void LoadDeviceTypes()
    {
        DeviceTypeSelectList = _context.DeviceTypes
            .Select(dt => new SelectListItem
            {
                Value = dt.Id.ToString(),
                Text = dt.Name
            })
            .ToList();
    }


    // private void LoadPrograms()
    // {
    //     var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");
    //     LoggedInUserDepartmentName = _context.Departments.First(de => de.Id == _context.Degrees.First(d => d.Id == _context.Users.FirstOrDefault(u => u.Id == loggedInUserId.Value).DegreeId).DepartmentId).Name;
    //     ProducerSelectList = _context.Producents.Where(p => p.Name == LoggedInUserDepartmentName)
    //         .Select(p => new SelectListItem
    //         {
    //             Value = p.Id.ToString(),
    //             Text = p.Name
    //         })
    //         .ToList();
    //     ProgramSelectList = _context.Programs.Where(p => p.ProducerId.ToString() == ProducerSelectList.First().Value)
    //         .Select(p => new SelectListItem
    //         {
    //             Value = p.Id.ToString(),
    //             Text = p.Name
    //         })
    //         .ToList();
    // }

    private void LoadDevices()
    {
        DeviceSelectList = _context.Devices.Where(e => e.StatusId == 5)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            })
            .ToList();
    }

    private void LoadPermissions()
    {
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");
        var departmentId = _context.Degrees.FirstOrDefault(x => x.Id == _context.Users.First(u => u.Id == loggedInUserId).DegreeId).DepartmentId;
        /*if (loggedInUserId == null || !ProgramId.HasValue)
        {
            PermissionSelectList = new List<SelectListItem>();
            return;
        }

        // Pobieranie istniej�cych uprawnie� u�ytkownika (w tym oczekuj�cych i zaakceptowanych)
        var existingPermissions = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId) // Pomijamy odrzucone
            .Select(up => up.PermissionId)
            .ToHashSet();

        // Pobieranie uprawnie� oznaczonych jako "odrzucone"
        // Pobieranie uprawnie� oznaczonych jako "odrzucone"
        var rejectedPermissions = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId.Value && up.StatusId == 6)
            .Select(up => up.PermissionId)
            .ToHashSet();*/

        // Pobieranie dost�pnych uprawnie� dla wybranego programu
        PermissionSelectList = _context.PermissionGroups.Where(p => p.DepartmentId == departmentId)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name
            })
            .ToList();
    }
    public string GetDeviceName(int deviceId)
    {
        var device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);
        return device?.Name ?? "Nieznane urządzenie";
    }
    public string GetDeviceType(int deviceId)
    {
        var device = _context.DeviceTypes.FirstOrDefault(d => d.Id == _context.Devices.FirstOrDefault(e => e.Id == deviceId).DeviceTypeId);
        return device?.Name ?? "Nieznane urządzenie";
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
    private void SavePendingMessagesToSession()
    {
        var serializedMessages = JsonSerializer.Serialize(PendingMessages);
        HttpContext.Session.SetString("PendingMessages", serializedMessages);
        var serializedDM = JsonSerializer.Serialize(DeletedMessages);
        HttpContext.Session.SetString("DeletedMessages", serializedDM);
    }
    private void SaveMessageTextToSession()
    {
        HttpContext.Session.SetString("PendingMessageText", MessageText ?? string.Empty);
        HttpContext.Session.SetString("PendingExpireDate", ExpireDate?.ToString("o") ?? string.Empty);
    }
    private void LoadMessageTextFromSession()
    {
        MessageText = HttpContext.Session.GetString("PendingMessageText") ?? string.Empty;
        var expireDateStr = HttpContext.Session.GetString("PendingExpireDate");
        if (!string.IsNullOrEmpty(expireDateStr) && DateTime.TryParse(expireDateStr, out var dt))
            ExpireDate = dt;
        else
            ExpireDate = null;
    }
    private void LoadPendingMessagesFromSession()
    {
        var serializedMessages = HttpContext.Session.GetString("PendingMessages");
        if (!string.IsNullOrEmpty(serializedMessages))
        {
            PendingMessages = JsonSerializer.Deserialize<List<Message>>(serializedMessages) ?? new List<Message>();
        }
        var serializedDM = HttpContext.Session.GetString("DeletedMessages");
        if (!string.IsNullOrEmpty(serializedDM))
        {
            DeletedMessages = JsonSerializer.Deserialize<List<Message>>(serializedDM) ?? new List<Message>();
        }
    }
    public JsonResult OnGetPermissions(int programId)
    {
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");

        if (loggedInUserId == null)
        {
            return new JsonResult(new { success = false, message = "Nie jesteś zalogowany." });
        }

        var existingPermissions = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId.Value && up.StatusId != 6) // Pomijamy odrzucone
            .Select(up => up.PermissionId)
            .ToHashSet();

        // Pobieranie uprawnie� oznaczonych jako "odrzucone"
        var rejectedPermissions = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId.Value && up.StatusId == 6)
            .Select(up => up.PermissionId)
            .ToHashSet();

        var permissions = _context.Permissions
            .Where(p => p.ProgramId == programId && (!existingPermissions.Contains(p.Id) || rejectedPermissions.Contains(p.Id)))
            .Select(p => new { id = p.Id, name = p.Name })
            .ToList();

        return new JsonResult(new { success = true, permissions });
    }
}
