using System.Data.SqlTypes;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using webapp.Services;

namespace webapp.Pages
{
    public class IODBrowseApplicationsModel : PageModel
    {
        private readonly AppDbContext _context;

        public IODBrowseApplicationsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ApplicationViewModelI> Applications { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<Degree> Degrees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<ActionHistory> ActionHistories { get; set; } = new();
        public List<User> UsersAll { get; set; } = new();
        public IEnumerable<SelectListItem> UsersSelectList => Users.Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = d.FirstName + " " + d.LastName
        });

        [BindProperty]
        public int? SelectedUserId { get; set; } // Wybrany użytkownik
        public bool showApprove { get; set; }
        
        private ActionLoggerService _actionLoggerService;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public bool isLastPage { get; set; }

        private const int PageSize = 10;

        public void OnGet(int pageNumber = 1)
        {
            PageNumber = pageNumber;
            var loggedInUserId = HttpContext.Session.GetInt32("UserId");

            if (loggedInUserId == null)
            {
                Applications = new List<ApplicationViewModelI>();
                return;
            }

            var selectedUserId = HttpContext.Session.GetInt32("SelectedUserId");
            if (selectedUserId.HasValue)
            {
                SelectedUserId = selectedUserId;
            }

            Users = _context.Users
                .Where(u => _context.Messages.Any(m => m.UserId == u.Id))
                .ToList();

            Degrees = _context.Degrees.ToList();
            Departments = _context.Departments.ToList();
            ActionHistories = _context.ActionHistories.ToList();
            UsersAll = _context.Users.ToList();

            var applications = GetApplications()
                .Where(app => app.Messages.Any(m => _context.MessageLinks
                    .Any(ml => ml.MessageId == m.Id && (ml.DegreeId == -10 || ml.DegreeId == -11 || ml.DegreeId == -6 || ml.DegreeId == -7))));

            if (SelectedUserId.HasValue)
            {
                applications = applications.Where(app => app.Messages.Any(m => m.UserId == SelectedUserId.Value));
            }

            var totalApplications = applications.Count();
            isLastPage = PageNumber * PageSize >= totalApplications;

            Applications = applications
                .OrderByDescending(app => app.ApplicationId)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();
            
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


        public IActionResult OnPostApprove(int applicationId)
        {
            _actionLoggerService = new ActionLoggerService(_context);
            // Pobierz MessageLinks powiązane z applicationId
            var messageLinks = _context.MessageLinks
                .Where(ml => ml.ApplicationId == applicationId)
                .ToList();


            int newActionId = 1;
            if(_context.ActionHistories.Count() > 0)
            {
                newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
            }
            // Zaktualizuj DegreeId w MessageLink na DegreeId topManagera
            foreach (var messageLink in messageLinks)
            {
                var message = _context.Messages.FirstOrDefault(m => m.Id == messageLink.MessageId);
                if (message == null) continue;

                // Zaktualizuj status urządzeń
                if (messageLink.DegreeId == -6)
                {
                    messageLink.DegreeId = -7;
                }
                else
                {
                    messageLink.DegreeId = -11;
                }
                _actionLoggerService.Log(
                    HttpContext.Session.GetInt32("UserId") ?? 0,
                    message.UPermissionsId,
                    message.DeviceId,
                    messageLink.ApplicationId,
                    5,
                    newActionId
                );
            }
   
            
            _context.SaveChanges();
            return RedirectToPage();
        }

        

        private List<ApplicationViewModelI> GetApplications()
        {
            var messageLinks = _context.MessageLinks.ToList();
            var Devices = _context.Devices;
            var UserPermissions = _context.UserPermissions;
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

            Dictionary<int, bool?> approveMap = new Dictionary<int, bool?>();
            foreach (Message m in _context.Messages.ToList())
            {
                if (messageLinks.FirstOrDefault(ml => ml.MessageId == m.Id).DegreeId == -11 ||
                    messageLinks.FirstOrDefault(ml => ml.MessageId == m.Id).DegreeId == -7)
                {
                    approveMap[messageLinks.FirstOrDefault(ml => ml.MessageId == m.Id).ApplicationId] = false;
                }
                else
                {
                    approveMap[messageLinks.FirstOrDefault(ml => ml.MessageId == m.Id).ApplicationId] = true;
                }
            }
            var groupedLinks = messageLinks.GroupBy(ml => ml.ApplicationId);

            var applicationDetails = _context.ApplicationDetails;
            
            return groupedLinks.Select(group => new ApplicationViewModelI
            {
                showApprove = approveMap[group.Key] ?? true,
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
        public bool isRejected(int messageId)
        {
            bool isRejected = false;
            var message = _context.Messages.FirstOrDefault(m => m.Id == messageId);
            var device = _context.Devices.FirstOrDefault(d => d.Id == message.DeviceId);
            var userPermission = _context.UserPermissions.FirstOrDefault(up => up.Id == message.UPermissionsId);
            if (device != null)
            {
                if (device.StatusId == 6 || device.StatusId == 5)
                {
                    isRejected = true;
                }
            }
            else if (userPermission != null)
            {
                if (userPermission.StatusId == 6 || userPermission.StatusId == 5)
                {
                    isRejected = true;
                }
            }
            return isRejected;
        }
    }

}
public class ApplicationViewModelI
{
    public int ApplicationId { get; set; }
    public List<Message> Messages { get; set; } = new();
    public string messageText { get; set; } = string.Empty;
    public DateTime expirationDate { get; set; } = DateTime.UnixEpoch;
    public bool showApprove { get; set; }
}
