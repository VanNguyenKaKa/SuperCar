using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HyperCar.BLL.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "HyperCarCart";

        public CartDto GetCart(ISession session)
        {
            var json = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(json))
                return new CartDto();

            return JsonSerializer.Deserialize<CartDto>(json) ?? new CartDto();
        }

        public void AddToCart(ISession session, int carId, string carName, string? carImage, decimal price, int quantity = 1)
        {
            var cart = GetCart(session);
            var existingItem = cart.Items.FirstOrDefault(i => i.CarId == carId);

            if (existingItem != null)
            {
                // Increment quantity if already in cart
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItemDto
                {
                    CarId = carId,
                    CarName = carName,
                    CarImage = carImage,
                    Price = price,
                    Quantity = quantity
                });
            }

            SaveCart(session, cart);
        }

        public void UpdateQuantity(ISession session, int carId, int quantity)
        {
            var cart = GetCart(session);
            var item = cart.Items.FirstOrDefault(i => i.CarId == carId);

            if (item != null)
            {
                if (quantity <= 0)
                    cart.Items.Remove(item);
                else
                    item.Quantity = quantity;
            }

            SaveCart(session, cart);
        }

        public void RemoveFromCart(ISession session, int carId)
        {
            var cart = GetCart(session);
            cart.Items.RemoveAll(i => i.CarId == carId);
            SaveCart(session, cart);
        }

        public void ClearCart(ISession session)
        {
            session.Remove(CartSessionKey);
        }

        public int GetCartItemCount(ISession session)
        {
            var cart = GetCart(session);
            return cart.TotalItems;
        }

        private void SaveCart(ISession session, CartDto cart)
        {
            var json = JsonSerializer.Serialize(cart);
            session.SetString(CartSessionKey, json);
        }
    }
}
