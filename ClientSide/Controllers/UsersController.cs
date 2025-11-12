using ClientSide.DataDtos; // Đảm bảo using DTOs của bạn
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Http; // Để dùng Session
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ClientSide.Controllers
{
	public class UsersController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;

		private readonly string _urlBase = MyTools.getUrl();

		public UsersController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}

		[HttpGet]
		public async Task<IActionResult> Customer(
			int pageNumber = 1,
			int pageSize = 5,
			string searchTerm = "",
			string roleFilter = "All",
			string sortBy = "FullName",
			string sortOrder = "asc")
		{
			// 1. Kiểm tra bảo vệ
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			// 2. Build URL với các tham số query
			var builder = new UriBuilder($"{_urlBase}/api/Admin/GetUsersPaged");
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["pageNumber"] = pageNumber.ToString();
			query["pageSize"] = pageSize.ToString();
			query["searchTerm"] = searchTerm;
			query["roleFilter"] = "Customer";
			query["sortBy"] = sortBy;
			query["sortOrder"] = sortOrder;
			builder.Query = query.ToString();
			string url = builder.ToString();

			UserListViewModel viewModel = new UserListViewModel();
			try
			{
				// 3. Gọi API đã nâng cấp
				var response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				// 4. API trả về chính xác cấu trúc của ViewModel
				viewModel = JsonSerializer.Deserialize<UserListViewModel>(json, options);
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
				// ... (Xử lý lỗi 401/403)
			}

			// 5. Gửi ViewModel hoàn chỉnh sang cho View
			return View(viewModel);
		}


		[HttpGet]
		public async Task<IActionResult> Supplier(
			int pageNumber = 1,
			int pageSize = 5,
			string searchTerm = "",
			string roleFilter = "All",
			string sortBy = "FullName",
			string sortOrder = "asc")
		{
			// 1. Kiểm tra bảo vệ
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", token);

			// 2. Build URL với các tham số query
			var builder = new UriBuilder($"{_urlBase}/api/Admin/GetUsersPaged");
			var query = HttpUtility.ParseQueryString(string.Empty);
			query["pageNumber"] = pageNumber.ToString();
			query["pageSize"] = pageSize.ToString();
			query["searchTerm"] = searchTerm;
			query["roleFilter"] = "Supplier";
			query["sortBy"] = sortBy;
			query["sortOrder"] = sortOrder;
			builder.Query = query.ToString();
			string url = builder.ToString();

			UserListViewModel viewModel = new UserListViewModel();
			try
			{
				// 3. Gọi API đã nâng cấp
				var response = await client.GetAsync(url);
				response.EnsureSuccessStatusCode();

				var json = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

				// 4. API trả về chính xác cấu trúc của ViewModel
				viewModel = JsonSerializer.Deserialize<UserListViewModel>(json, options);
			}
			catch (HttpRequestException ex)
			{
				ViewBag.Error = $"Lỗi khi gọi API: {ex.Message}";
				// ... (Xử lý lỗi 401/403)
			}

			// 5. Gửi ViewModel hoàn chỉnh sang cho View
			return View(viewModel);
		}

		public IActionResult Create(int roleId)
		{
			var viewModel = new CreateUserViewModel
			{
				
				RoleId = roleId,
				IsActive = true
			};
			return View(viewModel);
		}

		// [POST] /Users/Create
		[HttpPost]
		public async Task<IActionResult> Create(CreateUserViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			var userCreateDto = new
			{
				Username = viewModel.Username,
				Password = viewModel.Password,
				Email = viewModel.Email,
				FullName = viewModel.FullName,
				PhoneNumber = viewModel.PhoneNumber,
				Address = viewModel.Address,
				RoleId = viewModel.RoleId
			};

			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var jsonContent = new StringContent(JsonSerializer.Serialize(userCreateDto), Encoding.UTF8, "application/json");

			try
			{
				var response = await client.PostAsync($"{_urlBase}/api/Users", jsonContent);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					ModelState.AddModelError(string.Empty, $"Lỗi từ API: {errorContent}");
					return View(viewModel);
				}

				if (viewModel.RoleId == 2)
				{
					return RedirectToAction("Supplier");
				}
				else
				{
					return RedirectToAction("Customer");
				}
			}
			catch (HttpRequestException ex)
			{
				ModelState.AddModelError(string.Empty, $"Lỗi kết nối: {ex.Message}");
				return View(viewModel);
			}
		}

		public async Task<IActionResult> Edit(int id)
		{
			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync($"{_urlBase}/api/Users/{id}");
			if (!response.IsSuccessStatusCode)
			{
				return RedirectToAction("Index");
			}

			var json = await response.Content.ReadAsStringAsync();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var userDto = JsonSerializer.Deserialize<UserDto>(json, options);

			var viewModel = new EditUserViewModel
			{
				UserId = userDto.UserId,
				Username = userDto.Username,
				Email = userDto.Email,
				FullName = userDto.FullName,
				PhoneNumber = userDto.PhoneNumber,
				Address = userDto.Address,
				RoleId = userDto.RoleId,
				IsActive = userDto.IsActive ?? true
			};

			return View(viewModel);
		}

		// --- ACTION ĐỂ XỬ LÝ LƯU THAY ĐỔI ---
		// [POST] /Users/Edit/5
		[HttpPost]
		public async Task<IActionResult> Edit(EditUserViewModel viewModel)
		{
			if (!ModelState.IsValid)
			{
				return View(viewModel);
			}

			var userEditDto = new
			{
				Email = viewModel.Email,
				FullName = viewModel.FullName,
				PhoneNumber = viewModel.PhoneNumber,
				Address = viewModel.Address,
				RoleId = viewModel.RoleId,
				IsActive = viewModel.IsActive,
				Password = viewModel.Password
			};

			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var jsonContent = new StringContent(JsonSerializer.Serialize(userEditDto), Encoding.UTF8, "application/json");

			try
			{
				var response = await client.PutAsync($"{_urlBase}/api/Users/{viewModel.UserId}", jsonContent);

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					ModelState.AddModelError(string.Empty, $"Lỗi từ API: {errorContent}");
					return View(viewModel);
				}

				if (viewModel.RoleId == 2)
				{
					return RedirectToAction("Supplier");
				}
				else
				{
					return RedirectToAction("Customer");
				}
			}
			catch (HttpRequestException ex)
			{
				ModelState.AddModelError(string.Empty, $"Lỗi kết nối: {ex.Message}");
				return View(viewModel);
			}
		}
	}
}

