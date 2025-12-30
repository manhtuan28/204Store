// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc; // Thêm dòng này để dùng IActionResult
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace _204Store.Areas.Identity.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>

        // SỬA: Đổi từ void sang IActionResult
        public IActionResult OnGet()
        {
            if (!User.IsInRole("Admin"))
            {

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return Page();
        }
    }
}