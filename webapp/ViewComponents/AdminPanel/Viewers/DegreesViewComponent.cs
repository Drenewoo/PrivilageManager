using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class DegreesViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public DegreesViewComponent(AppDbContext context)
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
                degrees = degrees.Where(d => d.DepartmentId == selectedDepartmentId).ToList();
            }

            var model = new DegreesViewModel
            {
                Users = users,
                Degrees = degrees,
                Departments = departments,
                SelectedDepartmentId = selectedDepartmentId
            };

            return View("~/Views/Shared/Components/AdminPanel/Degrees/Default.cshtml", model);
        }
    }

    public class DegreesViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Degree> Degrees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public int? SelectedDepartmentId { get; set; }
    }
}