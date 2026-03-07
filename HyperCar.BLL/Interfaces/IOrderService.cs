using HyperCar.BLL.DTOs;
using HyperCar.DAL.Enums;

namespace HyperCar.BLL.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(string userId, CreateOrderDto dto, CartDto cart);
        Task<OrderDto?> GetByIdAsync(int orderId);
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
        Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20, OrderStatus? status = null);
        Task<bool> UpdateStatusAsync(int orderId, OrderStatus newStatus, string? note = null, string? changedBy = null);
        Task<bool> ConfirmReceivedAsync(int orderId, string userId);
        Task<bool> RequestReturnAsync(int orderId, string userId);
        Task<bool> CancelOrderAsync(int orderId, string userId);
        Task<IEnumerable<TransactionHistoryDto>> GetOrderTimelineAsync(int orderId);
    }
}
