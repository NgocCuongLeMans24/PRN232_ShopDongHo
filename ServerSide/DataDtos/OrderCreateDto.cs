using Microsoft.AspNetCore.Http.HttpResults;

namespace ServerSide.DataDtos
{
    public class OrderCreateDto
    {
        public string OrderCode { get; set; }
        public int CustomerId { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderDetailCreateDto> OrderDetails { get; set; }
    }
}
