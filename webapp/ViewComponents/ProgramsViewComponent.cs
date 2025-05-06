using Microsoft.AspNetCore.Mvc;
using webapp.Data;
using webapp.Models;

namespace webapp.ViewComponents
{
    public class ProgramsViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public ProgramsViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public IViewComponentResult Invoke()
        {
            var programs = _context.Programs.ToList();

            var model = new ProgramsViewModel
            {
                Programs = programs
            };

            return View(model);
        }
    }
    public class ProgramsViewModel
    {
        public List<Programs> Programs { get; set; } = new();
    }
}