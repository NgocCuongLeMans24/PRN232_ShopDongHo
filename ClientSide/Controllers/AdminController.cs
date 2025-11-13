using ClientSide.DataDtos;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ClientSide.Controllers
{
	public class AdminController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;

		private readonly string _urlBase = MyTools.getUrl();

		public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<IActionResult> Index()
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

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			var viewModel = new AdminDashboardViewModel();

			try
			{
				var allUsers = await GetApiData<List<UserDto>>(client, $"{_urlBase}/api/Users");
				var allProducts = await GetApiData<List<ProductDto>>(client, $"{_urlBase}/api/Products");
				var allOrders = await GetApiData<List<OrderDto>>(client, $"{_urlBase}/api/Orders");

				var customers = allUsers
								.Where(u => u.RoleId == 3)
								.ToList();

				var suppliers = allUsers
								.Where(u => u.RoleId == 2)
								.ToList();

				viewModel.Products = allProducts;
				viewModel.Orders = allOrders;

				viewModel.TotalCustomerCount = customers.Count;
				viewModel.TotalProductCount = allProducts.Count;
				viewModel.TotalOrderCount = allOrders.Count;
				viewModel.TotalSupplierCount = suppliers.Count;

				// Điền các danh sách rút gọn (cho 2 thẻ dưới)
				viewModel.RecentCustomers = customers
												.OrderByDescending(c => c.UserId) // Giả sử có UserId
												.Take(5) // Lấy 5 người mới nhất
												.ToList();

				viewModel.RecentProducts = allProducts
												.OrderByDescending(p => p.ProductId) // Giả sử có ProductId
												.Take(5) // Lấy 5 sản phẩm mới nhất
												.ToList();
				viewModel.RecentOrders = allOrders
												.OrderByDescending(o => o.OrderDate)
												.Take(5)
												.ToList();

				viewModel.RecentSuppliers = suppliers
												.OrderByDescending(s => s.UserId) // Giả sử có UserId
												.Take(5) // Lấy 5 nhà cung cấp mới nhất
												.ToList();
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
				if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
					ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
				{
					_httpContextAccessor.HttpContext?.Session.Clear();
					return RedirectToAction("Login", "Account");
				}
				// Nếu lỗi, trả về ViewModel trống để View không bị crash
				return View(new AdminDashboardViewModel());
			}

			return View(viewModel);
		}



		// Hàm hỗ trợ gọi API
		private async Task<T> GetApiData<T>(HttpClient client, string url)
		{
			var response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			var jsonString = await response.Content.ReadAsStringAsync();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return JsonSerializer.Deserialize<T>(jsonString, options) ?? default(T);
		}
	}
}