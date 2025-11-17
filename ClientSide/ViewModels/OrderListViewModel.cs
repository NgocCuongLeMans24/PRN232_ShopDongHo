using ClientSide.DataDtos;
using System.Collections.Generic;

namespace ClientSide.ViewModels
{
	public class OrderListViewModel
	{
		public List<OrderDto> Orders { get; set; } = new List<OrderDto>();

		public int TotalCount { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;

		public string SearchTerm { get; set; } = string.Empty;
		public string StatusFilter { get; set; } = "All";

		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPreviousPage => PageNumber > 1;
		public bool HasNextPage => PageNumber < TotalPages;
	}
}