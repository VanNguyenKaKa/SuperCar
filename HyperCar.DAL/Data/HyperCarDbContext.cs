using HyperCar.DAL.Entities;
using HyperCar.DAL.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.DAL.Data
{
    public class HyperCarDbContext : IdentityDbContext<ApplicationUser>
    {
        public HyperCarDbContext(DbContextOptions<HyperCarDbContext> options)
            : base(options)
        {
        }

        // Entity DbSets
        public DbSet<Brand> Brands { get; set; } = null!;
        public DbSet<Car> Cars { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Shipping> Shippings { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<ConversationHistory> ConversationHistories { get; set; } = null!;
        public DbSet<TransactionHistory> TransactionHistories { get; set; } = null!;
        public DbSet<ReportSnapshot> ReportSnapshots { get; set; } = null!;
        public DbSet<TestDriveBooking> TestDriveBookings { get; set; } = null!;
        public DbSet<Showroom> Showrooms { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ===== Brand Configuration =====
            builder.Entity<Brand>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            });

            // ===== Car Configuration =====
            builder.Entity<Car>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TopSpeed).HasColumnType("decimal(8,2)");
                entity.Property(e => e.Acceleration).HasColumnType("decimal(5,2)");

                // Brand → Cars (1:N)
                entity.HasOne(e => e.Brand)
                    .WithMany(b => b.Cars)
                    .HasForeignKey(e => e.BrandId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Order Configuration =====
            builder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedDate);

                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ShippingFee).HasColumnType("decimal(18,2)");

                // User → Orders (1:N)
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== OrderItem Configuration =====
            builder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                // Order → OrderItems (1:N)
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Car → OrderItems (1:N)
                entity.HasOne(e => e.Car)
                    .WithMany(c => c.OrderItems)
                    .HasForeignKey(e => e.CarId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Payment Configuration =====
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId).IsUnique(); // 1:1 with Order
                entity.HasIndex(e => e.TransactionRef);

                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

                // Order → Payment (1:1)
                entity.HasOne(e => e.Order)
                    .WithOne(o => o.Payment)
                    .HasForeignKey<Payment>(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Shipping Configuration =====
            builder.Entity<Shipping>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId).IsUnique(); // 1:1 with Order
                entity.HasIndex(e => e.TrackingCode);

                entity.Property(e => e.Fee).HasColumnType("decimal(18,2)");

                // Order → Shipping (1:1)
                entity.HasOne(e => e.Order)
                    .WithOne(o => o.Shipping)
                    .HasForeignKey<Shipping>(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Review Configuration =====
            builder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.CarId }); // Composite for lookups

                // Hard DB constraint: one review per OrderItem (filtered unique index for nullable FK)
                entity.HasIndex(e => e.OrderItemId)
                    .IsUnique()
                    .HasFilter("[OrderItemId] IS NOT NULL");

                // User → Reviews (1:N)
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Car → Reviews (1:N)
                entity.HasOne(e => e.Car)
                    .WithMany(c => c.Reviews)
                    .HasForeignKey(e => e.CarId)
                    .OnDelete(DeleteBehavior.Cascade);

                // OrderItem → Review (1:0..1)
                entity.HasOne(e => e.OrderItem)
                    .WithOne(oi => oi.Review)
                    .HasForeignKey<Review>(e => e.OrderItemId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== ConversationHistory Configuration =====
            builder.Entity<ConversationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedDate);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ConversationHistories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== TransactionHistory Configuration =====
            builder.Entity<TransactionHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.CreatedDate);

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.TransactionHistories)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ReportSnapshot Configuration =====
            builder.Entity<ReportSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ReportDate, e.Type });
            });

            // ===== TestDriveBooking Configuration =====
            builder.Entity<TestDriveBooking>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CarId, e.ScheduledDate })
                    .IsUnique()
                    .HasFilter($"[Status] != {(int)BookingStatus.Cancelled}");
                entity.HasIndex(e => e.ApplicationUserId);
                entity.HasIndex(e => e.Status);

                // User → TestDriveBookings (1:N)
                entity.HasOne(e => e.User)
                    .WithMany(u => u.TestDriveBookings)
                    .HasForeignKey(e => e.ApplicationUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Car → TestDriveBookings (1:N)
                entity.HasOne(e => e.Car)
                    .WithMany(c => c.TestDriveBookings)
                    .HasForeignKey(e => e.CarId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Showroom → TestDriveBookings (1:N, optional)
                entity.HasOne(e => e.Showroom)
                    .WithMany(s => s.TestDriveBookings)
                    .HasForeignKey(e => e.ShowroomId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== Showroom Configuration =====
            builder.Entity<Showroom>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name);
            });

            // ===== Seed Admin Role =====
            SeedData(builder);
        }

        /// <summary>
        /// Seeds initial roles and admin user
        /// </summary>
        private void SeedData(ModelBuilder builder)
        {
            // Seed roles
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "admin-role-id",
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = "customer-role-id",
                    Name = "Customer",
                    NormalizedName = "CUSTOMER"
                }
            );

            // Seed sample brands
            builder.Entity<Brand>().HasData(
                new Brand { Id = 1, Name = "Bugatti", Country = "France", Description = "French luxury hypercar manufacturer" },
                new Brand { Id = 2, Name = "Koenigsegg", Country = "Sweden", Description = "Swedish hypercar manufacturer" },
                new Brand { Id = 3, Name = "Pagani", Country = "Italy", Description = "Italian hypercar manufacturer" },
                new Brand { Id = 4, Name = "McLaren", Country = "United Kingdom", Description = "British supercar manufacturer" },
                new Brand { Id = 5, Name = "Ferrari", Country = "Italy", Description = "Italian luxury sports car manufacturer" },
                new Brand { Id = 6, Name = "Lamborghini", Country = "Italy", Description = "Italian luxury sports car manufacturer" },
                new Brand { Id = 7, Name = "Rimac", Country = "Croatia", Description = "Croatian electric hypercar manufacturer" },
                new Brand { Id = 8, Name = "Aston Martin", Country = "United Kingdom", Description = "British luxury car manufacturer" }
            );

            // Seed sample cars
            builder.Entity<Car>().HasData(
                new Car { Id = 1, Name = "Chiron Super Sport", BrandId = 1, Price = 3900000m, HorsePower = 1578, Engine = "8.0L Quad-Turbo W16", TopSpeed = 440m, Acceleration = 2.4m, Stock = 3, Category = "HyperCar", Description = "The Bugatti Chiron Super Sport is the ultimate expression of speed and luxury.", ImageUrl = "/images/cars/chiron.jpg" },
                new Car { Id = 2, Name = "Jesko Absolut", BrandId = 2, Price = 3400000m, HorsePower = 1600, Engine = "5.0L Twin-Turbo V8", TopSpeed = 531m, Acceleration = 2.5m, Stock = 2, Category = "HyperCar", Description = "The fastest Koenigsegg ever made, designed for maximum speed.", ImageUrl = "/images/cars/jesko.jpg" },
                new Car { Id = 3, Name = "Huayra R", BrandId = 3, Price = 3100000m, HorsePower = 850, Engine = "6.0L NA V12", TopSpeed = 370m, Acceleration = 2.7m, Stock = 5, Category = "HyperCar", Description = "Track-focused masterpiece from Pagani with a naturally aspirated V12.", ImageUrl = "/images/cars/huayra.jpg" },
                new Car { Id = 4, Name = "Speedtail", BrandId = 4, Price = 2250000m, HorsePower = 1055, Engine = "4.0L Twin-Turbo V8 Hybrid", TopSpeed = 403m, Acceleration = 2.5m, Stock = 4, Category = "HyperGT", Description = "McLaren's fastest car ever — a hyper-GT with a central driving position.", ImageUrl = "/images/cars/speedtail.jpg" },
                new Car { Id = 5, Name = "SF90 Stradale", BrandId = 5, Price = 625000m, HorsePower = 986, Engine = "4.0L Twin-Turbo V8 Hybrid", TopSpeed = 340m, Acceleration = 2.5m, Stock = 8, Category = "SuperCar", Description = "Ferrari's first plug-in hybrid, blending extreme performance with innovation.", ImageUrl = "/images/cars/sf90.jpg" },
                new Car { Id = 6, Name = "Revuelto", BrandId = 6, Price = 608358m, HorsePower = 1015, Engine = "6.5L NA V12 Hybrid", TopSpeed = 350m, Acceleration = 2.5m, Stock = 6, Category = "SuperCar", Description = "Lamborghini's V12 hybrid flagship with 1015 combined horsepower.", ImageUrl = "/images/cars/revuelto.jpg" },
                new Car { Id = 7, Name = "Nevera", BrandId = 7, Price = 2400000m, HorsePower = 1914, Engine = "Quad Electric Motors", TopSpeed = 412m, Acceleration = 1.85m, Stock = 3, Category = "Electric HyperCar", Description = "The world's fastest production electric hypercar.", ImageUrl = "/images/cars/nevera.jpg" },
                new Car { Id = 8, Name = "Valkyrie", BrandId = 8, Price = 3500000m, HorsePower = 1160, Engine = "6.5L NA V12 + Electric", TopSpeed = 402m, Acceleration = 2.5m, Stock = 2, Category = "HyperCar", Description = "Aston Martin and Red Bull Racing's F1-inspired hypercar.", ImageUrl = "/images/cars/valkyrie.jpg" }
            );
        }
    }
}
