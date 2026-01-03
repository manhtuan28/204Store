using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PromotionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Promotions.OrderByDescending(p => p.Id).ToListAsync());
        }

        public IActionResult Create()
        {
            return View(new Promotion { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion promotion)
        {
            if (ModelState.IsValid)
            {
                if (promotion.EndDate < promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu!");
                    return View(promotion);
                }

                if (await _context.Promotions.AnyAsync(p => p.Code == promotion.Code))
                {
                    ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại!");
                    return View(promotion);
                }

                _context.Add(promotion);
                await _context.SaveChangesAsync();
                TempData["success"] = "Tạo mã giảm giá thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Promotion promotion)
        {
            if (id != promotion.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (promotion.EndDate < promotion.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu!");
                    return View(promotion);
                }

                if (await _context.Promotions.AnyAsync(p => p.Code == promotion.Code && p.Id != id))
                {
                    ModelState.AddModelError("Code", "Mã giảm giá này đã được sử dụng bởi chương trình khác!");
                    return View(promotion);
                }

                try
                {
                    _context.Update(promotion);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật mã giảm giá thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PromotionExists(promotion.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(promotion);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã xóa mã giảm giá.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PromotionExists(int id)
        {
            return _context.Promotions.Any(e => e.Id == id);
        }
    }
}