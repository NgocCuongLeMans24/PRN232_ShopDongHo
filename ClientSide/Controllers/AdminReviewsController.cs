using ClientSide.DataDtos;
using ClientSide.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class AdminReviewsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _urlBase = MyTools.getUrl();
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AdminReviewsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
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
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            List<PendingReviewDto> reviews = new();

            try
            {
                var response = await client.GetAsync($"{_urlBase}/api/Reviews/GetPending");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    reviews = JsonSerializer.Deserialize<List<PendingReviewDto>>(json, _jsonOptions) ?? new();
                }
                else
                {
                    ViewBag.Error = "Không thể tải danh sách đánh giá.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.PutAsync($"{_urlBase}/api/Reviews/{id}/Approve", null);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đánh giá đã được duyệt.";
                }
                else
                {
                    TempData["Error"] = "Không thể duyệt đánh giá.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Account");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.DeleteAsync($"{_urlBase}/api/Reviews/{id}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Đánh giá đã được xóa.";
                }
                else
                {
                    TempData["Error"] = "Không thể xóa đánh giá.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class PendingReviewDto
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}

