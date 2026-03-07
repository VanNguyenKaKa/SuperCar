using HyperCar.BLL.DTOs;

namespace HyperCar.BLL.Interfaces
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string ipAddress);
        Task<PaymentDto> ValidateResponseAsync(IDictionary<string, string> queryParams);
        Task<bool> UpdatePaymentAsync(int orderId, PaymentDto paymentDto);
    }
}
