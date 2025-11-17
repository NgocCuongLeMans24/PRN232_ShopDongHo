using ClientSide.DataDtos;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace ClientSide.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5000");

            var registerDto = new RegisterDto
            {
                Username = model.Username,
                PasswordHash = model.Password,
                Email = model.Email,
                FullName = model.FullName
            };

            var content = new StringContent(JsonSerializer.Serialize(registerDto), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/Auth/register", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // Đọc message từ API
                var json = JsonDocument.Parse(responseBody);
                var message = json.RootElement.GetProperty("message").GetString();

                ViewBag.Success = message;
                return View();
            }
            else
            {
                ViewBag.Error = $"Đăng ký thất bại! {responseBody}";
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Verify(string token)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5000");

            var response = await client.GetAsync($"/api/Auth/verify?token={token}");
            var result = await response.Content.ReadAsStringAsync();

            ViewBag.Message = response.IsSuccessStatusCode
                ? "Tài khoản của bạn đã được xác minh!"
                : "Liên kết xác minh không hợp lệ hoặc đã hết hạn.";

            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5000");

            var content = new StringContent(
                JsonSerializer.Serialize(new { Email = model.Email }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/Auth/forgot-password", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(responseBody);
                ViewBag.Success = json.RootElement.GetProperty("message").GetString();
            }
            else
            {
                ViewBag.Error = "Không thể gửi email đặt lại mật khẩu.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token)
        {
            var model = new ResetPasswordViewModel { Token = token };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View(model);
            }

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://localhost:5000");

            var content = new StringContent(
                JsonSerializer.Serialize(new { Token = model.Token, NewPassword = model.NewPassword }),
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("/api/Auth/reset-password", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var json = JsonDocument.Parse(responseBody);
                ViewBag.Success = json.RootElement.GetProperty("message").GetString();
            }
            else
            {
                ViewBag.Error = "Không thể đặt lại mật khẩu. Liên kết có thể đã hết hạn.";
            }

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

			var userRole = loginData.User?.RoleName ?? "Customer";

			// Lưu token vào Session
			HttpContext.Session.SetString("JwtToken", loginData.Token);
			HttpContext.Session.SetString("UserRole", userRole);
			HttpContext.Session.SetString("UserInfo", JsonSerializer.Serialize(loginData.User));

			if (userRole == "Admin")
			{
				return RedirectToAction("Index", "Admin");
			}
			else if (userRole == "Manager")
			{
				return RedirectToAction("Products", "Manager");
			}
			else
			{
				return RedirectToAction("Index", "Products");
			}
		}

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
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
