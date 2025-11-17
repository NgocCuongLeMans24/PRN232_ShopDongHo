using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;
using System.Linq;

namespace ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;

        public ReviewsController(Prn232ClockShopContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/GetByProduct/5
        [HttpGet("GetByProduct/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Where(r => r.ProductId == productId && r.ApprovedBy != null) // Only approved reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReviewId,
                    r.Rating,
                    r.Comment,
                    r.CreatedAt,
                    CustomerName = r.Customer != null ? r.Customer.FullName : "Anonymous"
                })
                .ToListAsync();

            var averageRating = reviews.Any() 
                ? reviews.Average(r => (double)r.Rating) 
                : 0;

            var ratingDistribution = Enumerable.Range(1, 5)
                .Select(rating => new
                {
                    Rating = rating,
                    Count = reviews.Count(r => r.Rating == rating)
                })
                .ToList();

            return Ok(new
            {
                Reviews = reviews,
                AverageRating = Math.Round(averageRating, 1),
                TotalReviews = reviews.Count,
                RatingDistribution = ratingDistribution
            });
        }

        // GET: api/Reviews/CheckReviewStatus?customerId=X&productIds=1,2,3
        [HttpGet("CheckReviewStatus")]
        [Authorize]
        public async Task<IActionResult> CheckReviewStatus([FromQuery] int customerId, [FromQuery] string productIds)
        {
            if (string.IsNullOrEmpty(productIds))
            {
                return Ok(new List<object>());
            }

            var productIdList = productIds.Split(',')
                .Select(id => int.TryParse(id.Trim(), out var pid) ? pid : 0)
                .Where(id => id > 0)
                .ToList();

            if (!productIdList.Any())
            {
                return Ok(new List<object>());
            }

            var reviews = await _context.Reviews
                .Where(r => r.CustomerId == customerId && productIdList.Contains(r.ProductId))
                .Select(r => new
                {
                    r.ProductId,
                    r.Rating,
                    HasReviewed = true
                })
                .ToListAsync();

            var result = productIdList.Select(pid => new
            {
                ProductId = pid,
                HasReviewed = reviews.Any(r => r.ProductId == pid),
                Rating = reviews.FirstOrDefault(r => r.ProductId == pid)?.Rating ?? 0
            }).ToList();

            return Ok(result);
        }

        // POST: api/Reviews/Create
        [HttpPost("Create")]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return Unauthorized();
            }

            // Check if user has purchased this product (via OrderDetails)
            // Only allow review if payment status is "Đã thanh toán"
            var hasPurchased = await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.ProductId == dto.ProductId 
                    && od.Order != null 
                    && od.Order.CustomerId == user.UserId
                    && od.Order.PaymentStatus == "Đã thanh toán");

            if (!hasPurchased)
            {
                return BadRequest(new { message = "Bạn chỉ có thể đánh giá sản phẩm đã mua." });
            }

            // Check if user already reviewed this product
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.CustomerId == user.UserId);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Rating = dto.Rating;
                existingReview.Comment = dto.Comment;
                existingReview.UpdatedAt = DateTime.Now;
                existingReview.ApprovedBy = null; // Reset approval status when updated

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đánh giá đã được cập nhật.", ReviewId = existingReview.ReviewId });
            }

            // Create new review
            // Get OrderId from purchase history if not provided
            int orderId = dto.OrderId ?? 0;
            if (orderId == 0)
            {
                // Try to find the order ID from the user's purchase history
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => od.ProductId == dto.ProductId 
                        && od.Order != null 
                        && od.Order.CustomerId == user.UserId
                        && od.Order.PaymentStatus == "Đã thanh toán");
                
                if (orderDetail != null && orderDetail.Order != null)
                {
                    orderId = orderDetail.Order.OrderId;
                }
            }

            var review = new Review
            {
                ProductId = dto.ProductId,
                CustomerId = user.UserId,
                OrderId = orderId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                ApprovedBy = null // Requires admin approval
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá đã được gửi và đang chờ duyệt.", ReviewId = review.ReviewId });
        }

        // GET: api/Reviews/GetPending
        [HttpGet("GetPending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Product)
                .Where(r => r.ApprovedBy == null)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReviewId,
                    r.ProductId,
                    ProductName = r.Product != null ? r.Product.ProductName : "Unknown",
                    r.Rating,
                    r.Comment,
                    CustomerName = r.Customer != null ? r.Customer.FullName : "Anonymous",
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // PUT: api/Reviews/{id}/Approve
        [HttpPut("{id}/Approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            var username = User.Identity?.Name;
            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (admin == null)
            {
                return Unauthorized();
            }

            review.ApprovedBy = admin.UserId;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá đã được duyệt." });
        }

        // DELETE: api/Reviews/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đánh giá đã được xóa." });
        }
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int? OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
