using Microsoft.AspNetCore.Mvc;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class PermissionsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public PermissionsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var permissions = _context.Permissions.ToList();
            var programs = _context.Programs.ToList();

            var model = new PermissionsViewModel
            {
                Permissions = permissions,
                Programs = programs
            };

            return View(model);
        }
    }
    public class PermissionsViewModel
    {
        public List<Permission> Permissions { get; set; } = new();
        public List<Programs> Programs { get; set; } = new();
    }
}