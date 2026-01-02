using _204Store.Data;
using _204Store.Models;
using Microsoft.AspNetCore.Identity;

namespace _204Store.Middleware
{
    public class LoginHistoryMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginHistoryMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.User.Identity.IsAuthenticated && context.Session.GetString("IsLoginLogged") == null)
            {
                var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.GetUserAsync(context.User);
                if (user != null)
                {
                    var log = new LoginHistory
                    {
                        ApplicationUserId = user.Id,
                        LoginTime = DateTime.Now,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        DeviceInfo = context.Request.Headers["User-Agent"].ToString()
                    };

                    dbContext.LoginHistories.Add(log);
                    await dbContext.SaveChangesAsync();

                    context.Session.SetString("IsLoginLogged", "true");
                }
            }

            await _next(context);
        }
    }
}