using ClientSide.DataDtos;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

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

				viewModel.RecentCustomers = customers
												.OrderByDescending(c => c.UserId)
												.Take(5)
												.ToList();

				viewModel.RecentProducts = allProducts
												.OrderByDescending(p => p.ProductId)
												.Take(5)
												.ToList();
				viewModel.RecentOrders = allOrders
												.OrderByDescending(o => o.OrderId)
												.Take(5)
												.ToList();

				viewModel.RecentSuppliers = suppliers
												.OrderByDescending(s => s.UserId)
												.Take(5)
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
				return View(new AdminDashboardViewModel());
			}

			return View(viewModel);
		}


		public async Task<IActionResult> AdminOrders(
			int pageNumber = 1,
			int pageSize = 10,
			string searchTerm = "",
			string statusFilter = "Đã Xác Nhận")
		{
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			var builder = new UriBuilder($"{_urlBase}/api/Admin/GetOrdersPaged");
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["pageNumber"] = pageNumber.ToString();
			query["pageSize"] = pageSize.ToString();
			query["searchTerm"] = searchTerm;
			query["statusFilter"] = statusFilter;
			builder.Query = query.ToString();
			string url = builder.ToString();

			OrderListViewModel viewModel = new OrderListViewModel();
			try
			{
				var response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				viewModel = JsonSerializer.Deserialize<OrderListViewModel>(json, options);
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
			}
			viewModel.StatusFilter = statusFilter;
			return View(viewModel);
		}

		private async Task LoadDropdownsToViewBag()
		{
			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			try
			{
				var brandResponse = await client.GetAsync($"{_urlBase}/api/Brands");
				if (brandResponse.IsSuccessStatusCode)
				{
					var json = await brandResponse.Content.ReadAsStringAsync();
					ViewBag.Brands = JsonSerializer.Deserialize<List<BrandDto>>(json, options);
				}

				var categoryResponse = await client.GetAsync($"{_urlBase}/api/Categories");
				if (categoryResponse.IsSuccessStatusCode)
				{
					var json = await categoryResponse.Content.ReadAsStringAsync();
					ViewBag.Categories = JsonSerializer.Deserialize<List<CategoryDto>>(json, options);
				}
			}
			catch (HttpRequestException)
			{
				ViewBag.Brands = new List<BrandDto>();
				ViewBag.Categories = new List<CategoryDto>();
			}
		}

		[HttpGet]
		public async Task<IActionResult> AdminProducts(
			int pageNumber = 1,
			int pageSize = 10,
			string searchTerm = "",
			int brandId = 0,
			int categoryId = 0)
		{
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			var builder = new UriBuilder($"{_urlBase}/api/Admin/GetProductsPaged");
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["pageNumber"] = pageNumber.ToString();
			query["pageSize"] = pageSize.ToString();
			query["searchTerm"] = searchTerm;
			query["brandId"] = brandId.ToString();
			query["categoryId"] = categoryId.ToString();
			builder.Query = query.ToString();
			string url = builder.ToString();

			ProductListViewModel viewModel = new ProductListViewModel();
			try
			{
				var response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				viewModel = JsonSerializer.Deserialize<ProductListViewModel>(json, options);
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
			}

			await LoadDropdownsToViewBag();

			viewModel.BrandId = brandId;
			viewModel.CategoryId = categoryId;

			return View(viewModel);
		}


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