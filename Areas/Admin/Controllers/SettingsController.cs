using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _204Store.Data;
using _204Store.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var flashSaleSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "FlashSaleEndTime");
            ViewBag.FlashSaleEndTime = flashSaleSetting?.Value;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFlashSale(string FlashSaleEndTime)
        {
            if (string.IsNullOrEmpty(FlashSaleEndTime))
            {
                TempData["Error"] = "Vui lòng chọn thời gian kết thúc Flash Sale hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "FlashSaleEndTime");
            if (setting == null)
            {
                setting = new SystemSetting { Key = "FlashSaleEndTime", Value = FlashSaleEndTime };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = FlashSaleEndTime;
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã lưu thời gian kết thúc Flash Sale thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
