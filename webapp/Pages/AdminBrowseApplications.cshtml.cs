using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
using webapp.Models;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;

namespace webapp.Pages
{
    public class AdminBrowseApplicationsModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminBrowseApplicationsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<ApplicationViewModel> Applications { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<User> UsersAll { get; set; } = new();
        public List<Degree> Degrees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<MessageLink> MessageLinks { get; set; } = new();
        public List<ActionHistory> ActionHistories { get; set; } = new();
        public List<Login> Logins { get; set; } = new();
        
        public IEnumerable<SelectListItem> UsersSelectList => Users.Select(d => new SelectListItem
        {
            Value = d.Id.ToString(),
            Text = d.FirstName + " " + d.LastName + " " + d.Email
        });

        [BindProperty]
        public int? SelectedUserId { get; set; } // Wybrany użytkownik

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public bool isLastPage { get; set; } = false;
        public bool isAdmin { get; set; } = false; // Flaga, czy użytkownik jest administratorem

        public void OnGet(int? userId = null, int pageNumber = 1)
        {
            PageNumber = pageNumber;
            MessageLinks = _context.MessageLinks.ToList();
            HttpContext.Session.SetString("PendingMessages", "");
            Users = _context.Users
                .Where(u => _context.Messages.Any(m => m.UserId == u.Id))
                .ToList();

            Degrees = _context.Degrees.ToList();
            Departments = _context.Departments.ToList();
            ActionHistories = _context.ActionHistories.ToList();
            UsersAll = _context.Users.ToList();
            Logins = _context.Logins.ToList();

            var applications = GetApplications().Where(app => app.Messages.Count > 0);

            if (userId.HasValue)
            {
                SelectedUserId = userId;
                applications = applications.Where(app => app.Messages.Any(m => m.UserId == userId.Value));
            }

            var totalApplications = applications.Count();
            Applications = applications.Skip((PageNumber - 1) * PageSize).Take(PageSize).ToList();
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
            return RedirectToPage(new { userId = SelectedUserId });
        }
        public IActionResult OnPostRedirectToHistory(int ApplicationId)
        {
            HttpContext.Session.SetInt32("SelectedApplicationId", ApplicationId);
            return RedirectToPage("HistoryView");
        }
        public IActionResult OnPostRedirectToPDF(int ApplicationId)
        {
            HttpContext.Session.SetInt32("PDFApplicationId", ApplicationId);
            return RedirectToPage("ApplicationPreview");
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
        

        private List<ApplicationViewModel> GetApplications()
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

            var applicationDetails = _context.ApplicationDetails;
            var groupedLinks = messageLinks.GroupBy(ml => ml.ApplicationId);
            return groupedLinks.Select(group => new ApplicationViewModel
            {
                ApplicationId = group.Key,
                Messages = messages
                    .Where(m => group.Select(ml => ml.MessageId).Contains(m.Id))
                    .ToList(),
                messageText = applicationDetails.Where(a => a.ApplicationId == group.Key)
                    .Select(b => b.Message)
                    .FirstOrDefault() ?? string.Empty,
                expirationDate = applicationDetails.Where(a => a.ApplicationId == group.Key)
                    .Select(b => b.ExpireDate).First(),
                IsRejected = group.Any(ml => ml.DegreeId == -1),// Sprawdzenie, czy DegreeId == -1
                IsApproved = group.Any(ml => ml.DegreeId == -6 || ml.DegreeId == -7),
                IsDone = group.Any(ml => ml.DegreeId == -10 || ml.DegreeId == -11),
                IsChecked = group.Any(ml => ml.DegreeId == -7 || ml.DegreeId == -11)
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

        public string GetLogin(int userId, int programId)
        {
            var login = _context.Logins.FirstOrDefault(d => d.UserId == userId && d.ProgramId == programId);
            return login?.Username ?? string.Empty;
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
    }
    
}
