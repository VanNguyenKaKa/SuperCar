using HyperCar.BLL.DTOs;
using HyperCar.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HyperCar.Web.Pages.Cart
{
    public class IndexModel : PageModel
    {
        private readonly ICartService _cartService;

        public IndexModel(ICartService cartService)
        {
            _cartService = cartService;
        }

        public CartDto? Cart { get; set; }

        public void OnGet()
        {
            Cart = _cartService.GetCart(HttpContext.Session);
        }

        /// <summary>
        /// AJAX endpoint: Add item to cart
        /// </summary>
        public IActionResult OnPostAdd(int carId, string carName, string? carImage, decimal price)
        {
            _cartService.AddToCart(HttpContext.Session, carId, carName, carImage, price);
            var count = _cartService.GetCartItemCount(HttpContext.Session);
            return new JsonResult(new { success = true, cartCount = count });
        }

        /// <summary>
        /// AJAX endpoint: Update item quantity
        /// </summary>
        public IActionResult OnPostUpdate(int carId, int quantity)
        {
            _cartService.UpdateQuantity(HttpContext.Session, carId, quantity);
            var cart = _cartService.GetCart(HttpContext.Session);
            return new JsonResult(new { success = true, cart });
        }

        /// <summary>
        /// AJAX endpoint: Remove item from cart
        /// </summary>
        public IActionResult OnPostRemove(int carId)
        {
            _cartService.RemoveFromCart(HttpContext.Session, carId);
            var cart = _cartService.GetCart(HttpContext.Session);
            return new JsonResult(new { success = true, cart });
        }
    }
}
