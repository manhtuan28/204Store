using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using OfficeOpenXml;

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

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Vui lòng chọn file Excel.";
                return View();
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Chỉ hỗ trợ file .xlsx";
                return View();
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var productName = worksheet.Cells[row, 1].Value?.ToString().Trim();
                            var categoryName = worksheet.Cells[row, 2].Value?.ToString().Trim();
                            var color = worksheet.Cells[row, 3].Value?.ToString().Trim();
                            var rom = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            var ram = worksheet.Cells[row, 5].Value?.ToString().Trim();

                            double.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out double price);
                            int.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out int quantity);
                            var imageUrl = worksheet.Cells[row, 8].Value?.ToString().Trim();

                            if (string.IsNullOrEmpty(productName)) continue;

                            int categoryId;
                            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                            if (category != null)
                            {
                                categoryId = category.Id;
                            }
                            else
                            {
                                var firstCategory = await _context.Categories.FirstOrDefaultAsync();
                                categoryId = firstCategory?.Id ?? 1;
                            }

                            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == productName);
                            if (product == null)
                            {
                                product = new Product
                                {
                                    Name = productName,
                                    Description = $"Nhập từ Excel {DateTime.Now:dd/MM}",
                                    ImageUrl = imageUrl ?? "https://placehold.co/300",
                                    CategoryId = categoryId,
                                    IsActive = true
                                };
                                _context.Products.Add(product);
                                await _context.SaveChangesAsync();
                            }

                            var existingVariant = await _context.ProductDetails
                                .FirstOrDefaultAsync(d => d.ProductId == product.Id && d.Color == color && d.Rom == rom && d.Ram == ram);

                            if (existingVariant != null)
                            {
                                existingVariant.Quantity += quantity;
                                existingVariant.Price = (decimal)price;
                                _context.ProductDetails.Update(existingVariant);
                            }
                            else
                            {
                                var newVariant = new ProductDetail
                                {
                                    ProductId = product.Id,
                                    Color = color ?? "Mặc định",
                                    Rom = rom ?? "128GB",
                                    Ram = ram ?? "8GB",
                                    Price = (decimal)price,
                                    Quantity = quantity,
                                    ImageUrl = imageUrl ?? product.ImageUrl,
                                    IsActive = true
                                };
                                _context.ProductDetails.Add(newVariant);
                            }

                            await _context.SaveChangesAsync();
                        }
                    }
                }

                TempData["success"] = "Nhập hàng thành công!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | Chi tiết: " + ex.InnerException.Message;
                TempData["error"] = "Lỗi: " + msg;
                return View();
            }
        }
        public IActionResult DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("NhapHang");

                worksheet.Cells[1, 1].Value = "Tên Sản Phẩm";
                worksheet.Cells[1, 2].Value = "Danh Mục";
                worksheet.Cells[1, 3].Value = "Màu Sắc";
                worksheet.Cells[1, 4].Value = "Bộ Nhớ (ROM)";
                worksheet.Cells[1, 5].Value = "RAM";
                worksheet.Cells[1, 6].Value = "Giá Bán";
                worksheet.Cells[1, 7].Value = "Số Lượng";
                worksheet.Cells[1, 8].Value = "Link Ảnh";

                using (var range = worksheet.Cells[1, 1, 1, 8])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Mau_Nhap_Hang_Co_RAM_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}