using ClientSide.DataDtos;

namespace ClientSide.DataDtos
{
	public class OrderDetailDto
	{
		public int ProductId { get; set; }
		public int Quantity { get; set; }
		public decimal Price { get; set; }
		public ProductDto Product { get; set; }
	}
}