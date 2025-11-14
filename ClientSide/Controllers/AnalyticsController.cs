using ClientSide.DataDtos;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class AnalyticsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _urlBase = MyTools.getUrl();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AnalyticsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index([FromQuery] string period = "daily", [FromQuery] int year = 0)
        {
            var userRole = _httpContextAccessor.HttpContext?.Session.GetString("UserRole");
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(userRole) || !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                _httpContextAccessor.HttpContext?.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            if (year == 0) year = DateTime.Now.Year;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var viewModel = new AnalyticsViewModel
            {
                Period = period,
                Year = year
            };

            try
            {
                // Get Dashboard Stats
                var statsResponse = await client.GetAsync($"{_urlBase}/api/Analytics/DashboardStats");
                if (statsResponse.IsSuccessStatusCode)
                {
                    var statsJson = await statsResponse.Content.ReadAsStringAsync();
                    viewModel.Stats = JsonSerializer.Deserialize<DashboardStatsDto>(statsJson, _jsonOptions) ?? new();
                }

                // Get Sales Trend
                var trendResponse = await client.GetAsync($"{_urlBase}/api/Analytics/SalesTrend?period={period}");
                if (trendResponse.IsSuccessStatusCode)
                {
                    var trendJson = await trendResponse.Content.ReadAsStringAsync();
                    var trendData = JsonSerializer.Deserialize<List<object>>(trendJson, _jsonOptions) ?? new();
                    viewModel.SalesTrend = trendData.Select(item =>
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(item.ToString() ?? "{}", _jsonOptions);
                        return new SalesTrendItemDto
                        {
                            Date = dict?.ContainsKey("Date") == true ? dict["Date"].GetString() ?? "" : "",
                            Period = dict?.ContainsKey("Period") == true ? dict["Period"].GetString() ?? "" : "",
                            Sales = dict?.ContainsKey("Sales") == true ? dict["Sales"].GetDecimal() : 0
                        };
                    }).ToList();
                }

                // Get Sales by Category
                var categoryResponse = await client.GetAsync($"{_urlBase}/api/Analytics/SalesByCategory");
                if (categoryResponse.IsSuccessStatusCode)
                {
                    var categoryJson = await categoryResponse.Content.ReadAsStringAsync();
                    viewModel.SalesByCategory = JsonSerializer.Deserialize<List<SalesByCategoryDto>>(categoryJson, _jsonOptions) ?? new();
                }

                // Get Monthly Sales
                var monthlyResponse = await client.GetAsync($"{_urlBase}/api/Analytics/MonthlySales?year={year}");
                if (monthlyResponse.IsSuccessStatusCode)
                {
                    var monthlyJson = await monthlyResponse.Content.ReadAsStringAsync();
                    viewModel.MonthlySales = JsonSerializer.Deserialize<List<MonthlySalesDto>>(monthlyJson, _jsonOptions) ?? new();
                }

                // Get Top Products
                var topProductsResponse = await client.GetAsync($"{_urlBase}/api/Analytics/TopProducts?limit=10");
                if (topProductsResponse.IsSuccessStatusCode)
                {
                    var topProductsJson = await topProductsResponse.Content.ReadAsStringAsync();
                    viewModel.TopProducts = JsonSerializer.Deserialize<List<TopProductDto>>(topProductsJson, _jsonOptions) ?? new();
                }

                // Get Orders by Status
                var statusResponse = await client.GetAsync($"{_urlBase}/api/Analytics/OrdersByStatus");
                if (statusResponse.IsSuccessStatusCode)
                {
                    var statusJson = await statusResponse.Content.ReadAsStringAsync();
                    viewModel.OrdersByStatus = JsonSerializer.Deserialize<List<OrdersByStatusDto>>(statusJson, _jsonOptions) ?? new();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi khi tải dữ liệu: {ex.Message}";
            }

            return View(viewModel);
        }
    }
}

