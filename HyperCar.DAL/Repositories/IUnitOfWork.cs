using HyperCar.DAL.Entities;

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
        Task<int> SaveChangesAsync();
    }
}
