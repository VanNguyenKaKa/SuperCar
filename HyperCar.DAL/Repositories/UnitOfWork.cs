using HyperCar.DAL.Data;
using HyperCar.DAL.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace HyperCar.DAL.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HyperCarDbContext _context;

        // Lazy-initialized repository instances
        private IRepository<Brand>? _brands;
        private IRepository<Car>? _cars;
        private IRepository<Order>? _orders;
        private IRepository<OrderItem>? _orderItems;
        private IRepository<Payment>? _payments;
        private IRepository<Shipping>? _shippings;
        private IRepository<Review>? _reviews;
        private IRepository<ConversationHistory>? _conversationHistories;
        private IRepository<TransactionHistory>? _transactionHistories;
        private IRepository<ReportSnapshot>? _reportSnapshots;
        private IRepository<TestDriveBooking>? _testDriveBookings;
        private IRepository<Showroom>? _showrooms;

        public UnitOfWork(HyperCarDbContext context)
        {
            _context = context;
        }

        public IRepository<Brand> Brands =>
            _brands ??= new Repository<Brand>(_context);

        public IRepository<Car> Cars =>
            _cars ??= new Repository<Car>(_context);

        public IRepository<Order> Orders =>
            _orders ??= new Repository<Order>(_context);

        public IRepository<OrderItem> OrderItems =>
            _orderItems ??= new Repository<OrderItem>(_context);

        public IRepository<Payment> Payments =>
            _payments ??= new Repository<Payment>(_context);

        public IRepository<Shipping> Shippings =>
            _shippings ??= new Repository<Shipping>(_context);

        public IRepository<Review> Reviews =>
            _reviews ??= new Repository<Review>(_context);

        public IRepository<ConversationHistory> ConversationHistories =>
            _conversationHistories ??= new Repository<ConversationHistory>(_context);

        public IRepository<TransactionHistory> TransactionHistories =>
            _transactionHistories ??= new Repository<TransactionHistory>(_context);

        public IRepository<ReportSnapshot> ReportSnapshots =>
            _reportSnapshots ??= new Repository<ReportSnapshot>(_context);

        public IRepository<TestDriveBooking> TestDriveBookings =>
            _testDriveBookings ??= new Repository<TestDriveBooking>(_context);

        public IRepository<Showroom> Showrooms =>
            _showrooms ??= new Repository<Showroom>(_context);

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}

