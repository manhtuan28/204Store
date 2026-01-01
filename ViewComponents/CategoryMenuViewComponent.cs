using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using System.Threading.Tasks;
using System.Linq;

namespace _204Store.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.ChildCategories)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(categories);
        }
    }
}