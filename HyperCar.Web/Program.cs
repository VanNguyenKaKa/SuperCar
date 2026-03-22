using HyperCar.BLL.DTOs;
using HyperCar.BLL.Extensions;
using HyperCar.BLL.Interfaces;
using HyperCar.Web.Hubs;
using System.Security.Claims;

namespace HyperCar.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ===== Register ALL DAL + BLL services via BLL extension (Web never touches DAL) =====
            builder.Services.AddHyperCarServices(builder.Configuration);

            // ===== Session =====
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // ===== SignalR =====
            builder.Services.AddSignalR();

            // ===== Razor Pages + AntiForgery =====
            builder.Services.AddRazorPages();
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
            });

            var app = builder.Build();

            // ===== Seed Data (Admin account, roles, sample data) =====
            await DataSeeder.SeedDataAsync(app.Services);

            // ===== Middleware Pipeline =====
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapHub<NotificationHub>("/hubs/notification");
            app.MapHub<ReviewHub>("/hubs/review");

            // ===== AI Chatbot API =====
            app.MapPost("/api/chat", async (ChatRequestDto request, IAIChatService chatService, HttpContext ctx) =>
            {
                // Attach user ID if authenticated
                if (ctx.User.Identity?.IsAuthenticated == true)
                    request.UserId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

                var result = await chatService.SendMessageAsync(request);
                return Results.Ok(result);
            });

            app.Run();
        }
    }
}
