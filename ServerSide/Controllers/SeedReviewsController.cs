using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;
using System;

namespace ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SeedReviewsController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;
        private readonly Random _random = new Random();

        public SeedReviewsController(Prn232ClockShopContext context)
        {
            _context = context;
        }

        [HttpPost("CreateSampleReviews")]
        public async Task<IActionResult> CreateSampleReviews()
        {
            try
            {
                // Lấy danh sách sản phẩm
                var products = await _context.Products.Where(p => p.IsActive == true).Take(10).ToListAsync();
                
                // Lấy danh sách khách hàng đã mua hàng
                var customers = await _context.Users
                    .Where(u => u.RoleId == 3) // Customer role
                    .ToListAsync();

                if (!products.Any() || !customers.Any())
                {
                    return BadRequest(new { message = "Cần có ít nhất 1 sản phẩm và 1 khách hàng." });
                }

                // Lấy các đơn hàng đã xác nhận
                var orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(o => o.OrderStatus != null && 
                               (o.OrderStatus.Contains("Xác Nhận") || o.OrderStatus.Contains("xác nhận")))
                    .ToListAsync();

                if (!orders.Any())
                {
                    return BadRequest(new { message = "Chưa có đơn hàng nào. Vui lòng tạo đơn hàng trước." });
                }

                var reviewsCreated = 0;
                var reviewComments = new[]
                {
                    "Sản phẩm rất tốt, chất lượng cao!",
                    "Đồng hồ rất đẹp và bền, đáng mua.",
                    "Giao hàng nhanh, đóng gói cẩn thận.",
                    "Sản phẩm đúng như mô tả, hài lòng!",
                    "Chất lượng tốt, giá cả hợp lý.",
                    "Rất ưng ý với sản phẩm này.",
                    "Đồng hồ chạy rất chính xác.",
                    "Thiết kế đẹp, phù hợp với mọi phong cách.",
                    "Sản phẩm tốt hơn mong đợi!",
                    "Đáng giá từng đồng, sẽ mua lại.",
                    "Chất lượng tuyệt vời, rất hài lòng.",
                    "Sản phẩm đẹp, đóng gói kỹ lưỡng.",
                    "Giao hàng đúng hẹn, sản phẩm như mô tả.",
                    "Rất thích sản phẩm này!",
                    "Chất lượng tốt, giá cả phải chăng."
                };

                // Tạo reviews cho mỗi sản phẩm
                foreach (var product in products)
                {
                    // Tìm các đơn hàng có chứa sản phẩm này
                    var productOrders = orders
                        .Where(o => o.OrderDetails.Any(od => od.ProductId == product.ProductId))
                        .ToList();

                    if (!productOrders.Any()) continue;

                    // Tạo 3-5 reviews cho mỗi sản phẩm
                    var reviewCount = _random.Next(3, 6);
                    
                    for (int i = 0; i < reviewCount && i < productOrders.Count; i++)
                    {
                        var order = productOrders[i];
                        var customer = customers.FirstOrDefault(c => c.UserId == order.CustomerId);
                        if (customer == null) continue;

                        // Kiểm tra xem customer đã review sản phẩm này chưa
                        var existingReview = await _context.Reviews
                            .FirstOrDefaultAsync(r => r.ProductId == product.ProductId 
                                && r.CustomerId == customer.UserId);

                        if (existingReview != null) continue; // Skip if already reviewed

                        var rating = _random.Next(4, 6); // 4-5 stars (mostly positive)
                        var comment = reviewComments[_random.Next(reviewComments.Length)];

                        var review = new Review
                        {
                            ProductId = product.ProductId,
                            CustomerId = customer.UserId,
                            OrderId = order.OrderId,
                            Rating = rating,
                            Comment = comment,
                            CreatedAt = DateTime.Now.AddDays(-_random.Next(1, 30)), // Random date in last 30 days
                            UpdatedAt = DateTime.Now,
                            ApprovedBy = customer.UserId // Auto-approve for sample data
                        };

                        _context.Reviews.Add(review);
                        reviewsCreated++;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = $"Đã tạo thành công {reviewsCreated} đánh giá mẫu!",
                    reviewsCreated = reviewsCreated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}

