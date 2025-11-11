using ClientSide.DataDtos;
using ClientSide.DataDtos; 
using System.Collections.Generic;

namespace ClientSide.ViewModels
{
	public class AdminDashboardViewModel
	{
		public List<UserDto> Users { get; set; } = new List<UserDto>();
		public List<ProductDto> Products { get; set; } = new List<ProductDto>();
		public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
	}
}