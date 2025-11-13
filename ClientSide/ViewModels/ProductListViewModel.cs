using ClientSide.DataDtos;

namespace ClientSide.ViewModels
{
	public class ProductListViewModel
	{
		public List<ProductDto> Products { get; set; } = new List<ProductDto>();

		// Thông tin phân trang
		public int TotalCount { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
		public string SearchTerm { get; set; } = string.Empty;

		public int BrandId { get; set; }
		public int CategoryId { get; set; }

		public List<BrandDto> Brands { get; set; } = new List<BrandDto>();
		public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();

		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPreviousPage => PageNumber > 1;
		public bool HasNextPage => PageNumber < TotalPages;
	}
}
