using ClientSide.DataDtos;
// --- DÙNG NAMESPACE MỚI ---
using ClientSide.DataDtos;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientSide.Controllers
{
	public class AdminController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;

		// URL của API backend (ServerSide)
		private readonly string _urlBase = MyTools.getUrl();

		public AdminController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<IActionResult> Index()
		{
			// Lấy token từ Session (giả sử đã lưu khi Login)
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			// Dùng ViewModel mới
			var viewModel = new AdminDashboardViewModel();
			var client = _httpClientFactory.CreateClient();

			// Gắn token vào header
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			try
			{
				// Gọi API và gán vào các danh sách DTO
				viewModel.Users = await GetApiData<List<UserDto>>(client, $"{_urlBase}/api/Users");
				viewModel.Products = await GetApiData<List<ProductDto>>(client, $"{_urlBase}/api/Products");
				viewModel.Orders = await GetApiData<List<OrderDto>>(client, $"{_urlBase}/api/Orders");
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
				// Xử lý lỗi 401/403
				if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
					ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
				{
					_httpContextAccessor.HttpContext?.Session.Clear();
					return RedirectToAction("Login", "Account");
				}
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