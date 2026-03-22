using HyperCar.DAL.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HyperCar.DAL.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<Brand> Brands { get; }
        IRepository<Car> Cars { get; }
        IRepository<Order> Orders { get; }
        IRepository<OrderItem> OrderItems { get; }
        IRepository<Payment> Payments { get; }
        IRepository<Shipping> Shippings { get; }
        IRepository<Review> Reviews { get; }
        IRepository<ConversationHistory> ConversationHistories { get; }
        IRepository<TransactionHistory> TransactionHistories { get; }
        IRepository<ReportSnapshot> ReportSnapshots { get; }
        IRepository<TestDriveBooking> TestDriveBookings { get; }
        IRepository<Showroom> Showrooms { get; }
        Task<int> SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
