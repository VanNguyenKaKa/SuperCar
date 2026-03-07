using HyperCar.BLL.DTOs;
using Microsoft.AspNetCore.Http;

namespace HyperCar.BLL.Interfaces
{
    public interface ICartService
    {
        CartDto GetCart(ISession session);
        void AddToCart(ISession session, int carId, string carName, string? carImage, decimal price, int quantity = 1);
        void UpdateQuantity(ISession session, int carId, int quantity);
        void RemoveFromCart(ISession session, int carId);
        void ClearCart(ISession session);
        int GetCartItemCount(ISession session);
    }
}
