using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class DepartmentsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public DepartmentsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int? selectedDepartmentId)
        {
            var departments = _context.Departments.ToList();
            var model = new DepartmentsViewModel
            {
                Departments = departments
            };

            return View("~/Views/Shared/Components/AdminPanel/Departments/Default.cshtml",model);
        }
    }

    public class DepartmentsViewModel
    {
        public List<Department> Departments { get; set; } = new();
    }
}