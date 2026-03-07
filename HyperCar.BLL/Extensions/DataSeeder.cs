using HyperCar.DAL.Data;
using HyperCar.DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HyperCar.BLL.Extensions
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = scope.ServiceProvider.GetRequiredService<HyperCarDbContext>();

            // Ensure database is created (migrated)
            await context.Database.MigrateAsync();

            // ===== Seed Roles =====
            string[] roles = { "Admin", "Customer", "Staff" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ===== Seed Admin Account =====
            // Email: Admin@123, Password: 12345678
            const string adminEmail = "Admin@123";
            const string adminPassword = "Aa@12345678";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    PhoneNumber = "0901234567",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                // Ensure admin has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // ===== Seed Sample Brands (if empty) =====
            if (!await context.Brands.AnyAsync())
            {
                context.Brands.AddRange(
                    new Brand { Name = "Bugatti", Country = "France", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/bugatti", Description = "French luxury hypercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "Koenigsegg", Country = "Sweden", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/koenigsegg", Description = "Swedish hypercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "Pagani", Country = "Italy", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/pagani", Description = "Italian luxury hypercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "McLaren", Country = "United Kingdom", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/mclaren", Description = "British supercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "Ferrari", Country = "Italy", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/ferrari", Description = "Italian luxury sports car manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "Lamborghini", Country = "Italy", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/lamborghini", Description = "Italian luxury supercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow },
                    new Brand { Name = "Rimac", Country = "Croatia", Logo = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/brands/rimac", Description = "Croatian electric hypercar manufacturer", IsActive = true, CreatedDate = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }

            // ===== Seed Sample Cars (if empty) =====
            if (!await context.Cars.AnyAsync())
            {
                var brands = await context.Brands.ToListAsync();
                var bugatti = brands.FirstOrDefault(b => b.Name == "Bugatti");
                var koenigsegg = brands.FirstOrDefault(b => b.Name == "Koenigsegg");
                var pagani = brands.FirstOrDefault(b => b.Name == "Pagani");
                var mclaren = brands.FirstOrDefault(b => b.Name == "McLaren");
                var ferrari = brands.FirstOrDefault(b => b.Name == "Ferrari");
                var lamborghini = brands.FirstOrDefault(b => b.Name == "Lamborghini");

                if (bugatti != null && koenigsegg != null && pagani != null && mclaren != null && ferrari != null && lamborghini != null)
                {
                    context.Cars.AddRange(
                        new Car { Name = "Chiron Super Sport", BrandId = bugatti.Id, Price = 3900000, HorsePower = 1578, Engine = "8.0L Quad-Turbo W16", TopSpeed = 440, Acceleration = 2.4m, Stock = 3, Description = "The ultimate grand touring hypercar with unmatched luxury and performance.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/chiron-ss", Category = "Hypercar", IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Car { Name = "Jesko Absolut", BrandId = koenigsegg.Id, Price = 3400000, HorsePower = 1600, Engine = "5.0L Twin-Turbo V8", TopSpeed = 531, Acceleration = 2.5m, Stock = 2, Description = "The fastest Koenigsegg ever built, designed to break speed records.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/jesko", Category = "Hypercar", IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Car { Name = "Huayra Roadster BC", BrandId = pagani.Id, Price = 3500000, HorsePower = 791, Engine = "6.0L Twin-Turbo V12", TopSpeed = 370, Acceleration = 2.9m, Stock = 2, Description = "A work of art that combines Italian craftsmanship with extreme performance.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/huayra", Category = "Hypercar", IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Car { Name = "Speedtail", BrandId = mclaren.Id, Price = 2250000, HorsePower = 1036, Engine = "4.0L Twin-Turbo V8 Hybrid", TopSpeed = 403, Acceleration = 2.5m, Stock = 4, Description = "McLaren's hyper-GT with a central driving position.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/speedtail", Category = "Hypercar", IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Car { Name = "SF90 Stradale", BrandId = ferrari.Id, Price = 625000, HorsePower = 986, Engine = "4.0L Twin-Turbo V8 Hybrid", TopSpeed = 340, Acceleration = 2.5m, Stock = 5, Description = "Ferrari's first plug-in hybrid supercar with electrifying performance.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/sf90", Category = "Supercar", IsActive = true, CreatedDate = DateTime.UtcNow },
                        new Car { Name = "Aventador SVJ", BrandId = lamborghini.Id, Price = 573966, HorsePower = 770, Engine = "6.5L V12", TopSpeed = 350, Acceleration = 2.8m, Stock = 3, Description = "The pinnacle of Lamborghini's V12 lineage with active aerodynamics.", ImageUrl = "https://res.cloudinary.com/dkcr1fkxj/image/upload/v1/hypercar/cars/aventador-svj", Category = "Supercar", IsActive = true, CreatedDate = DateTime.UtcNow }
                    );
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
