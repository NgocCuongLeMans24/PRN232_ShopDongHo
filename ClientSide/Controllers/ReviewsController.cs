using ClientSide.DataDtos;
using ClientSide.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class ReviewsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _urlBase = MyTools.getUrl();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ReviewsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        public async Task<IActionResult> Create(int ProductId, int Rating, string? Comment, int? OrderId = null)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
                return RedirectToAction("ProductDetail", "Products", new { id = ProductId });
            }

            if (Rating < 1 || Rating > 5)
            {
                TempData["Error"] = "Đánh giá phải từ 1 đến 5 sao.";
                return RedirectToAction("ProductDetail", "Products", new { id = ProductId });
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var reviewData = new
            {
                ProductId,
                OrderId,
                Rating,
                Comment
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(reviewData),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                var response = await client.PostAsync($"{_urlBase}/api/Reviews/Create", jsonContent);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đánh giá của bạn đã được gửi và đang chờ duyệt. Cảm ơn bạn!";
                }
                else
                {
                    var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseText, _jsonOptions);
                    var errorMessage = errorObj?.ContainsKey("message") == true 
                        ? errorObj["message"].ToString() 
                        : "Không thể gửi đánh giá. Vui lòng thử lại.";
                    TempData["Error"] = errorMessage;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction("ProductDetail", "Products", new { id = ProductId });
        }
    }
}

