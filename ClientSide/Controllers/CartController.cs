using ClientSide.ViewModels;
using ClientSide.DataDtos;
using System.Text.Json;
using System.Net.Http.Headers;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ClientSide.Controllers
{
	public class CartController : Controller
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly string _urlBase = MyTools.getUrl();

		public CartController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}

		// --- SỬA LỖI Ở HÀM [GET] NÀY ---
		[HttpGet] // Đây là action [GET] để hiển thị trang
		public async Task<IActionResult> Checkout()
		{
			string token = HttpContext.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				return RedirectToAction("Login", "Account");
			}

			try
			{
				var client = _httpClientFactory.CreateClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				// 1. LẤY DỮ LIỆU USER (Để lấy CustomerId)
				var userInfo = await GetUserInfoFromApi(token);
				if (userInfo == null)
				{
					TempData["Error"] = "Không thể lấy thông tin người dùng.";
					return RedirectToAction("Index", "Home");
				}
				int customerId = userInfo.UserId;

				// 2. LẤY DỮ LIỆU ORDER (Dùng API của bạn)
				var orders = await GetOrdersByCustomerIdFromApi(token, customerId);

				// 3. TÌM ĐƠN HÀNG CẦN THANH TOÁN
				// (Giả sử bạn muốn thanh toán đơn hàng MỚI NHẤT mà "Chưa thanh toán")
				var orderToPay = orders
									.Where(o => o.PaymentStatus == "Chưa thanh toán")
									.OrderByDescending(o => o.CreatedAt)
									.FirstOrDefault();

				if (orderToPay == null)
				{
					TempData["Error"] = "Không có đơn hàng nào chờ thanh toán.";
					return RedirectToAction("Index", "Orders"); // Quay về trang lịch sử
				}


				var cartItems = orderToPay.OrderDetail.Select(detail => new CartItemDto
				{
					ProductId = detail.ProductId,
					ProductName = detail.Product?.ProductName ?? "Sản phẩm",
					Price = detail.Price,
					Quantity = detail.Quantity
				}).ToList();


				var viewModel = new CheckoutViewModel
				{
					OrderId = orderToPay.OrderId,
					TotalAmount = orderToPay.TotalAmount, // Giả sử OrderDto có TotalAmount
					CartItems = cartItems,
					CustomerName = userInfo.FullName,
					ShippingAddress = userInfo.Address,
					PhoneNumber = userInfo.PhoneNumber
				};

				// 6. GỬI VIEWMODEL NÀY CHO VIEW "CHECKOUT.CSHTML"
				return View(viewModel); // Gửi CheckoutViewModel (sửa lỗi)
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Lỗi khi chuẩn bị thanh toán: " + ex.Message;
				return RedirectToAction("Index", "Home");
			}
		}

		// --- HÀM HỖ TRỢ ĐỂ GỌI API CỦA BẠN ---

		private async Task<List<OrderDto>> GetOrdersByCustomerIdFromApi(string token, int customerId)
		{
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Gọi API "GetOrdersByCustomerId" (API của bạn)
			var response = await client.GetAsync($"{_urlBase}/api/Orders/GetOrdersByCustomerId/{customerId}");

			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				// (Lưu ý: Chúng ta dùng OrderDto, không phải Order (Model))
				return JsonSerializer.Deserialize<List<OrderDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}
			return new List<OrderDto>();
		}

		private async Task<UserDto> GetUserInfoFromApi(string token)
		{
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			var response = await client.GetAsync($"{_urlBase}/api/Auth/current-user");

			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				return JsonSerializer.Deserialize<UserDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			}
			return null;
		}
	}
}