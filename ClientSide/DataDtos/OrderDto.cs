namespace ClientSide.DataDtos
{
	public class OrderDto
	{
		public int OrderId { get; set; }
		public string OrderCode { get; set; } = null!;
		public int CustomerId { get; set; }
		public string? OrderStatus { get; set; }
		public string? PaymentStatus { get; set; }
		public string? PaymentMethod { get; set; }
		public string? Note { get; set; }
		public int? ProcessedBy { get; set; }
		public DateTime? CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public decimal TotalAmount { get; set; }
		public string? CustomerName { get; set; }
		public string? CustomerPhoneNumber { get; set; }
		public List<OrderDetailDto> OrderDetail { get; set; } = new List<OrderDetailDto>();
	}
}
