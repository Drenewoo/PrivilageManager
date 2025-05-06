using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class UsersViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public UsersViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int? selectedDepartmentId)
        {
            var departments = _context.Departments.ToList();
            var degrees = _context.Degrees.ToList();
            var users = _context.Users.ToList();

            if (selectedDepartmentId.HasValue)
            {
                users = users.Where(u => degrees.Any(d => d.Id == u.DegreeId && d.DepartmentId == selectedDepartmentId)).ToList();
            }

            var model = new UsersViewModel
            {
                Users = users,
                Departments = departments,
                Degrees = degrees,
                SelectedDepartmentId = selectedDepartmentId
            };

            return View(model);
        }
    }

    public class UsersViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Degree> Degrees { get; set; } = new();
        public int? SelectedDepartmentId { get; set; }
    }
}