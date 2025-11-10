using ClientSide.DataDtos;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5000");

            var content = new StringContent(
            JsonSerializer.Serialize(new { Username = model.Username, PasswordHash = model.Password }),
            Encoding.UTF8,
            "application/json");

            var response = await client.PostAsync("/api/Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
                return View(model);
            }

            var json = await response.Content.ReadAsStringAsync();
            var loginData = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Lưu token vào Session
            HttpContext.Session.SetString("JwtToken", loginData.Token);
            HttpContext.Session.SetString("UserInfo", JsonSerializer.Serialize(loginData.User));

            return RedirectToAction("Index", "Products");
        }

        public class LoginResponse
        {
            public string Token { get; set; }
            public DateTime Expiration { get; set; }
            public UserDto User { get; set; }
        }
    }
}
