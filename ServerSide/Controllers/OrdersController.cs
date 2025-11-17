using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.DataDtos;
using ServerSide.Models;

namespace ServerSide.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;

        public OrdersController(Prn232ClockShopContext context)
        {
            _context = context;
        }

		// GET: api/Orders
		[HttpGet]
		public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
		{
			var orders = await _context.Orders
									.Include(o => o.Customer)
									.Include(o => o.OrderDetails)
									.ToListAsync();

			var orderDtos = orders.Select(o => new
			{
				o.OrderId,
				o.OrderCode,
				o.CustomerId,
				o.OrderStatus,
				o.PaymentStatus,
				o.PaymentMethod,
				o.Note,
				o.ProcessedBy,
				o.CreatedAt,
				o.UpdatedAt,
				CustomerName = o.Customer?.FullName,
				TotalAmount = o.OrderDetails.Sum(od => od.Quantity * od.Price)
			});

			return Ok(orderDtos);
		}

		// GET: api/Orders/5
		[HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // GET: api/Orders/5
        [HttpGet("GetOrdersByCustomerId/{customerId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomerId(int customerId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            if (!orders.Any())
            {
                return NotFound(new { message = "Khách hàng chưa có đơn hàng nào." });
            }

            return Ok(orders);
        }

        [HttpGet("GetPurchaseHistory/{customerId}")]
        public async Task<ActionResult<IEnumerable<PurchaseHistoryItemDto>>> GetPurchaseHistory(int customerId)
        {
            var history = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderDetails)
                .SelectMany(o => o.OrderDetails.Select(d => new PurchaseHistoryItemDto
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderDate = o.CreatedAt,
                    OrderStatus = o.OrderStatus,
                    PaymentStatus = o.PaymentStatus,
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    Quantity = d.Quantity,
                    Price = d.Price,
                    TotalPrice = d.TotalPrice
                }))
                .OrderByDescending(x => x.OrderDate)
                .ToListAsync();

            return Ok(history);
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = new Order
            {
                OrderCode = dto.OrderCode,
                CustomerId = dto.CustomerId,
                OrderStatus = dto.OrderStatus,
                PaymentStatus = dto.PaymentStatus,
                PaymentMethod = dto.PaymentMethod,
                Note = dto.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderDetails = dto.OrderDetails.Select(d => new OrderDetail
                {
                    ProductId = d.ProductId,
                    ProductName = d.ProductName,
                    Quantity = d.Quantity,
                    Price = d.Price,
                    TotalPrice = d.Price * d.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }

		[HttpPut("{id}/UpdatePaymentStatus")]
		public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null)
			{
				return NotFound();
			}

			order.OrderStatus = dto.OrderStatus;
			order.PaymentStatus = dto.PaymentStatus;
			order.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return NoContent();
		}
	}
}
