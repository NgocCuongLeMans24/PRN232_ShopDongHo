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
    public class SeedDataController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;
        private readonly Random _random = new Random();

        public SeedDataController(Prn232ClockShopContext context)
        {
            _context = context;
        }

        [HttpPost("CreateSampleOrders")]
        public async Task<IActionResult> CreateSampleOrders()
        {
            try
            {
                // Lấy danh sách sản phẩm và khách hàng
                var products = await _context.Products.Where(p => p.IsActive == true).Take(10).ToListAsync();
                var customers = await _context.Users
                    .Where(u => u.RoleId == 3) // Customer role
                    .Take(5)
                    .ToListAsync();

                if (!products.Any() || !customers.Any())
                {
                    return BadRequest(new { message = "Cần có ít nhất 1 sản phẩm và 1 khách hàng để tạo đơn hàng mẫu." });
                }

                var ordersCreated = 0;
                var now = DateTime.Now;

                // Tạo đơn hàng cho 30 ngày qua (để test Daily chart)
                for (int day = 0; day < 30; day++)
                {
                    var orderDate = now.AddDays(-day);
                    
                    // Tạo 1-3 đơn hàng mỗi ngày
                    var ordersPerDay = _random.Next(1, 4);
                    
                    for (int i = 0; i < ordersPerDay; i++)
                    {
                        var customer = customers[_random.Next(customers.Count)];
                        var order = new Order
                        {
                            OrderCode = $"ORD{orderDate:yyyyMMdd}{ordersCreated + 1:D4}",
                            CustomerId = customer.UserId,
                            OrderStatus = "Đã Xác Nhận",
                            PaymentStatus = _random.Next(2) == 0 ? "Đã thanh toán" : "Chưa thanh toán",
                            PaymentMethod = _random.Next(2) == 0 ? "COD" : "Bank Transfer",
                            CreatedAt = orderDate.AddHours(_random.Next(8, 20)).AddMinutes(_random.Next(60)),
                            UpdatedAt = orderDate
                        };

                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        // Tạo OrderDetails (1-3 sản phẩm mỗi đơn)
                        var productCount = _random.Next(1, 4);
                        var selectedProducts = products.OrderBy(x => _random.Next()).Take(productCount).ToList();

                        foreach (var product in selectedProducts)
                        {
                            var quantity = _random.Next(1, 4);
                            var orderDetail = new OrderDetail
                            {
                                OrderId = order.OrderId,
                                ProductId = product.ProductId,
                                ProductName = product.ProductName,
                                Price = product.Price,
                                Quantity = quantity,
                                TotalPrice = product.Price * quantity
                            };

                            _context.OrderDetails.Add(orderDetail);
                        }

                        await _context.SaveChangesAsync();
                        ordersCreated++;
                    }
                }

                // Tạo thêm đơn hàng cho các tháng trước (để test Monthly chart)
                for (int month = 1; month <= 12; month++)
                {
                    var monthDate = new DateTime(now.Year, month, 15);
                    if (monthDate > now) continue; // Skip future months

                    var ordersPerMonth = _random.Next(5, 15);
                    
                    for (int i = 0; i < ordersPerMonth; i++)
                    {
                        var customer = customers[_random.Next(customers.Count)];
                        var dayInMonth = _random.Next(1, 28);
                        var orderDate = new DateTime(monthDate.Year, monthDate.Month, dayInMonth)
                            .AddHours(_random.Next(8, 20));

                        var order = new Order
                        {
                            OrderCode = $"ORD{orderDate:yyyyMMdd}{ordersCreated + 1:D4}",
                            CustomerId = customer.UserId,
                            OrderStatus = "Đã Xác Nhận",
                            PaymentStatus = "Đã thanh toán",
                            PaymentMethod = "Bank Transfer",
                            CreatedAt = orderDate,
                            UpdatedAt = orderDate
                        };

                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        // Tạo OrderDetails
                        var productCount = _random.Next(1, 4);
                        var selectedProducts = products.OrderBy(x => _random.Next()).Take(productCount).ToList();

                        foreach (var product in selectedProducts)
                        {
                            var quantity = _random.Next(1, 3);
                            var orderDetail = new OrderDetail
                            {
                                OrderId = order.OrderId,
                                ProductId = product.ProductId,
                                ProductName = product.ProductName,
                                Price = product.Price,
                                Quantity = quantity,
                                TotalPrice = product.Price * quantity
                            };

                            _context.OrderDetails.Add(orderDetail);
                        }

                        await _context.SaveChangesAsync();
                        ordersCreated++;
                    }
                }

                return Ok(new 
                { 
                    message = $"Đã tạo thành công {ordersCreated} đơn hàng mẫu!",
                    ordersCreated = ordersCreated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }

        [HttpDelete("ClearSampleOrders")]
        public async Task<IActionResult> ClearSampleOrders()
        {
            try
            {
                // Xóa tất cả OrderDetails trước
                var orderDetails = await _context.OrderDetails
                    .Where(od => od.Order.OrderCode.StartsWith("ORD"))
                    .ToListAsync();
                _context.OrderDetails.RemoveRange(orderDetails);

                // Xóa tất cả Orders có OrderCode bắt đầu bằng "ORD" (sample orders)
                var orders = await _context.Orders
                    .Where(o => o.OrderCode.StartsWith("ORD"))
                    .ToListAsync();
                _context.Orders.RemoveRange(orders);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã xóa tất cả đơn hàng mẫu." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}

