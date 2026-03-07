using CloudinaryDotNet;
using Google.GenAI;
using HyperCar.BLL.Interfaces;
using HyperCar.BLL.Services;
using HyperCar.DAL.Data;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HyperCar.BLL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHyperCarServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ===== Database Context (DAL) =====
            services.AddDbContext<HyperCarDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // ===== ASP.NET Core Identity (DAL entity) =====
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<HyperCarDbContext>()
            .AddDefaultTokenProviders();

            // ===== Cookie Configuration =====
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
            });

            // ===== Repository + Unit of Work (DAL) =====
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ===== Business Services (BLL) =====
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ICarService, CarService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IVNPayService, VNPayService>();
            services.AddScoped<IShippingService, ShippingService>();
            services.AddScoped<IAIChatService, AIChatService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IReviewService, ReviewService>();

            // ===== Cloudinary — Singleton (1 instance per app) =====
            services.AddSingleton<Cloudinary>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var account = new Account(
                    config["Cloudinary:CloudName"],
                    config["Cloudinary:ApiKey"],
                    config["Cloudinary:ApiSecret"]);
                return new Cloudinary(account) { Api = { Secure = true } };
            });
            services.AddScoped<ICloudinaryService, CloudinaryService>();

            // ===== Google Gemini SDK — Singleton (thread-safe, 1 client per app) =====
            services.AddSingleton<Client>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is required in appsettings.json");
                return new Client(apiKey: apiKey);
            });

            // ===== IMemoryCache — cho car list cache (10 phút TTL) =====
            services.AddMemoryCache();

            // ===== HttpClient for external APIs =====
            services.AddHttpClient("ViettelPost", client =>
            {
                client.BaseAddress = new Uri("https://partner.viettelpost.vn/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }
    }
}
