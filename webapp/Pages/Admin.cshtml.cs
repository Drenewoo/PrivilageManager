using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using webapp.Data;
using webapp.Models;
using webapp.Services;

namespace webapp.Pages;

public class AdminModel : PageModel
{
    private readonly AppDbContext _context;

    public AdminModel(AppDbContext context)
    {
        _context = context;
    }

    private int pageSize = 10;
    public int PageNumber { get; set; } = 1;
    public bool isLastPage { get; set; } = false;

    public string ActiveTab { get; set; } = "s";
    public List<User> Users { get; set; } = new();
    public List<Degree> Degrees { get; set; } = new();
    public List<Degree> AllDegrees { get; set; } = new();
    public List<Programs> Programs { get; set; } = new();
    public List<Permission> Permissions { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<UserPermission> UserPermissions { get; set; } = new();
    public List<PermissionGroup> PermissionGroups { get; set; } = new();
    public int? SelectedDepartmentId { get; set; }
    public int? SelectedUserId { get; set; }
    public int? SelectedId { get; set; }
    public List<Producent> Producents { get; set; } = new();
    public PermissionGroup? SelectedProducent { get; set; } //@TODO: ZMIEŃ NAZWĘ
    
    [BindProperty]
    public int PGPid { get; set; }
    [BindProperty]
    public int PGid { get; set; }

    [BindProperty] 
    public int Permission2Group { get; set; }
    [BindProperty]
    public int PermissionGroup2Add { get; set; }
    public IEnumerable<SelectListItem> permissionsSelectList; 
    
    public IEnumerable<SelectListItem> DepartmentSelectList => Departments.Select(d => new SelectListItem
    {
        Value = d.Id.ToString(),
        Text = d.Name
    });
    
    public IEnumerable<SelectListItem> UsersSelectList => Users.Select(d => new SelectListItem
    {
        Value = d.Id.ToString(),
        Text = d.FirstName + " " + d.LastName + " " + d.Email
    });
    public List<Device> Devices { get; set; } = new();
    public List<DeviceType> DeviceTypes { get; set; } = new();
    public Device? SelectedDevice { get; set; }
    public DeviceType? SelectedDeviceType { get; set; }
    public int? SelectedDeviceTypeId { get; set; }
    public int? SelectedProducerId { get; set; }
    public List<Status> Statuses { get; set; } = new();
    public bool ShowAllDevices { get; set; } = false;
    
    public ActionLoggerService _actionLoggerService { get; set; }

    public IActionResult OnGet()
    {
        PageNumber = HttpContext.Session.GetInt32("PageNumber") ?? 1;
        if (HttpContext.Session.GetInt32("IsAdmin") != 1)
        {
            return RedirectToPage("/Index");
        }
        TempData.Keep(); // Zachowaj inne dane w TempData
        TempData.Remove("ScrollToId"); // Usuń ScrollToId po użyciu
        LoadData();
        return Page();
    }

    public void OnPost()
    {
        LoadData();
    }

    private void LoadData()
    {
        LoadDevices();
        isLastPage = false;
        Statuses = _context.Statuses.ToList();
        Departments = _context.Departments.ToList();
        Degrees = _context.Degrees.ToList();
        AllDegrees = Degrees.ToList();
        Users = _context.Users.ToList();
        Programs = _context.Programs.ToList();
        Permissions = _context.Permissions.ToList();
        Producents = _context.Producents.ToList();
        PermissionGroups = _context.PermissionGroups.ToList();
        if (ShowAllDevices)
        {
            Devices = _context.Devices.Where(d => d.StatusId != 5 && d.StatusId != 6).ToList();
        }
        else
        {
            Devices = _context.Devices.Where(d => d.StatusId == 5).ToList();
        }

    }
    private void LoadDevices()
    {
        Devices = _context.Devices.ToList();
        DeviceTypes = _context.DeviceTypes.ToList();
    }
    public void OnPostShowUsers(int? SelectedDepartmentId, int pageNumber = 1)
    {
        LoadData();
        ActiveTab = "Users";
        PageNumber = pageNumber;
        // Filtrowanie u ytkownik w po plac wkach
        if (SelectedDepartmentId.HasValue)
        {
            var activeDegree = Degrees.FirstOrDefault(d => d.DepartmentId == SelectedDepartmentId);
            if (activeDegree != null)
            {
                Users = _context.Users.Where(u => u.DegreeId == activeDegree.Id).ToList();
            }
            else
            {
                Users = new();
            }
        }
        else
        {
            Users = _context.Users.ToList();
        }
        if (pageNumber * pageSize < Users.Count)
        {
            Users = Users.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            Users = Users.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        
        this.SelectedDepartmentId = SelectedDepartmentId; // Przypisanie do w a ciwo ci modelu
    }

    public void OnPostShowDegrees(int? SelectedDepartmentId, int pageNumber = 1)
    {
        LoadData();
        ActiveTab = "Degrees";
        PageNumber = pageNumber;
        if (SelectedDepartmentId.HasValue)
        {
            var filteredDegrees = Degrees.Where(d => d.DepartmentId == SelectedDepartmentId).ToList();
            if (pageNumber * pageSize < filteredDegrees.Count)
            {
                Degrees = filteredDegrees.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                isLastPage = false;
            }
            else
            {
                Degrees = filteredDegrees.Skip((pageNumber - 1) * pageSize).ToList();
                isLastPage = true;
            }
        }
        else
        {
            if (pageNumber * pageSize < Degrees.Count)
            {
                Degrees = Degrees.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                isLastPage = false;
            }
            else
            {
                Degrees = Degrees.Skip((pageNumber - 1) * pageSize).ToList();
                isLastPage = true;
            }
        }
        this.SelectedDepartmentId = SelectedDepartmentId; // Przypisanie do w a ciwo ci modelu

    }
    public void OnPostShowDepartments(int pageNumber = 1)
    {
        LoadData();
        PageNumber = pageNumber;
        if (pageNumber * pageSize < Departments.Count)
        {
            Departments = Departments.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            Departments = Departments.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        ActiveTab = "Departments";
    }

    public void OnPostShowPrograms(int? SelectedProducerId, int pageNumber = 1)
    {
        this.SelectedProducerId = SelectedProducerId;

        LoadData();
        PageNumber = pageNumber;
        if (pageNumber * pageSize < Programs.Count)
        {
            Programs = Programs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            Programs = Programs.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        ActiveTab = "Programs";
    }

    public void OnPostShowPermissions(int? SelectedDepartmentId, int pageNumber = 1)
    {
        LoadData();
        PageNumber = pageNumber;
        ActiveTab = "Permissions";
        if (SelectedDepartmentId.HasValue)
        {
            Permissions = _context.Permissions.Where(p => p.ProgramId == SelectedDepartmentId).ToList();
        }
        else
        {
            Permissions = _context.Permissions.ToList();
        }
        if (pageNumber * pageSize < Permissions.Count)
        {
            Permissions = Permissions.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            Permissions = Permissions.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        this.SelectedDepartmentId = SelectedDepartmentId;
    }
    public void OnPostShowAddUser()
    {
        LoadData();
        ActiveTab = "AddUser";
    }
    public void OnPostShowEditUser(int id)
    {
        LoadData();
        SelectedId = id;
        ActiveTab = "EditUser";
    }
    public void OnPostShowAddDepartment()
    {
        LoadData();
        ActiveTab = "AddDepartment";
    }
    public void OnPostShowEditDepartment(int id)
    {
        LoadData();
        SelectedId = id;
        ActiveTab = "EditDepartment";
    }
    public void OnPostShowAddDegree()
    {
        LoadData();
        ActiveTab = "AddDegree";
    }
    public void OnPostShowEditDegree(int id)
    {
        LoadData();
        SelectedId = id;
        ActiveTab = "EditDegree";
    }
    public void OnPostShowAddProgram()
    {
        LoadData();
        ActiveTab = "AddProgram";
    }
    public void OnPostShowEditProgram(int id)
    {
        LoadData();
        SelectedId = id;
        ActiveTab = "EditProgram";
    }
    public void OnPostShowAddPermission()
    {
        LoadData();
        ActiveTab = "AddPermission";
    }
    public void OnPostShowEditPermission(int id)
    {
        LoadData();
        SelectedId = id;
        ActiveTab = "EditPermission";
    }
    public IActionResult OnPostAddUser(string username, string firstName, string lastName, string email, int degreeId, string password, int isAdmin)
    {
        if (!_context.Users.Any(u => u.Username == username))
        {
            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = username,
                FirstName = firstName == null ? string.Empty : firstName,
                LastName = lastName == null ? string.Empty : lastName,
                Email = email == null ? string.Empty : email,
                DegreeId = degreeId,
                IsAdmin = isAdmin
            };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);

            _context.Users.Add(newUser);
            _context.SaveChanges();
        }

        ActiveTab = "Users";
        OnPostShowUsers(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostEditUser(int id, string username, string firstName, string lastName, string email, int degreeId, string password, int isAdmin)
    {
        var loggedInUser = HttpContext.Session.GetInt32("UserId");
        var loggedInUserU = _context.Users.FirstOrDefault(u => u.Id == loggedInUser)?.Username;
        if (_context.Users.FirstOrDefault(u => u.Id == id).Username == username || !_context.Users.Any(u => u.Username == username))
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (password == string.Empty || password == null) password = user.PasswordHash;
            else
            {
                var passwordHasher = new PasswordHasher<User>();
                password = passwordHasher.HashPassword(user, password);
            }

            if (user != null)
            {
                user.Username = username;
                user.FirstName = firstName == null ? string.Empty : firstName;
                user.LastName = lastName == null ? string.Empty : lastName;
                user.Email = email == null ? string.Empty : email;
                user.DegreeId = degreeId;
                user.PasswordHash = password;
                user.IsAdmin = isAdmin;
                if (id == _context.Users.Min(u => u.Id))
                {
                    user.Username = "admin";
                    user.DegreeId = _context.Degrees.Min(d => d.Id);
                    user.IsAdmin = 1;
                }

                _context.SaveChanges();
            }
        }

        ActiveTab = "Users";
        OnPostShowUsers(SelectedDepartmentId, PageNumber);

        return Page();
    }

    public IActionResult OnPostDeleteUser(int id)
    {
        if (id == _context.Users.Min(u => u.Id))
        {
            ActiveTab = "Users";
            LoadData();

            return Page();
        }
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        ActiveTab = "Users";
        OnPostShowUsers(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostAddDegree(string name, int departmentId, int managerId)
    {
        if (!_context.Degrees.Any(d => d.Name == name && d.DepartmentId == departmentId))
        {
            var newDegree = new Degree
            {
                Name = name,
                DepartmentId = departmentId,
                ManagerId = managerId

            };
            _context.Degrees.Add(newDegree);
            _context.SaveChanges();
        }

        ActiveTab = "Degrees";
        OnPostShowDegrees(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostEditDegree(int id, string name, int departmentId, int managerId)
    {
        if ((_context.Degrees.Any(d => d.Name == name && d.DepartmentId == departmentId) &&
             _context.Degrees.FirstOrDefault(d => d.Name == name && d.DepartmentId == departmentId)?.Name == name) ||
            !_context.Degrees.Any(d => d.Name == name && d.DepartmentId == departmentId))
        {
            var degree = _context.Degrees.FirstOrDefault(d => d.Id == id);
            if (degree != null)
            {
                degree.Name = name;
                degree.DepartmentId = departmentId;
                degree.ManagerId = managerId;
                _context.SaveChanges();
            }
        }

        ActiveTab = "Degrees";
        OnPostShowDegrees(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostDeleteDegree(int id)
    {
        if (id == _context.Degrees.Min(u => u.Id))
        {
            return Page();
        }
        var degree = _context.Degrees.FirstOrDefault(d => d.Id == id);
        if (degree != null)
        {
            _context.Degrees.Remove(degree);
            _context.SaveChanges();
        }
        ActiveTab = "Degrees";
        OnPostShowDegrees(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostAddDepartment(string name)
    {
        if (!_context.Departments.Any(d => d.Name == name))
        {
            var newDepartment = new Department
            {
                Name = name
            };
            _context.Departments.Add(newDepartment);
            _context.SaveChanges();
        }

        if (!_context.Producents.Any(p => p.Name == name))
        {
            var newProducent = new Producent
            {
                Name = name
            };
            _context.Producents.Add(newProducent);
            _context.SaveChanges();
        }

        ActiveTab = "Departments";
        OnPostShowDepartments(PageNumber);

        return Page();
    }
    public IActionResult OnPostEditDepartment(int id, string name)
    {
        var oldName = "";
        if ((_context.Departments.Any(d => d.Name == name) &&
             _context.Departments.FirstOrDefault(d => d.Id == id)?.Name == name) ||
            !_context.Departments.Any(d => d.Name == name))
        {
            var department = _context.Departments.FirstOrDefault(d => d.Id == id);
            oldName = department.Name;
            if (department != null)
            {
                department.Name = name;
                _context.SaveChanges();
            }
            var producent = _context.Producents.FirstOrDefault(d => d.Name == oldName);
            if (producent != null)
            {
                producent.Name = name;
                _context.SaveChanges();
            }
        }

        ActiveTab = "Departments";
        OnPostShowDepartments(PageNumber);

        return Page();
    }
    public IActionResult OnPostDeleteDepartment(int id)
    {
        if (id == _context.Departments.Min(u => u.Id))
        {
            return Page();
        }
        var department = _context.Departments.FirstOrDefault(d => d.Id == id);
        if (department != null)
        {
            _context.Departments.Remove(department);
            _context.SaveChanges();
        }
        ActiveTab = "Departments";
        OnPostShowDepartments(PageNumber);

        return Page();
    }
    public IActionResult OnPostAddProgram(string name, int producerId)
    {
        if (!_context.Programs.Any(p => p.Name == name))
        {
            var newProgram = new Programs
            {
                Name = name
            };
            _context.Programs.Add(newProgram);
            _context.SaveChanges();
        }

        ActiveTab = "Programs";
        OnPostShowPrograms(SelectedProducerId, PageNumber);

        return Page();
    }

    public IActionResult OnPostEditProgram(int id, string name, int producerId)
    {
        if ((_context.Programs.Any(p => p.Name == name) &&
             _context.Programs.FirstOrDefault(p => p.Id == id)?.Name == name) ||
            !_context.Programs.Any(p => p.Name == name))
        {
            var program = _context.Programs.FirstOrDefault(p => p.Id == id);
            if (program != null)
            {
                program.Name = name;
                _context.SaveChanges();
            }
        }

        ActiveTab = "Programs";
        OnPostShowPrograms(SelectedProducerId, PageNumber);

        return Page();
    }
    public IActionResult OnPostDeleteProgram(int id)
    {
        var program = _context.Programs.FirstOrDefault(p => p.Id == id);
        if (program != null)
        {
            _context.Programs.Remove(program);
            _context.SaveChanges();
        }
        ActiveTab = "Programs";
        OnPostShowPrograms(SelectedProducerId, PageNumber);

        return Page();
    }
    public IActionResult OnPostAddPermission(string name, int programId)
    {
        if (!_context.Permissions.Any(p => p.Name == name && p.ProgramId == programId))
        {
            var newPermission = new Permission
            {
                Name = name,
                ProgramId = programId
            };
            _context.Permissions.Add(newPermission);
            _context.SaveChanges();
        }

        ActiveTab = "Permissions";
        OnPostShowPermissions(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostEditPermission(int id, string name, int programId)
    {
        if ((_context.Permissions.Any(p => p.Name == name && p.ProgramId == programId) &&
             _context.Permissions.FirstOrDefault(p => p.Id == id)?.Name == name) ||
            !_context.Permissions.Any(p => p.Name == name && p.ProgramId == programId))
        {
            var permission = _context.Permissions.FirstOrDefault(p => p.Id == id);
            if (permission != null)
            {
                permission.Name = name;
                permission.ProgramId = programId;
                _context.SaveChanges();
            }
        }

        ActiveTab = "Permissions";
        OnPostShowPermissions(SelectedDepartmentId, PageNumber);

        return Page();
    }
    public IActionResult OnPostDeletePermission(int id)
    {
        var permission = _context.Permissions.FirstOrDefault(p => p.Id == id);
        if(permission != null)
        {
            _context.Permissions.Remove(permission);
            _context.SaveChanges();
        }
        ActiveTab = "Permissions";
        OnPostShowPermissions(SelectedDepartmentId, PageNumber);

        return Page();
    }
    
    public void OnPostShowProducents(int pageNumber = 1)
    {
        LoadData();
        PageNumber = pageNumber;
        if (pageNumber * pageSize < PermissionGroups.Count)
        {
            PermissionGroups = PermissionGroups.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            PermissionGroups = PermissionGroups.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        ActiveTab = "Producents";
    }

    public void OnPostShowAddProducent()
    {
        LoadData();
        ActiveTab = "AddProducent";
    }

    public void OnPostShowEditProducent(int id)
    {
        LoadData();
        SelectedId = id;
        SelectedProducent = _context.PermissionGroups.FirstOrDefault(p => p.Id == id);
        ActiveTab = "EditProducent";
    }

    public IActionResult OnPostAddProducent(string name)
    {
        if (!_context.PermissionGroups.Any(p => p.Name == name))
        {
            var newProducent = new PermissionGroup()
            {
                Name = name,
                DepartmentId = Int32.Parse(Request.Form["department"])
            };
            _context.PermissionGroups.Add(newProducent);
            _context.SaveChanges();
        }

        ActiveTab = "Producents";
        OnPostShowProducents(PageNumber);

        return Page();
    }

    public IActionResult OnPostEditProducent(int id, string name)
    {
        if ((_context.PermissionGroups.Any(p => p.Name == name) &&
             _context.PermissionGroups.FirstOrDefault(p => p.Id == id)?.Name == name) ||
            !_context.PermissionGroups.Any(p => p.Name == name))
        {
            var producent = _context.PermissionGroups.FirstOrDefault(p => p.Id == id);
            if (producent != null)
            {
                producent.Name = name;
                producent.DepartmentId = Int32.Parse(Request.Form["department"]);
                _context.SaveChanges();
            }
        }

        ActiveTab = "Producents";
        OnPostShowProducents(PageNumber);

        return Page();
    }

    public IActionResult OnPostDeleteProducent(int id)
    {
        var producent = _context.PermissionGroups.FirstOrDefault(p => p.Id == id);
        if (producent != null)
        {
            _context.PermissionGroups.Remove(producent);
            _context.SaveChanges();
        }
        ActiveTab = "Producents";
        OnPostShowProducents(PageNumber);

        return Page();
    }
    public void OnPostShowDevices(bool? ShowAll, int pageNumber = 1)
    {
        if (ShowAll != null)
        {
            ShowAllDevices = true;
        }
        else
        {
            ShowAllDevices = false;
        }
        LoadData();
        PageNumber = pageNumber;
        if (pageNumber * pageSize < Devices.Count)
        {
            Devices = Devices.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            Devices = Devices.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        ActiveTab = "Devices";
    }

    public void OnPostShowAddDevice()
    {
        LoadDevices();
        ActiveTab = "AddDevice";
    }

    public void OnPostShowEditDevice(int id)
    {
        LoadDevices();
        SelectedId = id;
        SelectedDevice = _context.Devices.FirstOrDefault(d => d.Id == id);
        ActiveTab = "EditDevice";
    }

    public IActionResult OnPostAddDevice(string name, int deviceTypeId, string? serial)
    {
        if (!_context.Devices.Any(d => d.Id == deviceTypeId && d.Name == name && d.Serial == serial))
        {
            var newDevice = new Device
            {
                Name = name,
                DeviceTypeId = deviceTypeId,
                Serial = serial ?? string.Empty,
                StatusId = _context.Statuses.Min(e => e.Id) + 4
            };
            _context.Devices.Add(newDevice);
            _context.SaveChanges();
        }

        OnPostShowDevices(ShowAllDevices, PageNumber);

        return Page();
    }

    public IActionResult OnPostEditDevice(int id, string name, int deviceTypeId, string? serial)
    {
        if ((_context.Devices.Any(d => d.Name == name && d.DeviceTypeId == deviceTypeId && d.Serial == serial) &&
             _context.Devices.FirstOrDefault(d => d.Id == id)?.Name == name) || !_context.Devices.Any(d =>
                d.Name == name && d.DeviceTypeId == deviceTypeId && d.Serial == serial))
        {
            var device = _context.Devices.FirstOrDefault(d => d.Id == id);
            if (device != null)
            {
                var oldName = device.Name;
                device.Serial = serial ?? string.Empty;
                var devices = _context.Devices.Where(d => d.Name == oldName).ToList();
                foreach (var d in devices)
                {
                    d.Name = name;
                    d.DeviceTypeId = deviceTypeId;
                }

                _context.SaveChanges();
            }
        }

        OnPostShowDevices(ShowAllDevices, PageNumber);

        return Page();
    }

    public IActionResult OnPostDeleteDevice(int id)
    {
        var device = _context.Devices.FirstOrDefault(d => d.Id == id);
        if (device != null)
        {
            _context.Devices.Remove(device);
            _context.SaveChanges();
        }
        ActiveTab = "Devices";
        OnPostShowDevices(ShowAllDevices, PageNumber);

        return Page();
    }
    // Wy wietlanie listy typ w urz dze 
    public void OnPostShowDeviceTypes(int pageNumber = 1)
    {
        LoadDeviceTypes();
        PageNumber = pageNumber;
        if (pageNumber * pageSize < DeviceTypes.Count)
        {
            DeviceTypes = DeviceTypes.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            isLastPage = false;
        }
        else
        {
            DeviceTypes = DeviceTypes.Skip((pageNumber - 1) * pageSize).ToList();
            isLastPage = true;
        }
        ActiveTab = "DeviceTypes";
    }

    // Wy wietlanie formularza dodawania typu urz dzenia
    public void OnPostShowAddDeviceType()
    {
        LoadDeviceTypes();
        ActiveTab = "AddDeviceType";
    }

    // Wy wietlanie formularza edycji typu urz dzenia
    public void OnPostShowEditDeviceType(int id)
    {
        LoadDeviceTypes();
        SelectedId = id;
        SelectedDeviceType = _context.DeviceTypes.FirstOrDefault(dt => dt.Id == id);
        ActiveTab = "EditDeviceType";
    }

    // Dodawanie nowego typu urz dzenia
    public IActionResult OnPostAddDeviceType(string name)
    {
        if (!_context.DeviceTypes.Any(dt => dt.Name == name))
        {
            var newDeviceType = new DeviceType
            {
                Name = name
            };
            _context.DeviceTypes.Add(newDeviceType);
            _context.SaveChanges();
        }
        
        ActiveTab = "DeviceTypes";
        OnPostShowDeviceTypes(PageNumber);

        return Page();
    }

    // Edytowanie istniej cego typu urz dzenia
    public IActionResult OnPostEditDeviceType(int id, string name)
    {
        if ((_context.DeviceTypes.Any(dt => dt.Name == name) &&
             _context.DeviceTypes.FirstOrDefault(dt => dt.Id == id).Name == name) ||
            !_context.DeviceTypes.Any(dt => dt.Name == name))
        {
            var deviceType = _context.DeviceTypes.FirstOrDefault(dt => dt.Id == id);
            if (deviceType != null)
            {
                deviceType.Name = name;
                _context.SaveChanges();
            }
        }

        ActiveTab = "DeviceTypes";
        OnPostShowDeviceTypes(PageNumber);

        return Page();
    }

    // Usuwanie typu urz dzenia
    public IActionResult OnPostDeleteDeviceType(int id)
    {
        var deviceType = _context.DeviceTypes.FirstOrDefault(dt => dt.Id == id);
        if (deviceType != null)
        {
            _context.DeviceTypes.Remove(deviceType);
            _context.SaveChanges();
        }
        ActiveTab = "DeviceTypes";
        OnPostShowDeviceTypes(PageNumber);

        return Page();
    }
    
    public IActionResult OnPostShowUP(int? SelectedUserId, int pageNumber = 1)
    {
        PageNumber = pageNumber;
        if (SelectedUserId == null)
        {
            LoadData();
            UserPermissions = _context.UserPermissions.Where(u => u.StatusId != 6).ToList();
            if (pageNumber * pageSize < UserPermissions.Count)
            {
                UserPermissions = UserPermissions.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                isLastPage = false;
            }
            else
            {
                UserPermissions = UserPermissions.Skip((pageNumber - 1) * pageSize).ToList();
                isLastPage = true;
            }
            ActiveTab = "UserPermissions";
            return Page();
        }
        else
        {
            LoadData();
            UserPermissions = _context.UserPermissions.Where(u =>  u.StatusId != 6 && u.UserId == SelectedUserId).ToList();
            if (pageNumber * pageSize < UserPermissions.Count)
            {
                UserPermissions = UserPermissions.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                isLastPage = false;
            }
            else
            {
                UserPermissions = UserPermissions.Skip((pageNumber - 1) * pageSize).ToList();
                isLastPage = true;
            }
            ActiveTab = "UserPermissions";
            return Page();
        }
        
    }
    public IActionResult OnPostShowAddUP(int id)
    {
        LoadData();
        UserPermissions = _context.UserPermissions.ToList();
        ActiveTab = "UserPermissionsAdd";
        return Page();
    }

    public IActionResult OnPostDeletePermissionUser(int id)
    {
        _actionLoggerService = new ActionLoggerService(_context);
        var UP =  _context.UserPermissions.FirstOrDefault(u => u.Id == id);
        if (UP != null)
        {
            UP.StatusId = 6;
            _context.UserPermissions.Update(UP);
            _context.SaveChanges();
        }
        LoadData();
        int newActionId = 1;
        if(_context.ActionHistories.Count() > 0)
        {
            newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
        }
        UserPermissions = _context.UserPermissions.Where(u => u.StatusId != 6).ToList();
        var message = _context.Messages.FirstOrDefault(m => m.UPermissionsId == id);
        var messageLink = _context.MessageLinks.FirstOrDefault(ml => ml.MessageId == message.Id);
        _actionLoggerService.Log(
            HttpContext.Session.GetInt32("UserId") ?? 0,
            message.UPermissionsId,
            message.DeviceId,
            messageLink.ApplicationId,
            3,
            newActionId
        );
        ActiveTab = "UserPermissions";  
        return Page();
    }
    public IActionResult OnPostAddUP(int PermissionId, int UserId)
    {
        _actionLoggerService = new ActionLoggerService(_context);
        if (PermissionId == -2 || UserId == -2)
        {
            LoadData();
            UserPermissions = _context.UserPermissions.Where(u => u.StatusId != 6).ToList();
            ActiveTab = "UserPermissions";  
            return Page();
        }
        var userPermission = new UserPermission
        {
            UserId = UserId,
            StatusId = 7,
            PermissionId = PermissionId,
            RequestDate = DateTime.Now,
            ResponseDate = DateTime.Now,
        };
        _context.UserPermissions.Add(userPermission);
        _context.SaveChanges();
        var message = new Message
        {
            PermissionId = PermissionId,
            UserId = UserId,
            RequestDate = DateTime.Now,
            ProgramId = _context.Permissions.FirstOrDefault(p => p.Id == PermissionId).ProgramId,
            UPermissionsId = userPermission.Id
        };
        _context.Messages.Add(message);
        _context.SaveChanges();
        int newApplicationId = (_context.MessageLinks.Max(ml => (int?)ml.ApplicationId) ?? 0) + 1;
        var messageLink = new MessageLink
        {
            ApplicationId = newApplicationId,
            DegreeId = -10,
            MessageId = message.Id,
        };
        _context.MessageLinks.Add(messageLink);
        _context.SaveChanges();
        LoadData();
        int newActionId = 1;
        if(_context.ActionHistories.Count() > 0)
        {
            newActionId = _context.ActionHistories.Max(a => a.ActionHistoryId) + 1;
        }
        UserPermissions = _context.UserPermissions.Where(u => u.StatusId != 6).ToList();
        _actionLoggerService.Log(
            HttpContext.Session.GetInt32("UserId") ?? 0,
            message.UPermissionsId,
            message.DeviceId,
            messageLink.ApplicationId,
            4,
            newActionId
        );
        OnPostShowUP(SelectedUserId, PageNumber);
        ActiveTab = "UserPermissions";  
        return Page();
    }

    public IActionResult OnPostAddPermission2group()
    {
        var pg = _context.PermissionGroups.FirstOrDefault(p => p.Id == Int32.Parse(Request.Form["group"]));
        TempData["ScrollToId"] = $"card-{pg.Id}";
        if (!pg.PermissionIds.Contains(Int32.Parse(Request.Form["select"])))
        {
            pg.PermissionIds.Add(Int32.Parse(Request.Form["select"]));
            _context.SaveChanges();
        }
        OnPostShowProducents(PageNumber);
        return Page();
    }

    public IActionResult OnPostDeletePermissionFromGroup()
    {
        var pg = _context.PermissionGroups.FirstOrDefault(p => p.Id == Int32.Parse(Request.Form["group"]));
        TempData["ScrollToId"] = $"card-{pg.Id}";
        if (pg.PermissionIds.Contains(Int32.Parse(Request.Form["perm"])))
        {
            pg.PermissionIds.Remove(Int32.Parse(Request.Form["perm"]));
            _context.SaveChanges();
        }

        OnPostShowProducents(PageNumber);
        return Page();
    }
    
    // Prywatna metoda do  adowania typ w urz dze 
    private void LoadDeviceTypes()
    {
        DeviceTypes = _context.DeviceTypes.ToList();
    }
    // public void OnPostFilterPrograms(int? producerId)
    // {
    //     LoadData();
    //     ActiveTab = "Programs";
    //
    //     if (producerId.HasValue)
    //     {
    //         Programs = Programs.Where(p => p.ProducerId == producerId).ToList();
    //         SelectedProducerId = producerId;
    //     }
    //     else
    //     {
    //         SelectedProducerId = null;
    //     }
    // }

    public string GetPermissionName(int id)
    {
        return _context.Permissions.FirstOrDefault(p => p.Id == id).Name;
    }

    public string GetPermissionProgram(int id)
    {
        return _context.Programs.FirstOrDefault(d => d.Id == _context.Permissions.First(e => e.Id == id).ProgramId)
            .Name;

    }

    public IEnumerable<SelectListItem> GetPermissionsSelectList(PermissionGroup pg)
    {
        var badlist = _context.Permissions.Where(p => pg.PermissionIds.Contains(p.Id)).ToList();
        var goodlist = _context.Permissions;
        foreach (var perm in badlist)
        {
            goodlist.Remove(perm);
        }
         IEnumerable<SelectListItem> list = goodlist.Select(d => new SelectListItem {
            Value = d.Id.ToString(),
            Text = (d.Name + " - " + _context.Programs.FirstOrDefault(e => e.Id == d.ProgramId).Name).ToString()
        });
        return list;
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
    public JsonResult OnGetUsers(int userId)
    {
        
        var degrees =  _context.Degrees.Where(d => d.DepartmentId == userId).Select(d => d.Id).ToList();
        var users = _context.Users
            .Where(u => degrees.Contains(u.DegreeId))
            .Select(p => new { id = p.Id, text = p.FirstName + " " + p.LastName + " " + p.Email })
            .ToList();

        return new JsonResult(new { success = true, users });
    }

    public JsonResult OnGetSetUser(int id)
    {
        if (id == -2)
        {
            return new JsonResult(new { success = true });
        }

        HttpContext.Session.SetInt32("ApplicationUserId", id);
        return new JsonResult(new { success = true });
    }
    public JsonResult OnGetPermissionsPA(int programId)
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
            .Where(p => p.ProgramId == programId && (!existingPermissions.Contains(p.Id)))
            .Select(p => new { id = p.Id, name = p.Name })
            .ToList();

        return new JsonResult(new { success = true, permissions });
    }
}

