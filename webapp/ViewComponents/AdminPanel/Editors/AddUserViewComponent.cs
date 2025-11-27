using Microsoft.AspNetCore.Mvc;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class AddUserViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public AddUserViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var model = new AddUserViewModel
            {
                Degrees = _context.Degrees.ToList(),
                Departments = _context.Departments.ToList()
            };

            return View("~/Views/Shared/Components/AdminPanel/Users/Add.cshtml", model);
        }
    }
    public class AddUserViewModel
    {
        public List<Degree> Degrees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
    }
}