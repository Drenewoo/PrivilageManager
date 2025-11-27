using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using webapp.Services;

namespace webapp.Pages
{
    public class ManagerBrowseApplicationsModel : PageModel
    {
        private readonly AppDbContext _context;
        
        private ActionLoggerService _actionLoggerService;

        public ManagerBrowseApplicationsModel(AppDbContext context)
        {
            _actionLoggerService = new ActionLoggerService(context);
            _context = context;
        }

        public List<ApplicationViewModel> Applications { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Degree> Degrees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<ActionHistory> ActionHistories { get; set; } = new();
        public List<User> UsersAll { get; set; } = new();
        
        public Dictionary<int, string> serialMap = new();
        public Dictionary<int, string> loginMap = new();

        public bool IsAdmin = false;
        
        public IEnumerable<SelectListItem> UsersSelectList => Users.Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = d.FirstName + " " + d.LastName
        });

        [BindProperty]
        public int? SelectedUserId { get; set; } // Wybrany użytkownik
        [BindProperty]
        public int? DevId { get; set; }
        [BindProperty]
        public string Serial { get; set; }
        [BindProperty]
        public int? ApplicationId { get; set; }
        [BindProperty]
        public string Username  { get; set; }
        [BindProperty]
        public int? UPId { get; set; }
        
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public bool isLastPage { get; set; } = false;

        public void OnGet(int pageNumber = 1)
        {
            PageNumber = pageNumber;
            var loggedInUserId = HttpContext.Session.GetInt32("UserId");
            IsAdmin = HttpContext.Session.GetInt32("IsAdmin") == 1;
            if (loggedInUserId == null)
            {
                Applications = new List<ApplicationViewModel>();
                return;
            }

            var selectedUserId = HttpContext.Session.GetInt32("SelectedUserId");
            if (selectedUserId.HasValue)
            {
                SelectedUserId = selectedUserId;
            }

            string json = HttpContext.Session.GetString("SerialMap");

            if (!string.IsNullOrEmpty(json))
            {
                serialMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            }
            json = String.Empty;
            json = HttpContext.Session.GetString("LoginMap");
            if (!string.IsNullOrEmpty(json))
            {
                loginMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            }
            
            HttpContext.Session.SetString("PendingMessages", "");
            Users = _context.Users
                .Where(u => _context.Messages.Any(m => m.UserId == u.Id))
                .ToList();
            
            Degrees = _context.Degrees.ToList();
            Departments = _context.Departments.ToList();
            ActionHistories = _context.ActionHistories.ToList();
            UsersAll = _context.Users.ToList();

            var applicationsQuery = GetApplications().AsQueryable();
            applicationsQuery = applicationsQuery.Where(app => app.Messages.Count > 0 && (app.IsApproved != true || (IsAdmin && !app.IsDone )));

            if (SelectedUserId.HasValue)
            {
                applicationsQuery = applicationsQuery.Where(app => app.Messages.Any(m => m.UserId == SelectedUserId.Value));
            }

            var totalApplications = applicationsQuery.Count();
            Applications = applicationsQuery
                .OrderByDescending(app => app.ApplicationId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            
            

            isLastPage = PageNumber * PageSize >= totalApplications;
        }
        public IActionResult OnPostShowData(int? SelectedId)
        {
            var message = _context.Messages.FirstOrDefault(m => m.Id == SelectedId);
            if (message == null)
                return Page();
            if (message.DeviceId.HasValue)
            {
                HttpContext.Session.SetInt32("SelectedId", message.DeviceId.Value);
                return RedirectToPage("ManagerDeviceManager");
            }
            else if (message.UPermissionsId.HasValue)
            {
                HttpContext.Session.SetInt32("SelectedId", message.UPermissionsId.Value);
                return RedirectToPage("ManagerProgramManager");
            }

            return Page();
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

        public IActionResult OnPostSetSerial()
        {
            TempData["ExpandId"] = ApplicationId;
            TempData["ScrollToId"] = $"card2-{ApplicationId}";
            string json = HttpContext.Session.GetString("SerialMap");

            if (!string.IsNullOrEmpty(json))
            {
                serialMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            }
            if (DevId != null)
            {
                var device = _context.Devices.FirstOrDefault(d => d.Id == DevId);
                if (device != null)
                {
                    serialMap[device.Id] = Serial;
                }
            }
            json = JsonSerializer.Serialize(serialMap);
            HttpContext.Session.SetString("SerialMap", json);
            return RedirectToPage(new { userId = SelectedUserId });
        }
        public IActionResult OnPostSetUsername()
        {
            TempData["ExpandId"] = ApplicationId;
            TempData["ScrollToId"] = $"card2-{ApplicationId}";
            string json = HttpContext.Session.GetString("LoginMap");

            if (!string.IsNullOrEmpty(json))
            {
                loginMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            }
            if (UPId != null)
            {
                var userpermission = _context.UserPermissions.FirstOrDefault(d => d.Id == UPId);
                if (userpermission != null)
                {
                    loginMap[userpermission.Id] = Username;
                }
            }
            json = JsonSerializer.Serialize(loginMap);
            HttpContext.Session.SetString("LoginMap", json);
            return RedirectToPage(new { userId = SelectedUserId });
        }

        private int GetCurrentManagerDegreeId()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                throw new InvalidOperationException("Nie można pobrać UserId z sesji.");
                
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                throw new InvalidOperationException("Nie znaleziono użytkownika w bazie danych.");
            }

            return user.DegreeId;
        }
        public IActionResult OnPostApprove(int applicationId)
        {
            string json = HttpContext.Session.GetString("SerialMap");

            if (!string.IsNullOrEmpty(json))
            {
                serialMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
            }
            
            string json2 = HttpContext.Session.GetString("LoginMap");
            if (!string.IsNullOrEmpty(json2))
            {
                loginMap = JsonSerializer.Deserialize<Dictionary<int, string>>(json2);
            }
            // Pobierz MessageLinks powiązane z applicationId
            var messageLinks = _context.MessageLinks
                .Where(ml => ml.ApplicationId == applicationId)
                .ToList();

            int newActionId = 1;
            if(_context.ActionHistories.Count() > 0)
            {
                newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
            }

            int action = 1;
            // Zaktualizuj DegreeId w MessageLink na DegreeId topManagera
            foreach (var messageLink in messageLinks)
            {
                var message = _context.Messages.FirstOrDefault(m => m.Id == messageLink.MessageId);
                if (message == null) continue;
                if (_context.Devices.FirstOrDefault(d => d.Id == message.DeviceId)?.StatusId == 6 || _context.UserPermissions.FirstOrDefault(u => u.Id == message.UPermissionsId)?.StatusId == 6) continue;
                

                // Zaktualizuj status urządzeń


                if (HttpContext.Session.GetInt32("IsAdmin") == 1)
                {
                    action = 6;
                    if (messageLink.DegreeId == -7)
                    {
                        messageLink.DegreeId = -11;
                    }
                    else
                    {
                        messageLink.DegreeId = -10;
                    }
                    if (message.DeviceId.HasValue)
                    {
                        var device = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId.Value);
                        if (device != null && device.StatusId != _context.Statuses.Min(d => d.Id) + 5)
                        {
                            if (serialMap.ContainsKey(device.Id))
                            {
                                device.Serial = serialMap[device.Id];
                            }
                            
                            device.StatusId = _context.Statuses.Min(d => d.Id) + 6; // Status "Wykonano"
                            device.StatusUpdate = DateTime.Now;
                        }
                    }
                    

                    // Zaktualizuj status uprawnień
                    if (message.ProgramId.HasValue)
                    {
                        var userPermission = _context.UserPermissions.FirstOrDefault(up => up.Id == message.UPermissionsId);
                        if (userPermission != null && userPermission.StatusId != _context.Statuses.Min(d => d.Id) + 5)
                        {
                            userPermission.StatusId = _context.Statuses.Min(d => d.Id) + 6; // Status "Wykonano"
                            userPermission.ResponseDate = DateTime.Now;
                        }

                        if (loginMap.ContainsKey(userPermission.Id))
                        {
                            var permission = _context.UserPermissions.FirstOrDefault(d => d.Id == userPermission.Id).PermissionId;
                            var who = _context.UserPermissions.FirstOrDefault(d => d.Id == userPermission.Id).UserId;
                            var program = _context.Permissions.FirstOrDefault(d => d.Id == permission).ProgramId;
                            if (_context.Logins.Any(d => d.ProgramId == program && d.UserId == who))
                            {
                                var ls = _context.Logins.Where(d => d.ProgramId == program && d.UserId == who).ToList();
                                foreach (var l in ls)
                                {
                                    l.Username = loginMap[userPermission.Id];
                                }
                                
                            }
                            else
                            {
                                _context.Logins.Add(new Login()
                                {
                                    ProgramId = program,
                                    UserId = who,
                                    Username = loginMap[userPermission.Id]
                                });
                            }
                        }
                    }
                }
                else
                {
                    messageLink.DegreeId = -6; // Oznacz jako zaakceptowane
                    if (message.DeviceId.HasValue)
                    {
                        var device = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId.Value);
                        if (device != null &&  device.StatusId != _context.Statuses.Min(d => d.Id) + 5)
                        {
                            device.StatusId = _context.Statuses.Min(d => d.Id) + 1; // Status "Zatwierdzono"
                            device.StatusUpdate = DateTime.Now;
                        }
                    }

                    // Zaktualizuj status uprawnień
                    if (message.ProgramId.HasValue)
                    {
                        var userPermission = _context.UserPermissions.FirstOrDefault(up => up.Id == message.UPermissionsId);
                        if (userPermission != null &&  userPermission.StatusId != _context.Statuses.Min(d => d.Id) + 6)
                        {
                            userPermission.StatusId = _context.Statuses.Min(d => d.Id) + 1; // Status "Zatwierdzono"
                            userPermission.ResponseDate = DateTime.Now;
                        }
                    }
                }
                _actionLoggerService.Log(
                    HttpContext.Session.GetInt32("UserId") ?? 0,
                    message.UPermissionsId,
                    message.DeviceId,
                    messageLink.ApplicationId,
                    action,
                    newActionId
                );
            }

            
            _context.SaveChanges();
            return RedirectToPage();
        }

        public IActionResult OnPostEdit(int applicationId)
        {
            HttpContext.Session.SetInt32("EditApplicationId", applicationId);
            return RedirectToPage("EditApplication");
        }
        public IActionResult OnPostReject(int applicationId)
        {
            int newActionId = 1;
            if(_context.ActionHistories.Count() > 0)
            {
                newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
            }
            // Pobierz MessageLinks powiązane z applicationId
            var messageLinks = _context.MessageLinks
                .Where(ml => ml.ApplicationId == applicationId)
                .ToList();

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
                    }
                }
                messageLink.DegreeId = -1; // Oznacz jako odrzucone
                _actionLoggerService.Log(
                    HttpContext.Session.GetInt32("UserId") ?? 0,
                    message.UPermissionsId,
                    message.DeviceId,
                    messageLink.ApplicationId,
                    3,
                    newActionId
                );
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

        private List<ApplicationViewModel> GetApplications()
        {
            
            
            var messageLinks = _context.MessageLinks.ToList();
            var Devices = _context.Devices.Where(d => d.StatusId != _context.Statuses.Min(e => e.Id) + 5);
            var UserPermissions = _context.UserPermissions.Where(up => up.StatusId != _context.Statuses.Min(e => e.Id) + 5);
            var messages = _context.Messages.ToList();
            var messagesToDelete = new List<Message>();

            foreach (Message m in messages)
            {
                if(Devices.Any(d => d.Id == m.DeviceId) || UserPermissions.Any(up => up.Id == m.UPermissionsId)) { }
                else { messagesToDelete.Add(m); }
            }
            foreach (Message m in messagesToDelete)
            {
                messages.Remove(m);
            }

            
            var loggedInDegreeId = GetCurrentManagerDegreeId();
            var isAdmin = HttpContext.Session.GetInt32("IsAdmin") == 1;
            
            var groupedLinks = messageLinks.AsQueryable();
            int[] ids = {-6, -7};
            if (isAdmin)
            {
                groupedLinks = groupedLinks.Where(m => ids.Contains(m.DegreeId));
            }
            else
            {
                groupedLinks = groupedLinks.Where(m => m.DegreeId == loggedInDegreeId);
            }

            var groupedLinks2 = groupedLinks.GroupBy(ml => ml.ApplicationId);

            var applicationDetails = _context.ApplicationDetails;
            
            return groupedLinks2.Select(group => new ApplicationViewModel
            {
                ApplicationId = group.Key,
                messageText = applicationDetails.Where(a => a.ApplicationId == group.Key)
                    .Select(b => b.Message)
                    .FirstOrDefault() ?? string.Empty,
                expirationDate = applicationDetails.Where(a => a.ApplicationId == group.Key)
                    .Select(b => b.ExpireDate).First(),
                Messages = messages
                    .Where(m => group.Select(ml => ml.MessageId).Contains(m.Id))
                    .ToList()
            })
            .OrderByDescending(app => app.ApplicationId)
            .ToList();
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
        public string GetSerial(int deviceId)
        {
            var device = _context.Devices.FirstOrDefault(d => d.Id == deviceId);
            if (device?.Serial == String.Empty)
            {
                if(serialMap.ContainsKey(deviceId))
                {
                    return serialMap[deviceId];
                }
                else
                {
                    return string.Empty;
                }
            }
            return device?.Serial ?? string.Empty;
        }

        public string GetUsername(int uPermissionsId)
        {
            var permission = _context.UserPermissions.FirstOrDefault(d => d.Id == uPermissionsId).PermissionId;
            var who = _context.UserPermissions.FirstOrDefault(d => d.Id == uPermissionsId).UserId;
            var program = _context.Permissions.FirstOrDefault(d => d.Id == permission).ProgramId;
            var userLogins = _context.Logins.Where(l => l.UserId == who && l.ProgramId == program);
            if (!userLogins.Any())
            {
                if (loginMap.ContainsKey(uPermissionsId))
                {
                    return loginMap[uPermissionsId];
                }
                else
                {
                    return String.Empty;
                }
            }
            return userLogins.First()?.Username ?? String.Empty;
        }
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
        public bool Expand { get; set; } = false;
        public bool? IsChecked { get; set; } = false;
    }
}
