using ClientSide.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class SeedDataController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _urlBase = MyTools.getUrl();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public SeedDataController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSampleOrders()
        {
            var userRole = _httpContextAccessor.HttpContext?.Session.GetString("UserRole");
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");

            if (string.IsNullOrEmpty(userRole) || !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.PostAsync($"{_urlBase}/api/SeedData/CreateSampleOrders", null);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseText, _jsonOptions);
                    var message = result?.ContainsKey("message") == true 
                        ? result["message"].ToString() 
                        : "Đã tạo đơn hàng mẫu thành công!";
                    TempData["Success"] = message;
                }
                else
                {
                    var error = JsonSerializer.Deserialize<Dictionary<string, object>>(responseText, _jsonOptions);
                    var errorMessage = error?.ContainsKey("message") == true 
                        ? error["message"].ToString() 
                        : "Không thể tạo đơn hàng mẫu.";
                    TempData["Error"] = errorMessage;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction("Index", "Analytics");
        }
    }
}

