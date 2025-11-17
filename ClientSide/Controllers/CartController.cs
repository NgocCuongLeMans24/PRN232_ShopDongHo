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
using System.Text;

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

		// GET: Hiển thị trang checkout với cart items
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

				// 1. LẤY DỮ LIỆU USER (Để lấy CustomerId)
				var userInfo = await GetUserInfoFromApi(token);
				if (userInfo == null)
				{
					TempData["Error"] = "Không thể lấy thông tin người dùng.";
					return RedirectToAction("Index", "Home");
				}
				int customerId = userInfo.UserId;

				// 2. LẤY DỮ LIỆU CART ITEMS từ API
				var cartItems = await GetCartItemsFromApi(token, customerId);

				if (cartItems == null || !cartItems.Any())
				{
					TempData["Error"] = "Giỏ hàng của bạn đang trống.";
					return RedirectToAction("Index", "Products");
				}

				// 3. TÍNH TỔNG TIỀN
				decimal totalAmount = cartItems.Sum(item => item.Price * item.Quantity);

				var viewModel = new CheckoutViewModel
				{
					OrderId = 0, // Chưa có order, sẽ tạo khi thanh toán
					TotalAmount = totalAmount,
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

		// POST: Tạo order từ cart items và chuyển đến thanh toán
		[HttpPost]
		public async Task<IActionResult> CreateOrderFromCart([FromForm] Dictionary<int, int> quantities)
		{
			string token = HttpContext.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				TempData["Error"] = "Bạn cần đăng nhập để thanh toán.";
				return RedirectToAction("Login", "Account");
			}

			try
			{
				var client = _httpClientFactory.CreateClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				// 1. Lấy thông tin user
				var userInfo = await GetUserInfoFromApi(token);
				if (userInfo == null)
				{
					TempData["Error"] = "Không thể lấy thông tin người dùng.";
					return RedirectToAction("Index", "Home");
				}
				int customerId = userInfo.UserId;

				// 2. Lấy cart items
				var cartItems = await GetCartItemsFromApi(token, customerId);
				if (cartItems == null || !cartItems.Any())
				{
					TempData["Error"] = "Giỏ hàng của bạn đang trống.";
					return RedirectToAction("Index", "Products");
				}

				// 3. Cập nhật số lượng nếu có thay đổi
				if (quantities != null && quantities.Any())
				{
					foreach (var item in cartItems)
					{
						if (quantities.ContainsKey(item.ProductId))
						{
							item.Quantity = quantities[item.ProductId];
						}
					}
				}

				// 4. Tạo order từ cart items
				var orderRequest = new
				{
					OrderCode = "ORD" + DateTime.Now.Ticks,
					CustomerId = customerId,
					OrderStatus = "Chờ xác nhận",
					PaymentStatus = "Chưa thanh toán",
					PaymentMethod = "VNPay",
					Note = "Đơn hàng từ giỏ hàng",
					ProcessedBy = (int?)null,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now,
					OrderDetails = cartItems.Select(item => new
					{
						ProductId = item.ProductId,
						ProductName = item.ProductName,
						Price = item.Price,
						Quantity = item.Quantity,
						TotalPrice = item.Price * item.Quantity
					}).ToArray()
				};

				string orderJson = JsonSerializer.Serialize(orderRequest);
				StringContent content = new StringContent(orderJson, Encoding.UTF8, "application/json");

				HttpResponseMessage resOrder = await client.PostAsync($"{_urlBase}/api/Orders", content);
				if (!resOrder.IsSuccessStatusCode)
				{
					var errorText = await resOrder.Content.ReadAsStringAsync();
					TempData["Error"] = "Tạo đơn hàng thất bại! " + errorText;
					return RedirectToAction("Checkout");
				}

				var orderResponseJson = await resOrder.Content.ReadAsStringAsync();
				var orderResponse = JsonSerializer.Deserialize<OrderDto>(orderResponseJson, new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

				// 5. Xóa cart items sau khi tạo order thành công
				await ClearCartFromApi(token, customerId);

				// 6. Tính tổng tiền và chuyển đến thanh toán
				decimal totalAmount = cartItems.Sum(item => item.Price * item.Quantity);
				return RedirectToAction("Checkout", "Payment", new { orderId = orderResponse.OrderId, totalAmount = totalAmount });
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Lỗi khi tạo đơn hàng: " + ex.Message;
				return RedirectToAction("Checkout");
			}
		}

		// POST: Tạo order COD từ cart items
		[HttpPost]
		public async Task<IActionResult> CreateOrderCOD([FromForm] Dictionary<int, int> quantities)
		{
			string token = HttpContext.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				TempData["Error"] = "Bạn cần đăng nhập để đặt hàng.";
				return RedirectToAction("Login", "Account");
			}

			try
			{
				var client = _httpClientFactory.CreateClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				// 1. Lấy thông tin user
				var userInfo = await GetUserInfoFromApi(token);
				if (userInfo == null)
				{
					TempData["Error"] = "Không thể lấy thông tin người dùng.";
					return RedirectToAction("Index", "Home");
				}
				int customerId = userInfo.UserId;

				// 2. Lấy cart items
				var cartItems = await GetCartItemsFromApi(token, customerId);
				if (cartItems == null || !cartItems.Any())
				{
					TempData["Error"] = "Giỏ hàng của bạn đang trống.";
					return RedirectToAction("Index", "Products");
				}

				// 3. Cập nhật số lượng nếu có thay đổi
				if (quantities != null && quantities.Any())
				{
					foreach (var item in cartItems)
					{
						if (quantities.ContainsKey(item.ProductId))
						{
							item.Quantity = quantities[item.ProductId];
						}
					}
				}

				// 4. Tạo order COD từ cart items
				var orderRequest = new
				{
					OrderCode = "ORD" + DateTime.Now.Ticks,
					CustomerId = customerId,
					OrderStatus = "Chờ xác nhận",
					PaymentStatus = "Chưa thanh toán",
					PaymentMethod = "COD",
					Note = "Đơn hàng từ giỏ hàng - Thanh toán khi nhận hàng",
					ProcessedBy = (int?)null,
					CreatedAt = DateTime.Now,
					UpdatedAt = DateTime.Now,
					OrderDetails = cartItems.Select(item => new
					{
						ProductId = item.ProductId,
						ProductName = item.ProductName,
						Price = item.Price,
						Quantity = item.Quantity,
						TotalPrice = item.Price * item.Quantity
					}).ToArray()
				};

				string orderJson = JsonSerializer.Serialize(orderRequest);
				StringContent content = new StringContent(orderJson, Encoding.UTF8, "application/json");

				HttpResponseMessage resOrder = await client.PostAsync($"{_urlBase}/api/Orders", content);
				if (!resOrder.IsSuccessStatusCode)
				{
					var errorText = await resOrder.Content.ReadAsStringAsync();
					TempData["Error"] = "Tạo đơn hàng thất bại! " + errorText;
					return RedirectToAction("Checkout");
				}

				// 5. Xóa cart items sau khi tạo order thành công
				await ClearCartFromApi(token, customerId);

				TempData["Success"] = "Đặt hàng thành công! Đơn hàng của bạn đang chờ xác nhận.";
				return RedirectToAction("Index", "Orders");
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Lỗi khi tạo đơn hàng: " + ex.Message;
				return RedirectToAction("Checkout");
			}
		}

		// --- HÀM HỖ TRỢ ĐỂ GỌI API CỦA BẠN ---

		private async Task<List<CartItemDto>> GetCartItemsFromApi(string token, int customerId)
		{
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.GetAsync($"{_urlBase}/api/Carts/GetByCustomer/{customerId}");

			if (response.IsSuccessStatusCode)
			{
				var json = await response.Content.ReadAsStringAsync();
				var cartData = JsonSerializer.Deserialize<List<CartItemApiResponse>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				
				if (cartData != null)
				{
					return cartData.Select(c => new CartItemDto
					{
						ProductId = c.ProductId,
						ProductName = c.ProductName,
						Price = c.Price,
						Quantity = c.Quantity
					}).ToList();
				}
			}
			return new List<CartItemDto>();
		}

		private async Task<bool> ClearCartFromApi(string token, int customerId)
		{
			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var response = await client.DeleteAsync($"{_urlBase}/api/Carts/Clear/{customerId}");
			return response.IsSuccessStatusCode;
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

		// Class để deserialize response từ Cart API
		private class CartItemApiResponse
		{
			public int CartId { get; set; }
			public int ProductId { get; set; }
			public string ProductName { get; set; }
			public decimal Price { get; set; }
			public int Quantity { get; set; }
			public decimal TotalPrice { get; set; }
		}
	}
}