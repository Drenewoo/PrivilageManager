using Microsoft.AspNetCore.Mvc;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class EditUserViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public EditUserViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var model = new EditUserViewModel
            {
                User = user,
                Degrees = _context.Degrees.ToList(),
                Departments = _context.Departments.ToList()
            };

            return View("~/Views/Shared/Components/AdminPanel/Users/Edit.cshtml", model);
        }
    }

    public class EditUserViewModel
    {
        public User User { get; set; }
        public List<Degree> Degrees { get; set; }
        public List<Department> Departments { get; set; }
    }
}