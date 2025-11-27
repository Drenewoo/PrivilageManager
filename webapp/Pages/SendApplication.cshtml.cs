using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using webapp.Services;


namespace webapp.Pages;

public class SendApplicationModel : PageModel
{
    private readonly AppDbContext _context;

    public SendApplicationModel(AppDbContext context)
    {
        _context = context;
    }



    [BindProperty]
    public List<Message> PendingMessages { get; set; } = new();

    [BindProperty]
    public int? ProgramId { get; set; }
    [BindProperty]
    public int? ProducerId { get; set; }

    [BindProperty]
    public int? DeviceId { get; set; }

    [BindProperty]
    public int? PermissionId { get; set; }

    [BindProperty]
    public string? MessageText { get; set; } // <-- teraz wspólne dla całości

    [BindProperty]
    [DataType(DataType.Date)]
    public DateTime? ExpireDate { get; set; }

    public IEnumerable<SelectListItem> ProgramSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> ProducerSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> PermissionSelectList { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> DeviceTypeSelectList { get; set; } = new List<SelectListItem>();

    public List<User> users = new();
        
    private ActionLoggerService _actionLoggerService;
    
    public string LoggedInUserDepartmentName { get; set; } = string.Empty;

    public void OnGet()
    {
        LoadPendingMessagesFromSession();
        //LoadPrograms();
        LoadDevices();
        LoadPermissions();
        LoadDeviceTypes();

        users = _context.Users.ToList();
    }


    public IActionResult OnPostAddMessage(string type)
    {
        // Pobierz aktualne wartości z formularza (nie są automatycznie bindowane przy tym handlerze)
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;

        LoadPendingMessagesFromSession();
        
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");

        if (loggedInUserId == null)
        {
            ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany.");
            return Page();
        }
        // SaveMessageTextToSession() przeniesione niżej

        if (type == "Program" && PermissionId.HasValue)
        {
            foreach (var pid in _context.PermissionGroups.First(p => p.Id == PermissionId.Value).PermissionIds)
            {
                if (!_context.UserPermissions.Any(d => (d.StatusId == 1 || d.StatusId == 2 || d.StatusId == 7) && d.UserId == loggedInUserId && d.PermissionId == pid))
                {
                    if (PendingMessages.All(m => m.PermissionId != pid))
                    {
                        PendingMessages.Add(new Message
                        {
                            ProgramId = _context.Permissions.FirstOrDefault(p => p.Id == pid).ProgramId,
                            PermissionId = pid
                        });
                    }
                    else
                    {
                        //ModelState.AddModelError(string.Empty, "To uprawnienie już znajduje się na liście.");
                    }
                }
            }
        }
        else if (type == "Device" && DeviceId.HasValue)
        {
            PendingMessages.Add(new Message
            {
                DeviceId = DeviceId
            });
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Wszystkie wymagane pola muszą być wypełnione.");
        }

        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        //LoadPrograms();
        LoadDeviceTypes();
        LoadDevices();
        LoadPermissions();
        return Page();
    }


    public IActionResult OnPostRemoveMessage(int messageIndex)
    {
        // Pobierz aktualne wartości z formularza (nie są automatycznie bindowane przy tym handlerze)
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;

        LoadPendingMessagesFromSession();
        // SaveMessageTextToSession() przeniesione niżej

        if (messageIndex >= 0 && messageIndex < PendingMessages.Count)
        {
            PendingMessages.RemoveAt(messageIndex);
        }

        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        //LoadPrograms();
        LoadDeviceTypes();
        LoadDevices();
        LoadPermissions();
        return Page();
    }


    public IActionResult OnPostSubmitApplication()
    {
        _actionLoggerService = new ActionLoggerService(_context);
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");

        if (loggedInUserId == null)
        {
            ModelState.AddModelError(string.Empty, "Nie jesteś zalogowany.");
            return Page();
        }
        LoadPendingMessagesFromSession();
        LoadMessageTextFromSession();
        
        MessageText = Request.Form["MessageText"];
        if (DateTime.TryParse(Request.Form["ExpireDate"], out var expireDate))
            ExpireDate = expireDate;
        else
            ExpireDate = null;

        int newApplicationId = (_context.MessageLinks.Max(ml => (int?)ml.ApplicationId) ?? 0) + 1;
        int newActionId = 1;
        if(_context.ActionHistories.Count() > 0)
        {
            newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
        }
        foreach (var message in PendingMessages)
        {
            message.UserId = loggedInUserId.Value;
            message.RequestDate = DateTime.Now;
            _context.Messages.Add(message);
            _context.SaveChanges(); // Zapisujemy, aby uzyska� MessageId

            // Tworzenie nowego urz�dzenia, je�li wniosek dotyczy urz�dzenia
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
                        StatusId = _context.Statuses.Min(u => u.Id), // Ustawienie statusu na "oczekuj�cy"
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
                    StatusId = _context.Statuses.Min(u => u.Id), // Status "oczekuj�cy"
                    RequestDate = DateTime.Now
                };
                _context.UserPermissions.Add(userPermission);
                _context.SaveChanges();

                // Przypisanie UPermissionId do wiadomo�ci
                message.UPermissionsId = userPermission.Id;
                _context.Messages.Update(message);
            }

            // Dodanie MessageLink do bazy
            var messageLink = new MessageLink
            {
                ApplicationId = newApplicationId,
                MessageId = message.Id,
                DegreeId = _context.Degrees.First(d => d.Id == _context.Users.First(u => u.Id == loggedInUserId.Value).DegreeId)?.ManagerId ?? 0
            };
            _context.MessageLinks.Add(messageLink);
            _actionLoggerService.Log(
                HttpContext.Session.GetInt32("UserId") ?? 0,
                message.UPermissionsId,
                message.DeviceId,
                messageLink.ApplicationId,
                4,
                newActionId
            );
        }
        // Dodanie ApplicationDetails z MessageText i ExpireDate
        var appDetails = new ApplicationDetails
            {
                ApplicationId = newApplicationId,
                Message = MessageText ?? String.Empty,
                ExpireDate = ExpireDate ?? DateTime.UnixEpoch
            };
        _context.ApplicationDetails.Add(appDetails);
        
        _context.SaveChanges();
        PendingMessages.Clear();
        MessageText = string.Empty;
        ExpireDate = null;
        SavePendingMessagesToSession();
        SaveMessageTextToSession();
        TempData["SuccessMessage"] = "Wniosek został pomyślnie złożony.";
        return RedirectToPage("/Index");
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
    //     if (ProducerSelectList.Any())
    //     {
    //         ProgramSelectList = _context.Programs
    //             .Where(p => p.ProducerId.ToString() == ProducerSelectList.First().Value)
    //             .Select(p => new SelectListItem
    //             {
    //                 Value = p.Id.ToString(),
    //                 Text = p.Name
    //             })
    //             .ToList();
    //     }
    // }

    private void LoadDevices()
    {
        DeviceSelectList = _context.Devices.Where(e => e.StatusId == _context.Statuses.Min(e => e.Id) + 4)
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
        }*/

        /*// Pobieranie istniej�cych uprawnie� u�ytkownika (w tym oczekuj�cych i zaakceptowanych)
        var existingPermissions = _context.UserPermissions
            .Where(up => up.UserId == loggedInUserId.Value && up.StatusId != 6) // Pomijamy odrzucone
            .Select(up => up.PermissionId)
            .ToHashSet();

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
    public JsonResult OnGetPermissions(int programId)
    {
        var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");

        if (loggedInUserId == null)
        {
            return new JsonResult(new { success = false, message = "Nie jesteś zalogowany." });
        }
        var DeletedMessages = new List<Message>();
        var serializedDM = HttpContext.Session.GetString("DeletedMessages");
        if (!string.IsNullOrEmpty(serializedDM))
        {
            DeletedMessages = JsonSerializer.Deserialize<List<Message>>(serializedDM) ?? new List<Message>();
        }

        var DMUP = DeletedMessages.Select(dm => dm.UPermissionsId).ToList();
        var PID = DeletedMessages.Select(dm => dm.PermissionId).ToList();

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
            .Where(p => p.ProgramId == programId && (!existingPermissions.Contains(p.Id) || PID.Contains(p.Id)))
            .Select(p => new { id = p.Id, name = p.Name })
            .ToList();

        return new JsonResult(new { success = true, permissions });
    }
    public JsonResult OnGetDevices(int deviceTypeId)
    {
        var devices = _context.Devices.Where(d => d.StatusId == 5)
            .Where(d => d.DeviceTypeId == deviceTypeId)
            .Select(d => new { id = d.Id, name = d.Name })
            .ToList();

        return new JsonResult(new { success = true, devices });
    }
    // public JsonResult OnGetPrograms(int programId)
    // {
    //     var loggedInUserId = HttpContext.Session.GetInt32("ApplicationUserId");
    //
    //     if (loggedInUserId == null)
    //     {
    //         return new JsonResult(new { success = false, message = "Nie jeste� zalogowany." });
    //     }
    //
    //     var permissions = _context.Programs.Where(p => p.ProducerId == programId);
    //
    //     return new JsonResult(new { success = true, permissions });
    // }
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
    }
    private void SaveMessageTextToSession()
    {
        HttpContext.Session.SetString("PendingMessageText", MessageText ?? string.Empty);
        HttpContext.Session.SetString("PendingExpireDate", ExpireDate?.ToString("o") ?? string.Empty);
    }
    private void LoadPendingMessagesFromSession()
    {
        var serializedMessages = HttpContext.Session.GetString("PendingMessages");
        if (!string.IsNullOrEmpty(serializedMessages))
        {
            PendingMessages = JsonSerializer.Deserialize<List<Message>>(serializedMessages) ?? new List<Message>();
        }
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

}
