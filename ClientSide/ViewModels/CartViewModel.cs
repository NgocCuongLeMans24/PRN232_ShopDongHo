using System.Collections.Generic;
using ClientSide.DataDtos;

namespace ClientSide.ViewModels
{
	public class CartViewModel
	{
		public List<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
		public decimal TotalAmount { get; set; }
	}
}