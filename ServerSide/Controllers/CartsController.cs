using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;

namespace ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartsController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;

        public CartsController(Prn232ClockShopContext context)
        {
            _context = context;
        }

        // GET: api/Carts/GetByCustomer/{customerId}
        [HttpGet("GetByCustomer/{customerId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetCartByCustomer(int customerId)
        {
            var cartItems = await _context.Carts
                .Include(c => c.Product)
                .Where(c => c.CustomerId == customerId)
                .Select(c => new
                {
                    c.CartId,
                    c.ProductId,
                    ProductName = c.Product != null ? c.Product.ProductName : "Unknown",
                    c.Product.Price,
                    c.Quantity,
                    TotalPrice = c.Quantity * c.Product.Price,
                    c.Product.Image,
                    c.Product.StockQuantity
                })
                .ToListAsync();

            return Ok(cartItems);
        }

        // POST: api/Carts/Add
        [HttpPost("Add")]
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if product exists
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại." });
            }

            // Check if item already in cart
            var existingCartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.CustomerId == dto.CustomerId && c.ProductId == dto.ProductId);

            if (existingCartItem != null)
            {
                // Update quantity
                var newQuantity = existingCartItem.Quantity + dto.Quantity;
                if (newQuantity > product.StockQuantity)
                {
                    return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Tồn kho hiện tại: {product.StockQuantity}" });
                }
                existingCartItem.Quantity = newQuantity;
            }
            else
            {
                // Add new item
                if (dto.Quantity > product.StockQuantity)
                {
                    return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Tồn kho hiện tại: {product.StockQuantity}" });
                }

                var cartItem = new Cart
                {
                    CustomerId = dto.CustomerId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    AddedAt = DateTime.Now
                };
                _context.Carts.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã thêm vào giỏ hàng." });
        }

        // PUT: api/Carts/UpdateQuantity
        [HttpPut("UpdateQuantity")]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQuantityDto dto)
        {
            var cartItem = await _context.Carts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.CartId == dto.CartId);

            if (cartItem == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            if (dto.Quantity <= 0)
            {
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });
            }

            if (dto.Quantity > cartItem.Product.StockQuantity)
            {
                return BadRequest(new { message = $"Số lượng vượt quá tồn kho. Tồn kho hiện tại: {cartItem.Product.StockQuantity}" });
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã cập nhật số lượng." });
        }

        // DELETE: api/Carts/{id}
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            var cartItem = await _context.Carts.FindAsync(id);
            if (cartItem == null)
            {
                return NotFound();
            }

            _context.Carts.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khỏi giỏ hàng." });
        }

        // DELETE: api/Carts/Clear/{customerId}
        [HttpDelete("Clear/{customerId}")]
        [Authorize]
        public async Task<IActionResult> ClearCart(int customerId)
        {
            var cartItems = await _context.Carts
                .Where(c => c.CustomerId == customerId)
                .ToListAsync();

            _context.Carts.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa toàn bộ giỏ hàng." });
        }
    }

    public class AddToCartDto
    {
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartQuantityDto
    {
        public int CartId { get; set; }
        public int Quantity { get; set; }
    }
}

