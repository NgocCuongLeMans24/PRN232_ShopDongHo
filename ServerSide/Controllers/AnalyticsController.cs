using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;
using System.Linq;

namespace ServerSide.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly Prn232ClockShopContext _context;

        public AnalyticsController(Prn232ClockShopContext context)
        {
            _context = context;
        }

        [HttpGet("DashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            // Total Sales (all time)
            var totalSales = await _context.OrderDetails
                .SumAsync(od => od.TotalPrice);

            // Monthly Sales (current month)
            var monthlySales = await _context.Orders
                .Where(o => o.CreatedAt >= startOfMonth && o.CreatedAt < now)
                .Include(o => o.OrderDetails)
                .SelectMany(o => o.OrderDetails)
                .SumAsync(od => od.TotalPrice);

            // Total Orders
            var totalOrders = await _context.Orders.CountAsync();

            // Average Order Value
            var avgOrderValue = totalOrders > 0
                ? await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Select(o => o.OrderDetails.Sum(od => od.TotalPrice))
                    .AverageAsync()
                : 0;

            return Ok(new
            {
                TotalSales = totalSales,
                MonthlySales = monthlySales,
                TotalOrders = totalOrders,
                AverageOrderValue = avgOrderValue
            });
        }

        [HttpGet("SalesTrend")]
        public async Task<IActionResult> GetSalesTrend([FromQuery] string period = "daily")
        {
            var now = DateTime.Now;
            var data = new List<object>();

            if (period == "daily")
            {
                var startDate = now.AddDays(-30);
                var sales = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate)
                    .Include(o => o.OrderDetails)
                    .GroupBy(o => o.CreatedAt.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Sales = g.SelectMany(o => o.OrderDetails).Sum(od => od.TotalPrice)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                data = sales.Select(s => new { Date = s.Date.ToString("yyyy-MM-dd"), Sales = s.Sales }).ToList<object>();
            }
            else if (period == "weekly")
            {
                var startDate = now.AddDays(-84); // 12 weeks
                var sales = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate)
                    .Include(o => o.OrderDetails)
                    .GroupBy(o => new
                    {
                        Year = o.CreatedAt.Value.Year,
                        Week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                            o.CreatedAt.Value.Date,
                            System.Globalization.CalendarWeekRule.FirstDay,
                            DayOfWeek.Monday)
                    })
                    .Select(g => new
                    {
                        Week = $"Week {g.Key.Week}/{g.Key.Year}",
                        Sales = g.SelectMany(o => o.OrderDetails).Sum(od => od.TotalPrice)
                    })
                    .OrderBy(x => x.Week)
                    .ToListAsync();

                data = sales.Select(s => new { Period = s.Week, Sales = s.Sales }).ToList<object>();
            }
            else // monthly
            {
                var startDate = new DateTime(now.Year - 1, 1, 1);
                var sales = await _context.Orders
                    .Where(o => o.CreatedAt >= startDate)
                    .Include(o => o.OrderDetails)
                    .GroupBy(o => new { o.CreatedAt.Value.Year, o.CreatedAt.Value.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Sales = g.SelectMany(o => o.OrderDetails).Sum(od => od.TotalPrice)
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync();

                data = sales.Select(s => new { Period = s.Month, Sales = s.Sales }).ToList<object>();
            }

            return Ok(data);
        }

        [HttpGet("SalesByCategory")]
        public async Task<IActionResult> GetSalesByCategory()
        {
            var sales = await _context.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Where(od => od.Product != null && od.Product.Category != null)
                .GroupBy(od => od.Product.Category.CategoryName)
                .Select(g => new
                {
                    Category = g.Key ?? "Unknown",
                    Sales = g.Sum(od => od.TotalPrice),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Sales)
                .ToListAsync();

            return Ok(sales);
        }

        [HttpGet("MonthlySales")]
        public async Task<IActionResult> GetMonthlySales([FromQuery] int year = 0)
        {
            if (year == 0) year = DateTime.Now.Year;

            var sales = await _context.Orders
                .Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value.Year == year)
                .Include(o => o.OrderDetails)
                .GroupBy(o => o.CreatedAt.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key),
                    Sales = g.SelectMany(o => o.OrderDetails).Sum(od => od.TotalPrice)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            // Fill missing months with 0
            var allMonths = Enumerable.Range(1, 12).Select(m => new
            {
                Month = m,
                MonthName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m),
                Sales = sales.FirstOrDefault(s => s.Month == m)?.Sales ?? 0m
            });

            return Ok(allMonths);
        }

        [HttpGet("TopProducts")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
        {
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.Product != null)
                .GroupBy(od => new { od.ProductId, od.ProductName })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(limit)
                .ToListAsync();

            return Ok(topProducts);
        }

        [HttpGet("OrdersByStatus")]
        public async Task<IActionResult> GetOrdersByStatus()
        {
            var orders = await _context.Orders
                .GroupBy(o => o.OrderStatus ?? "Unknown")
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}

