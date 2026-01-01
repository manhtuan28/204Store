using _204Store.Data;
using _204Store.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CategoriesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(categories);
        }
        public IActionResult Create()
        {
            ViewData["ParentId"] = new SelectList(_context.Categories.Where(c => c.ParentId == null), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                if (file != null)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\categories");

                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    category.ImageUrl = @"\images\categories\" + fileName;
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["success"] = "Tạo danh mục thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.Categories.Where(c => c.ParentId == null), "Id", "Name", category.ParentId);
            return View(category);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            var categories = _context.Categories.Where(c => c.Id != id && c.ParentId == null);
            ViewData["ParentId"] = new SelectList(categories, "Id", "Name", category.ParentId);

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category, IFormFile? file)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null)
                    {
                        string wwwRootPath = _webHostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = Path.Combine(wwwRootPath, @"images\categories");

                        if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                        if (!string.IsNullOrEmpty(category.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, category.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        category.ImageUrl = @"\images\categories\" + fileName;
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật danh mục thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Categories.Any(e => e.Id == category.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ParentId"] = new SelectList(_context.Categories.Where(c => c.ParentId == null && c.Id != id), "Id", "Name", category.ParentId);
            return View(category);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var category = await _context.Categories.FirstOrDefaultAsync(m => m.Id == id);
            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa danh mục thành công";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}