using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductDetailsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductDetailsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int productId)
        {
            if (productId == 0) return NotFound();
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();
            var details = await _context.ProductDetails.Where(x => x.ProductId == productId).OrderBy(x => x.Id).ToListAsync();
            ViewBag.Product = product;
            return View(details);
        }

        public IActionResult Create(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();
            ViewBag.Product = product;
            return View(new ProductDetail { ProductId = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDetail model, IFormFile? imageFile)
        {
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "variants");

                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    model.ImageUrl = "/images/variants/" + fileName;
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["success"] = "Thêm biến thể thành công";
                return RedirectToAction(nameof(Index), new { productId = model.ProductId });
            }

            ViewBag.Product = _context.Products.Find(model.ProductId);
            return View(model);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.ProductDetails.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDetail model, IFormFile? imageFile)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "variants");

                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                        string filePath = Path.Combine(uploadFolder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        model.ImageUrl = "/images/variants/" + fileName;
                    }
                    else
                    {
                        var existingDetail = await _context.ProductDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                        if (existingDetail != null)
                        {
                            model.ImageUrl = existingDetail.ImageUrl;
                        }
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ProductDetails.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { productId = model.ProductId });
            }

            if (model.Product == null)
            {
                model.Product = await _context.Products.FindAsync(model.ProductId);
            }
            return View(model);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detail = await _context.ProductDetails
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (detail == null) return NotFound();

            return View(detail);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail = await _context.ProductDetails.FindAsync(id);
            if (detail != null)
            {
                _context.ProductDetails.Remove(detail);
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã xóa biến thể thành công";
                return RedirectToAction(nameof(Index), new { productId = detail.ProductId });
            }
            return NotFound();
        }
    }
}