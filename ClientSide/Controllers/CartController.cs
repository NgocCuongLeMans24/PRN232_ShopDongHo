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


		[HttpGet]
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

				var userInfo = await GetUserInfoFromApi(token);
				if (userInfo == null)
				{
					TempData["Error"] = "Không thể lấy thông tin người dùng.";
					return RedirectToAction("Index", "Home");
				}
				int customerId = userInfo.UserId;


				var orders = await GetOrdersByCustomerIdFromApi(token, customerId);

				var orderToPay = orders
									.Where(o => o.PaymentStatus == "Chưa thanh toán")
									.OrderByDescending(o => o.CreatedAt)
									.FirstOrDefault();

				if (orderToPay == null)
				{
					TempData["Error"] = "Không có đơn hàng nào chờ thanh toán.";
					return RedirectToAction("Index", "Orders");
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
					TotalAmount = orderToPay.TotalAmount,
					CartItems = cartItems,
					CustomerName = userInfo.FullName,
					ShippingAddress = userInfo.Address,
					PhoneNumber = userInfo.PhoneNumber
				};
				return View(viewModel);
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Lỗi khi chuẩn bị thanh toán: " + ex.Message;
				return RedirectToAction("Index", "Home");
			}
		}

		private async Task<List<OrderDto>> GetOrdersByCustomerIdFromApi(string token, int customerId)
		{
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync($"{_urlBase}/api/Orders/GetOrdersByCustomerId/{customerId}");

			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
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