using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;

namespace webapp.Pages
{
    public class AdminRestoreModel : PageModel
    {
        private readonly AppDbContext _context;

        private string[] _statuses = { "Oczekujący", "Zatwierdzony", "Zwrócony", "Odebrane", "Nieznany", "Odrzucone", "Wykonane" };
        private string[] _actions = { "Zatwierdzenie", "Edycja", "Odrzucenie", "Dodanie", "Przegląd", "Wykonanie" };
        
        public AdminRestoreModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            if (!_context.Users.Any())
            {
                OnPostAddDepartment("Administracja");
                OnPostAddDegree("Administratorzy", _context.Departments.Min(d => d.Id), -1);
                OnPostAddUser("admin", "password", _context.Degrees.Min(d => d.Id), 1);
                
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT Statuses ON");
                        
                        int i = 1;
                        foreach (var name in _statuses)
                        {
                            AddStatus(i, name);
                            i++;
                        }
                        
                        _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT Statuses OFF");
                        _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT Actions ON");

                        i = 1;
                        foreach (var name in _actions)
                        {
                            AddAction(i, name);
                            i++;
                        }
                        _context.Database.ExecuteSqlRaw($"SET IDENTITY_INSERT Actions OFF");
                        

                        // Zatwierdzenie transakcji
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                _context.SaveChanges();

                OnPostAddDegree("IOD", _context.Departments.Min(d => d.Id), -1);
                OnPostAddUser("iod", "password", _context.Degrees.Min(d => d.Id) + 1, 2);
            }
        }
        public void OnPostAddUser(string username, string password, int degreeId, int admin = 0)
        {
            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = username,
                PasswordHash = string.Empty,
                FirstName = string.Empty, LastName = string.Empty,
                Email = string.Empty,
                DegreeId = degreeId,
                IsAdmin = admin,
            };
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, password);

            _context.Users.Add(newUser);
            _context.SaveChanges();
            ModelState.AddModelError(string.Empty, "Użytkownik został dodany pomyślnie.");
            
        }

        public void AddStatus(int i, string name)
        {
            var status = new Status
            {
                Id = i,
                Name = name
            };
            _context.Statuses.Add(status);
            _context.SaveChanges();
        }
        public void AddAction(int i, string name)
        {
            var action = new Actions
            {
                Id = i,
                Name = name
            };
            _context.Actions.Add(action);
            _context.SaveChanges();
        }
        public void OnPostAddDepartment(string name)
        {
            var newDepartment = new Department
            {
                Name = name
            };
            _context.Departments.Add(newDepartment);
            _context.SaveChanges();
        }
        public void OnPostAddDegree(string name, int departmentId, int managerId)
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
    }
}
