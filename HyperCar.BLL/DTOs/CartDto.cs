namespace HyperCar.BLL.DTOs
{
    /// <summary>
    /// Cart item stored in session — lightweight representation
    /// </summary>
    public class CartItemDto
    {
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? CarImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity;
    }

    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.Subtotal);
        public int TotalItems => Items.Sum(i => i.Quantity);
    }
}
