using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class OrdersController : Controller
    {
        private string urlBase = MyTools.getUrl();

        public async Task<IActionResult> Index()
        {
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để xem các đơn hàng!";
                return RedirectToAction("Index", "Products");
            }

            List<Order> orders = new List<Order>();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 4️⃣ Lấy thông tin user hiện tại
                HttpResponseMessage resUser = await client.GetAsync(urlBase + "/api/Auth/current-user");
                if (!resUser.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể lấy thông tin người dùng!";
                    return RedirectToAction("Index", "Products");
                }

                string userJson = await resUser.Content.ReadAsStringAsync();
                var currentUser = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                int customerId = currentUser?.UserId ?? 0;
                if (customerId == 0)
                {
                    TempData["Error"] = "Bạn cần đăng nhập để xem các đơn hàng!";
                    return RedirectToAction("Index", "Products");
                }

                using (HttpResponseMessage res = await client.GetAsync(urlBase + "/api/Orders/GetOrdersByCustomerId/" + customerId))
                {
                    using (HttpContent cont = res.Content)
                    {
                        string result = await cont.ReadAsStringAsync();
                        List<Order> od = JsonSerializer.Deserialize<List<Order>>(
                            result, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                        orders = od;
                    }
                }
            }

            ViewBag.orders = orders;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            using (HttpClient client = new HttpClient())
            {
                string json = JsonSerializer.Serialize(order);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage res = await client.PostAsync(urlBase + "/api/Orders", content);

                if (res.IsSuccessStatusCode)
                {
                    // Redirect to Index or Details after successful creation
                    return RedirectToAction("Index", new { customerId = order.CustomerId });
                }
                else
                {
                    // Optionally read the error message from response
                    string error = await res.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, "Error creating order: " + error);
                    return View(order);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFromProduct(int ProductId, int Quantity = 1)
        {
            // 1️⃣ Lấy token từ session
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để mua hàng!";
                return RedirectToAction("Index", "Products");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(urlBase);

                // 2️⃣ Gửi token qua header Authorization
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // 3️⃣ Lấy thông tin sản phẩm
                HttpResponseMessage resProduct = await client.GetAsync($"/api/Products/{ProductId}");
                if (!resProduct.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Sản phẩm không tồn tại!";
                    return RedirectToAction("Index", "Products");
                }

                string productJson = await resProduct.Content.ReadAsStringAsync();
                Product product = JsonSerializer.Deserialize<Product>(productJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (product == null || Quantity <= 0 || Quantity > product.StockQuantity)
                {
                    TempData["Error"] = "Số lượng không hợp lệ hoặc vượt quá tồn kho!";
                    return RedirectToAction("Index", "Products");
                }

                // 4️⃣ Lấy thông tin user hiện tại
                HttpResponseMessage resUser = await client.GetAsync("/api/Auth/current-user");
                if (!resUser.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Không thể lấy thông tin người dùng!";
                    return RedirectToAction("Index", "Products");
                }

                string userJson = await resUser.Content.ReadAsStringAsync();
                var currentUser = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                int customerId = currentUser?.UserId ?? 0;
                if (customerId == 0)
                {
                    TempData["Error"] = "Bạn cần đăng nhập để mua hàng!";
                    return RedirectToAction("Index", "Products");
                }

                // 5️⃣ Tạo order
                var orderRequest = new
                {
                    OrderCode = "ORD" + DateTime.Now.Ticks,
                    CustomerId = customerId,
                    OrderStatus = "Chờ xác nhận",
                    PaymentStatus = "Chưa thanh toán",
                    PaymentMethod = "COD",
                    Note = "Ghi chú đơn hàng",
                    ProcessedBy = (int?)null,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    OrderDetails = new[]
                    {
                new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = Quantity,
                    TotalPrice = product.Price * Quantity
                }
            }
                };

                string orderJson = JsonSerializer.Serialize(orderRequest);
                StringContent content = new StringContent(orderJson, Encoding.UTF8, "application/json");

                HttpResponseMessage resOrder = await client.PostAsync("/api/Orders", content);
                if (resOrder.IsSuccessStatusCode)
                    TempData["Success"] = "Đặt hàng thành công!";
                else
                {
                    var errorText = await resOrder.Content.ReadAsStringAsync();
                    TempData["Error"] = "Đặt hàng thất bại! " + errorText;
                }

                return RedirectToAction("Index", "Products");
            }
        }
    }
}