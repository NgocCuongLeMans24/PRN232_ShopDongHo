using ClientSide.Helper;
using ClientSide.Utils; // Giả sử MyTools ở đây
using Microsoft.AspNetCore.Http; // Để lấy IP
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

		[HttpPost]
		public IActionResult Checkout(int orderId, decimal totalAmount)
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
			pay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress.ToString());
			pay.AddRequestData("vnp_Locale", "vn");
			pay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {orderId}");
			pay.AddRequestData("vnp_OrderType", "other");
			pay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
			pay.AddRequestData("vnp_TxnRef", orderId.ToString());

			string paymentUrl = pay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

			return Redirect(paymentUrl);
		}

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

			bool checkSignature = pay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

			if (checkSignature)
			{
				if (vnp_ResponseCode == "00")
				{
					ViewBag.Message = "Giao dịch thành công!";
					await UpdateOrderStatusApi(orderId, "Đã xác nhận", "Đã thanh toán");
				}
				else
				{
					ViewBag.Message = $"Giao dịch thất bại. Mã lỗi: {vnp_ResponseCode}";
					await UpdateOrderStatusApi(orderId, "Chờ xác nhận", "Chưa thanh toán");
				}
			}
			else
			{
				ViewBag.Message = "Lỗi: Chữ ký không hợp lệ.";
			}

			return View();
		}

		private async Task UpdateOrderStatusApi(int orderId, string orderStatus, string paymentStatus)
		{
			var client = _httpClientFactory.CreateClient();
			var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			var updateDto = new { OrderStatus = orderStatus, PaymentStatus = paymentStatus };
			var jsonContent = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json");

			await client.PutAsync($"{_urlBase}/api/Orders/{orderId}/UpdatePaymentStatus", jsonContent);
		}
	}
}