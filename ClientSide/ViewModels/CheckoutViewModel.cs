using ClientSide.DataDtos;

namespace ClientSide.ViewModels
{
	public class CheckoutViewModel
	{
		public int OrderId { get; set; }
		public decimal TotalAmount { get; set; }
		public string CustomerName { get; set; }
		public string ShippingAddress { get; set; }
		public string PhoneNumber { get; set; }
		public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
	}
}
