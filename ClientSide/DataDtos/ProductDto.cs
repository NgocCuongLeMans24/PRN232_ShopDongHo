namespace ClientSide.DataDtos
{
	public class ProductDto
	{
		public int ProductId { get; set; }
		public string ProductCode { get; set; }
		public string ProductName { get; set; }
		public decimal Price { get; set; }
		public int StockQuantity { get; set; }
		public bool? IsActive { get; set; }
		public DateTime? CreatedAt { get; set; }
		public string BrandName { get; set; }
		public string CategoryName { get; set; }
	}
}
