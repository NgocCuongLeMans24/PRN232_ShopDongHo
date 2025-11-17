using ClientSide.Helper;
using ClientSide.Utils; // Giả sử MyTools ở đây
using Microsoft.AspNetCore.Http; // Để lấy IP
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks; // Cho async

namespace ClientSide.Controllers
{
	public class PaymentController : Controller
	{
		private readonly IConfiguration _config;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly string _urlBase = MyTools.getUrl();

		public PaymentController(IConfiguration config, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
		{
			_config = config;
			_httpClientFactory = httpClientFactory;
			_httpContextAccessor = httpContextAccessor;
		}

		// GET: Hiển thị trang thanh toán
		[HttpGet]
		public async Task<IActionResult> Checkout(int orderId, decimal totalAmount)
		{
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				TempData["Error"] = "Bạn cần đăng nhập để thanh toán!";
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Lấy thông tin đơn hàng
			var orderRes = await client.GetAsync($"{_urlBase}/api/Orders/{orderId}");
			if (!orderRes.IsSuccessStatusCode)
			{
				TempData["Error"] = "Không tìm thấy đơn hàng!";
				return RedirectToAction("Index", "Orders");
			}

			var orderJson = await orderRes.Content.ReadAsStringAsync();
			var order = JsonSerializer.Deserialize<ClientSide.Models.Order>(orderJson, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (order == null)
			{
				TempData["Error"] = "Không tìm thấy đơn hàng!";
				return RedirectToAction("Index", "Orders");
			}

			ViewBag.OrderId = orderId;
			ViewBag.TotalAmount = totalAmount;
			ViewBag.Order = order;

			return View();
		}

		// POST: Xử lý thanh toán VNPay
		[HttpPost]
		public IActionResult ProcessPayment(int orderId, decimal totalAmount)
		{
			string vnp_ReturnUrl = _config.GetValue<string>("VnPay:PaymentBackReturnUrl");
			string vnp_Url = _config.GetValue<string>("VnPay:BaseUrl");
			string vnp_TmnCode = _config.GetValue<string>("VnPay:TmnCode");
			string vnp_HashSecret = _config.GetValue<string>("VnPay:HashSecret");

			VnPayLibrary pay = new VnPayLibrary();

			pay.AddRequestData("vnp_Version", "2.1.0");
			pay.AddRequestData("vnp_Command", "pay");
			pay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
			pay.AddRequestData("vnp_Amount", ((long)totalAmount * 100).ToString());
			pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
			pay.AddRequestData("vnp_CurrCode", "VND");
			pay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
			pay.AddRequestData("vnp_Locale", "vn");
			pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {orderId}");
			pay.AddRequestData("vnp_OrderType", "other");
			pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
			pay.AddRequestData("vnp_TxnRef", orderId.ToString());

			string paymentUrl = pay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

			return Redirect(paymentUrl);
		}

		// --- HÀM XỬ LÝ KẾT QUẢ TRẢ VỀ TỪ VNPAY ---
		[HttpGet]
		public async Task<IActionResult> PaymentCallback()
		{
			var vnpayData = HttpContext.Request.Query;
			VnPayLibrary pay = new VnPayLibrary();

			foreach (string s in vnpayData.Keys)
			{
				if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
				{
					pay.AddResponseData(s, vnpayData[s]);
				}
			}

			int orderId = Convert.ToInt32(pay.GetResponseData("vnp_TxnRef"));
			string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode");
			string vnp_SecureHash = pay.GetResponseData("vnp_SecureHash");
			string vnp_HashSecret = _config.GetValue<string>("VnPay:HashSecret");

			// Xác thực chữ ký
			bool checkSignature = pay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

			if (checkSignature)
			{
				if (vnp_ResponseCode == "00")
				{
					// Thanh toán thành công
					ViewBag.Message = "Giao dịch thành công!";

					// Gọi API (ServerSide) để cập nhật trạng thái đơn hàng
					// (Bạn có thể đổi "Processing", "Paid" thành trạng thái bạn muốn)
					await UpdateOrderStatusApi(orderId, "Đã xác nhận", "Đã thanh toán");
				}
				else
				{
					// Thanh toán thất bại
					ViewBag.Message = $"Giao dịch thất bại. Mã lỗi: {vnp_ResponseCode}";
					await UpdateOrderStatusApi(orderId, "Chờ xác nhận", "Chưa thanh toán");
				}
			}
			else
			{
				ViewBag.Message = "Lỗi: Chữ ký không hợp lệ.";
			}

			// Trả về View thông báo kết quả
			return View();
		}

		// POST: Thanh toán nhiều đơn hàng cùng lúc
		[HttpPost]
		public async Task<IActionResult> BulkPayment([FromForm] int[] orderIds)
		{
			if (orderIds == null || orderIds.Length == 0)
			{
				TempData["Error"] = "Vui lòng chọn ít nhất một đơn hàng để thanh toán!";
				return RedirectToAction("Index", "Orders");
			}

			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			if (string.IsNullOrEmpty(token))
			{
				TempData["Error"] = "Bạn cần đăng nhập để thanh toán!";
				return RedirectToAction("Login", "Account");
			}

			var client = _httpClientFactory.CreateClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Lấy thông tin user
			var userRes = await client.GetAsync($"{_urlBase}/api/Auth/current-user");
			if (!userRes.IsSuccessStatusCode)
			{
				TempData["Error"] = "Không thể lấy thông tin người dùng!";
				return RedirectToAction("Index", "Orders");
			}

			var userJson = await userRes.Content.ReadAsStringAsync();
			var currentUser = JsonSerializer.Deserialize<ClientSide.DataDtos.UserDto>(userJson, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (currentUser == null)
			{
				TempData["Error"] = "Không thể lấy thông tin người dùng!";
				return RedirectToAction("Index", "Orders");
			}

			// Lấy thông tin các đơn hàng đã chọn
			var orders = new List<ClientSide.Models.Order>();
			decimal totalAmount = 0;

			foreach (var orderId in orderIds)
			{
				var orderRes = await client.GetAsync($"{_urlBase}/api/Orders/{orderId}");
				if (orderRes.IsSuccessStatusCode)
				{
					var orderJsonResponse = await orderRes.Content.ReadAsStringAsync();
					var order = JsonSerializer.Deserialize<ClientSide.Models.Order>(orderJsonResponse, new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					});

					if (order != null && order.PaymentStatus != "Đã thanh toán" && order.CustomerId == currentUser.UserId)
					{
						orders.Add(order);
						totalAmount += order.OrderDetails?.Sum(d => d.TotalPrice) ?? 0;
					}
				}
			}

			if (orders.Count == 0)
			{
				TempData["Error"] = "Không có đơn hàng hợp lệ để thanh toán!";
				return RedirectToAction("Index", "Orders");
			}

			// Tạo đơn hàng tổng hợp từ các đơn đã chọn
			var combinedOrderDetails = new List<object>();
			foreach (var order in orders)
			{
				if (order.OrderDetails != null)
				{
					foreach (var detail in order.OrderDetails)
					{
						combinedOrderDetails.Add(new
						{
							ProductId = detail.ProductId,
							ProductName = detail.ProductName,
							Price = detail.Price,
							Quantity = detail.Quantity,
							TotalPrice = detail.TotalPrice
						});
					}
				}
			}

			// Tạo đơn hàng tổng hợp
			var combinedOrderRequest = new
			{
				OrderCode = "ORD" + DateTime.Now.Ticks,
				CustomerId = currentUser.UserId,
				OrderStatus = "Chờ xác nhận",
				PaymentStatus = "Chưa thanh toán",
				PaymentMethod = "VNPay",
				Note = $"Đơn hàng tổng hợp từ {orders.Count} đơn: {string.Join(", ", orders.Select(o => o.OrderCode))}",
				ProcessedBy = (int?)null,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now,
				OrderDetails = combinedOrderDetails.ToArray()
			};

			string orderJson = JsonSerializer.Serialize(combinedOrderRequest);
			StringContent content = new StringContent(orderJson, Encoding.UTF8, "application/json");

			HttpResponseMessage createRes = await client.PostAsync($"{_urlBase}/api/Orders", content);
			if (!createRes.IsSuccessStatusCode)
			{
				var errorText = await createRes.Content.ReadAsStringAsync();
				TempData["Error"] = "Tạo đơn hàng tổng hợp thất bại! " + errorText;
				return RedirectToAction("Index", "Orders");
			}

			var createdOrderJson = await createRes.Content.ReadAsStringAsync();
			var createdOrder = JsonSerializer.Deserialize<ClientSide.Models.Order>(createdOrderJson, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			if (createdOrder == null)
			{
				TempData["Error"] = "Không thể tạo đơn hàng tổng hợp!";
				return RedirectToAction("Index", "Orders");
			}

			// Chuyển đến trang thanh toán đơn hàng tổng hợp
			return RedirectToAction("Checkout", new { orderId = createdOrder.OrderId, totalAmount = totalAmount });
		}

		// Hàm hỗ trợ gọi API ServerSide
		private async Task UpdateOrderStatusApi(int orderId, string orderStatus, string paymentStatus)
		{
			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			// Khớp với DTO ở Bước 3
			var updateDto = new { OrderStatus = orderStatus, PaymentStatus = paymentStatus };
			var jsonContent = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json");

			// URL này phải khớp với API ở Bước 3
			await client.PutAsync($"{_urlBase}/api/Orders/{orderId}/UpdatePaymentStatus", jsonContent);
		}
	}
}