using ClientSide.DataDtos;
using System.Collections.Generic;

namespace ClientSide.ViewModels
{
	public class AdminDashboardViewModel
	{
		public int TotalCustomerCount { get; set; }
		public int TotalProductCount { get; set; }
		public int TotalOrderCount { get; set; }
		public int TotalSupplierCount { get; set; }

		// 2 Thẻ danh sách dưới cùng (chỉ chứa 5 item gần nhất)
		public List<UserDto> RecentCustomers { get; set; } = new List<UserDto>();
		public List<ProductDto> RecentProducts { get; set; } = new List<ProductDto>();
		public List<OrderDto> RecentOrders { get; set; } = new List<OrderDto>();
		public List<UserDto> RecentSuppliers { get; set; } = new List<UserDto>();

		public List<UserDto> Users { get; set; } = new List<UserDto>();
		public List<ProductDto> Products { get; set; } = new List<ProductDto>();
		public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
	}
}