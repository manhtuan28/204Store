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

        // Lấy danh mục theo cấu trúc cha-con
        private async Task<List<Category>> GetHierarchicalCategories()
        {
            var allCategories = await _context.Categories
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
            
            var result = new List<Category>();
            var parentCategories = allCategories.Where(c => c.ParentId == null || c.ParentId == 0).ToList();
            
            foreach (var parent in parentCategories)
            {
                result.Add(parent);
                var children = allCategories.Where(c => c.ParentId == parent.Id).OrderBy(c => c.DisplayOrder);
                result.AddRange(children);
            }
            
            return result;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.ProductDetails)
                .Include(p => p.ProductImages)
                .ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["Categories"] = await GetHierarchicalCategories();
            ViewData["AllCategories"] = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? file, List<IFormFile>? galleryFiles, List<int>? selectedCategories)
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

                // Đặt CategoryId mặc định từ danh mục đầu tiên được chọn (tương thích ngược)
                if (selectedCategories != null && selectedCategories.Any())
                {
                    product.CategoryId = selectedCategories.First();
                }

                _context.Add(product);
                await _context.SaveChangesAsync();

                // Lưu quan hệ nhiều-nhiều
                if (selectedCategories != null && selectedCategories.Any())
                {
                    foreach (var categoryId in selectedCategories)
                    {
                        _context.ProductCategories.Add(new ProductCategory
                        {
                            ProductId = product.Id,
                            CategoryId = categoryId
                        });
                    }
                    await _context.SaveChangesAsync();
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
                    await _context.SaveChangesAsync();
                }

                TempData["success"] = "Thêm sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            ViewData["Categories"] = await GetHierarchicalCategories();
            ViewData["AllCategories"] = await _context.Categories.ToListAsync();
            return View(product);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductCategories)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewData["Categories"] = await GetHierarchicalCategories();
            ViewData["AllCategories"] = await _context.Categories.ToListAsync();
            ViewData["SelectedCategories"] = product.ProductCategories.Select(pc => pc.CategoryId).ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? file, List<IFormFile>? galleryFiles, List<int>? selectedCategories)
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

                    var existingCategories = await _context.ProductCategories
                        .Where(pc => pc.ProductId == product.Id)
                        .ToListAsync();
                    _context.ProductCategories.RemoveRange(existingCategories);

                    if (selectedCategories != null && selectedCategories.Any())
                    {
                        product.CategoryId = selectedCategories.First();

                        foreach (var categoryId in selectedCategories)
                        {
                            _context.ProductCategories.Add(new ProductCategory
                            {
                                ProductId = product.Id,
                                CategoryId = categoryId
                            });
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
            ViewData["Categories"] = await GetHierarchicalCategories();
            ViewData["AllCategories"] = await _context.Categories.ToListAsync();
            ViewData["SelectedCategories"] = selectedCategories ?? new List<int>();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return Json(new { success = false, message = "Không tìm thấy ảnh" });

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