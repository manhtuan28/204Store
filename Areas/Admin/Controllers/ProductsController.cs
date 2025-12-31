using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductDetails)
                .Include(p => p.ProductImages)
                .ToListAsync();
            return View(products);
        }

        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? file, List<IFormFile>? galleryFiles)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\products");

                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = @"\images\products\" + fileName;
                }
                else
                {
                    product.ImageUrl = "";
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                if (galleryFiles != null && galleryFiles.Count > 0)
                {
                    string galleryPath = Path.Combine(wwwRootPath, @"images\products\gallery");
                    if (!Directory.Exists(galleryPath)) Directory.CreateDirectory(galleryPath);

                    foreach (var galleryFile in galleryFiles)
                    {
                        string galleryFileName = Guid.NewGuid().ToString() + Path.GetExtension(galleryFile.FileName);
                        using (var stream = new FileStream(Path.Combine(galleryPath, galleryFileName), FileMode.Create))
                        {
                            await galleryFile.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            Url = @"\images\products\gallery\" + galleryFileName
                        };
                        _context.ProductImages.Add(productImage);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["success"] = "Thêm sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? file, List<IFormFile>? galleryFiles)
        {
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;

                    if (file != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = Path.Combine(wwwRootPath, @"images\products");

                        if (!string.IsNullOrEmpty(product.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
                            if (System.IO.File.Exists(oldImagePath)) System.IO.File.Delete(oldImagePath);
                        }

                        using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        product.ImageUrl = @"\images\products\" + fileName;
                    }

                    if (galleryFiles != null && galleryFiles.Count > 0)
                    {
                        string galleryPath = Path.Combine(wwwRootPath, @"images\products\gallery");
                        if (!Directory.Exists(galleryPath)) Directory.CreateDirectory(galleryPath);

                        foreach (var galleryFile in galleryFiles)
                        {
                            string galleryFileName = Guid.NewGuid().ToString() + Path.GetExtension(galleryFile.FileName);
                            using (var stream = new FileStream(Path.Combine(galleryPath, galleryFileName), FileMode.Create))
                            {
                                await galleryFile.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ProductId = product.Id,
                                Url = @"\images\products\gallery\" + galleryFileName
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật sản phẩm thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == product.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return Json(new { success = false, message = "Không tìm thấy ảnh" });

            // Xóa file vật lý
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            var oldImagePath = Path.Combine(wwwRootPath, image.Url.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa ảnh" });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var mainPath = Path.Combine(wwwRootPath, product.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(mainPath)) System.IO.File.Delete(mainPath);
                }

                if (product.ProductImages != null)
                {
                    foreach (var img in product.ProductImages)
                    {
                        var galleryPath = Path.Combine(wwwRootPath, img.Url.TrimStart('\\'));
                        if (System.IO.File.Exists(galleryPath)) System.IO.File.Delete(galleryPath);
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa sản phẩm thành công";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}