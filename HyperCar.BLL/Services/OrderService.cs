using HyperCar.BLL.DTOs;
using HyperCar.BLL.Helpers;
using HyperCar.BLL.Interfaces;
using HyperCar.DAL.Entities;
using HyperCar.DAL.Enums;
using HyperCar.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HyperCar.BLL.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<OrderDto> CreateOrderAsync(string userId, CreateOrderDto dto, CartDto cart)
        {
            if (!cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            // Calculate totals
            var subtotal = cart.TotalAmount;

            var order = new Order
            {
                UserId = userId,
                TotalAmount = subtotal,
                ShippingFee = 0, // Will be updated after shipping calculation
                Status = OrderStatus.Pending,
                ShippingAddress = dto.ShippingAddress,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                Note = dto.Note,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // Create order items and deduct stock
            foreach (var item in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    CarId = item.CarId,
                    Price = item.Price,
                    Quantity = item.Quantity
                };

                await _unitOfWork.OrderItems.AddAsync(orderItem);

                // Deduct stock
                var car = await _unitOfWork.Cars.GetByIdAsync(item.CarId);
                if (car != null)
                {
                    car.Stock -= item.Quantity;
                    if (car.Stock < 0) car.Stock = 0;
                    _unitOfWork.Cars.Update(car);
                }
            }

            // Create initial transaction history entry
            await AddTransactionHistoryAsync(order.Id, null, OrderStatus.Pending.ToString(),
                "Tạo đơn hàng", "System");

            // Create payment record (pending)
            var payment = new Payment
            {
                OrderId = order.Id,
                Method = "VNPay",
                Amount = subtotal,
                Status = PaymentStatus.Pending,
                CreatedDate = DateTime.UtcNow
            };
            await _unitOfWork.Payments.AddAsync(payment);

            // Create shipping record
            var shipping = new Shipping
            {
                OrderId = order.Id,
                Provider = "ViettelPost",
                Status = ShippingStatus.Calculating,
                Address = dto.ShippingAddress,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                ProvinceId = dto.ProvinceId,
                DistrictId = dto.DistrictId,
                WardCode = dto.WardCode,
                CreatedDate = DateTime.UtcNow
            };
            await _unitOfWork.Shippings.AddAsync(shipping);

            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(order.Id) ?? throw new Exception("Failed to create order");
        }

        public async Task<OrderDto?> GetByIdAsync(int orderId)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Car)
                .Include(o => o.Payment)
                .Include(o => o.Shipping)
                .Include(o => o.TransactionHistories)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order == null ? null : MapToDto(order);
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
        {
            var orders = await _unitOfWork.Orders.Query()
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Car)
                .Include(o => o.Payment)
                .Include(o => o.Shipping)
                .Include(o => o.TransactionHistories)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return orders.Select(MapToDto);
        }

        public async Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20, OrderStatus? status = null)
        {
            var query = _unitOfWork.Orders.Query()
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Car)
                .Include(o => o.Payment)
                .Include(o => o.Shipping)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<OrderDto>
            {
                Items = orders.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Transitions order status and records in transaction history.
        /// Restores stock when cancelling.
        /// </summary>
        public async Task<bool> UpdateStatusAsync(int orderId, OrderStatus newStatus, string? note = null, string? changedBy = null)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return false;

            var oldStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedDate = DateTime.UtcNow;

            // Restore stock when cancelling (business logic)
            if (newStatus == OrderStatus.Cancelled && oldStatus != OrderStatus.Cancelled)
            {
                foreach (var item in order.OrderItems)
                {
                    var car = await _unitOfWork.Cars.GetByIdAsync(item.CarId);
                    if (car != null)
                    {
                        car.Stock += item.Quantity;
                        _unitOfWork.Cars.Update(car);
                    }
                }
            }

            _unitOfWork.Orders.Update(order);

            // Record status change in timeline
            await AddTransactionHistoryAsync(orderId, oldStatus.ToString(), newStatus.ToString(),
                note ?? $"Cập nhật trạng thái sang {StatusHelper.ToVietnamese(newStatus)}", changedBy ?? "System");

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmReceivedAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Orders.Query()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Delivered) return false;

            order.UserAction = UserOrderAction.ConfirmReceived;
            order.Status = OrderStatus.Completed;
            order.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);

            await AddTransactionHistoryAsync(orderId, OrderStatus.Delivered.ToString(),
                OrderStatus.Completed.ToString(), "Khách hàng xác nhận đã nhận hàng", userId);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequestReturnAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Orders.Query()
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status != OrderStatus.Delivered) return false;

            order.UserAction = UserOrderAction.RequestReturn;
            order.UpdatedDate = DateTime.UtcNow;

            _unitOfWork.Orders.Update(order);

            await AddTransactionHistoryAsync(orderId, order.Status.ToString(),
                order.Status.ToString(), "Khách hàng yêu cầu trả hàng", userId);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Orders.Query()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Status > OrderStatus.Confirmed) return false;

            var oldStatus = order.Status;
            order.Status = OrderStatus.Cancelled;
            order.UpdatedDate = DateTime.UtcNow;

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                var car = await _unitOfWork.Cars.GetByIdAsync(item.CarId);
                if (car != null)
                {
                    car.Stock += item.Quantity;
                    _unitOfWork.Cars.Update(car);
                }
            }

            _unitOfWork.Orders.Update(order);

            await AddTransactionHistoryAsync(orderId, oldStatus.ToString(),
                OrderStatus.Cancelled.ToString(), "Đơn hàng bị hủy bởi khách hàng", userId);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TransactionHistoryDto>> GetOrderTimelineAsync(int orderId)
        {
            var history = await _unitOfWork.TransactionHistories.Query()
                .Where(t => t.OrderId == orderId)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync();

            return history.Select(t => new TransactionHistoryDto
            {
                Id = t.Id,
                OrderId = t.OrderId,
                StatusFrom = t.StatusFrom,
                StatusTo = t.StatusTo,
                Note = t.Note,
                ChangedBy = t.ChangedBy,
                CreatedDate = t.CreatedDate
            });
        }

        private async Task AddTransactionHistoryAsync(int orderId, string? from, string? to, string note, string? changedBy)
        {
            var history = new TransactionHistory
            {
                OrderId = orderId,
                StatusFrom = from,
                StatusTo = to,
                Note = note,
                ChangedBy = changedBy,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.TransactionHistories.AddAsync(history);
        }

        private static OrderDto MapToDto(Order order) => new()
        {
            Id = order.Id,
            UserId = order.UserId,
            UserName = order.User?.FullName ?? "",
            UserEmail = order.User?.Email ?? "",
            TotalAmount = order.TotalAmount,
            ShippingFee = order.ShippingFee,
            Status = order.Status,
            UserAction = order.UserAction,
            ShippingAddress = order.ShippingAddress,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            Note = order.Note,
            CreatedDate = order.CreatedDate,
            Items = order.OrderItems?.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                CarId = oi.CarId,
                CarName = oi.Car?.Name ?? "",
                CarImage = oi.Car?.ImageUrl,
                Price = oi.Price,
                Quantity = oi.Quantity
            }).ToList() ?? new(),
            Payment = order.Payment == null ? null : new PaymentDto
            {
                Id = order.Payment.Id,
                OrderId = order.Payment.OrderId,
                Method = order.Payment.Method,
                Amount = order.Payment.Amount,
                TransactionRef = order.Payment.TransactionRef,
                BankCode = order.Payment.BankCode,
                Status = order.Payment.Status,
                VnPayResponseCode = order.Payment.VnPayResponseCode,
                CreatedDate = order.Payment.CreatedDate,
                PaidAt = order.Payment.PaidAt
            },
            Shipping = order.Shipping == null ? null : new ShippingDto
            {
                Id = order.Shipping.Id,
                OrderId = order.Shipping.OrderId,
                Provider = order.Shipping.Provider,
                TrackingCode = order.Shipping.TrackingCode,
                Fee = order.Shipping.Fee,
                Status = order.Shipping.Status,
                EstimatedDelivery = order.Shipping.EstimatedDelivery,
                Address = order.Shipping.Address
            },
            Timeline = order.TransactionHistories?.OrderBy(t => t.CreatedDate).Select(t => new TransactionHistoryDto
            {
                Id = t.Id,
                OrderId = t.OrderId,
                StatusFrom = t.StatusFrom,
                StatusTo = t.StatusTo,
                Note = t.Note,
                ChangedBy = t.ChangedBy,
                CreatedDate = t.CreatedDate
            }).ToList() ?? new()
        };
    }
}
