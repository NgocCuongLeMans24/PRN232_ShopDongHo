using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
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

                // Lấy thông tin user hiện tại
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

                using HttpResponseMessage res = await client.GetAsync(urlBase + "/api/Orders/GetOrdersByCustomerId/" + customerId);
                if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    orders = new List<Order>();
                }
                else if (res.IsSuccessStatusCode)
                {
                    string result = await res.Content.ReadAsStringAsync();
                    orders = JsonSerializer.Deserialize<List<Order>>(
                        result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<Order>();
                }
                else
                {
                    TempData["Error"] = "Không thể tải danh sách đơn hàng.";
                    return RedirectToAction("Index", "Products");
                }
            }

            return View(orders);
        }

        public async Task<IActionResult> History()
        {
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để xem lịch sử mua hàng!";
                return RedirectToAction("Index", "Products");
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

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
                TempData["Error"] = "Bạn cần đăng nhập để xem lịch sử mua hàng!";
                return RedirectToAction("Index", "Products");
            }

            HttpResponseMessage resHistory = await client.GetAsync(urlBase + "/api/Orders/GetPurchaseHistory/" + customerId);
            if (!resHistory.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không thể tải lịch sử mua hàng.";
                return RedirectToAction(nameof(Index));
            }

            string historyJson = await resHistory.Content.ReadAsStringAsync();
            var history = JsonSerializer.Deserialize<List<PurchaseHistoryItemDto>>(historyJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<PurchaseHistoryItemDto>();

            // Get review status for all products in history
            if (history.Any() && customerId > 0)
            {
                try
                {
                    var productIds = string.Join(",", history.Select(h => h.ProductId).Distinct());
                    var reviewStatusRes = await client.GetAsync($"{urlBase}/api/Reviews/CheckReviewStatus?customerId={customerId}&productIds={productIds}");
                    if (reviewStatusRes.IsSuccessStatusCode)
                    {
                        var reviewStatusJson = await reviewStatusRes.Content.ReadAsStringAsync();
                        var reviewStatuses = JsonSerializer.Deserialize<List<ReviewStatusDto>>(reviewStatusJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<ReviewStatusDto>();

                        ViewBag.ReviewStatuses = reviewStatuses.ToDictionary(r => r.ProductId, r => r);
                    }
                }
                catch
                {
                    // If review check fails, continue without review status
                    ViewBag.ReviewStatuses = new Dictionary<int, ReviewStatusDto>();
                }
            }

            return View(history);
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
            // Lấy token từ session
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để mua hàng!";
                return RedirectToAction("Index", "Products");
            }

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(urlBase);

                // Gửi token qua header Authorization
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                // Lấy thông tin sản phẩm
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

                // Lấy thông tin user hiện tại
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

                // Tạo order
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

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để xem chi tiết đơn hàng!";
                return RedirectToAction("Index", "Products");
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            HttpResponseMessage res = await client.GetAsync(urlBase + "/api/Orders/" + id);
            if (!res.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            string orderJson = await res.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<Order>(orderJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            // Load product details for each order detail
            if (order.OrderDetails != null && order.OrderDetails.Any())
            {
                foreach (var detail in order.OrderDetails)
                {
                    try
                    {
                        var productRes = await client.GetAsync(urlBase + "/api/Products/" + detail.ProductId);
                        if (productRes.IsSuccessStatusCode)
                        {
                            string productJson = await productRes.Content.ReadAsStringAsync();
                            var product = JsonSerializer.Deserialize<Product>(productJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            detail.Product = product;
                        }
                    }
                    catch
                    {
                        // Skip if product not found
                    }
                }
            }

            return View(order);
        }

        // POST: Orders/UpdateQuantities
        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(int orderId, Dictionary<int, int> quantities)
        {
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập!";
                return RedirectToAction("Index");
            }

            if (quantities == null || !quantities.Any())
            {
                TempData["Error"] = "Vui lòng nhập số lượng!";
                return RedirectToAction("Details", new { id = orderId });
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Get order details
            HttpResponseMessage orderRes = await client.GetAsync(urlBase + "/api/Orders/" + orderId);
            if (!orderRes.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            string orderJson = await orderRes.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<Order>(orderJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null || order.OrderDetails == null)
            {
                TempData["Error"] = "Không tìm thấy chi tiết đơn hàng!";
                return RedirectToAction("Index");
            }

            // Update quantities
            bool hasError = false;
            string errorMessage = "";

            foreach (var detail in order.OrderDetails)
            {
                if (quantities.ContainsKey(detail.OrderDetailId))
                {
                    int newQuantity = quantities[detail.OrderDetailId];
                    
                    // Validate quantity
                    if (newQuantity <= 0)
                    {
                        hasError = true;
                        errorMessage = "Số lượng phải lớn hơn 0!";
                        break;
                    }

                    // Get product to check stock
                    var productRes = await client.GetAsync(urlBase + "/api/Products/" + detail.ProductId);
                    if (productRes.IsSuccessStatusCode)
                    {
                        string productJson = await productRes.Content.ReadAsStringAsync();
                        var product = JsonSerializer.Deserialize<Product>(productJson, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (product != null && newQuantity > product.StockQuantity)
                        {
                            hasError = true;
                            errorMessage = $"Số lượng sản phẩm '{detail.ProductName}' vượt quá tồn kho ({product.StockQuantity})!";
                            break;
                        }
                    }

                    // Update order detail
                    detail.Quantity = newQuantity;
                    detail.TotalPrice = detail.Price * newQuantity;

                    // Call API to update
                    var updateData = new
                    {
                        OrderDetailId = detail.OrderDetailId,
                        OrderId = detail.OrderId,
                        ProductId = detail.ProductId,
                        ProductName = detail.ProductName,
                        Price = detail.Price,
                        Quantity = detail.Quantity,
                        TotalPrice = detail.TotalPrice
                    };

                    string updateJson = JsonSerializer.Serialize(updateData);
                    StringContent content = new StringContent(updateJson, Encoding.UTF8, "application/json");

                    HttpResponseMessage updateRes = await client.PutAsync(urlBase + "/api/OrderDetails/" + detail.OrderDetailId, content);
                    if (!updateRes.IsSuccessStatusCode)
                    {
                        hasError = true;
                        var errorText = await updateRes.Content.ReadAsStringAsync();
                        errorMessage = $"Có lỗi xảy ra khi cập nhật số lượng! Chi tiết: {errorText}";
                        break;
                    }
                }
            }

            if (hasError)
            {
                TempData["Error"] = errorMessage;
            }
            else
            {
                TempData["Success"] = "Đã cập nhật số lượng thành công!";
            }

            return RedirectToAction("Details", new { id = orderId });
        }

        // POST: Orders/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            string token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập để hủy đơn hàng!";
                return RedirectToAction("Index");
            }

            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Kiểm tra đơn hàng trước khi hủy
            HttpResponseMessage orderRes = await client.GetAsync(urlBase + "/api/Orders/" + id);
            if (!orderRes.IsSuccessStatusCode)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            string orderJson = await orderRes.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<Order>(orderJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index");
            }

            // Kiểm tra điều kiện hủy đơn
            if (order.PaymentStatus == "Đã thanh toán")
            {
                TempData["Error"] = "Không thể hủy đơn hàng đã thanh toán!";
                return RedirectToAction("Index");
            }

            if (order.OrderStatus == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã được hủy trước đó!";
                return RedirectToAction("Index");
            }

            // Gọi API để hủy đơn
            HttpResponseMessage cancelRes = await client.PutAsync(urlBase + "/api/Orders/" + id + "/Cancel", null);
            if (!cancelRes.IsSuccessStatusCode)
            {
                var errorText = await cancelRes.Content.ReadAsStringAsync();
                TempData["Error"] = "Hủy đơn hàng thất bại! " + errorText;
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Đơn hàng đã được hủy thành công!";
            return RedirectToAction("Index");
        }
    }
}