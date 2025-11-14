using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ClientSide.Controllers;

public class ProductsController : Controller
{
    private readonly string _urlBase = MyTools.getUrl().TrimEnd('/');
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private string NormalizeImageUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return string.Empty;
        }

        if (imagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return imagePath;
        }

        return $"{_urlBase}/{imagePath.TrimStart('/')}";
    }

    // GET: Products - Danh sách sản phẩm (cho Customer xem)
    public async Task<IActionResult> Index()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"{_urlBase}/api/Products/");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải danh sách sản phẩm. Vui lòng thử lại sau!";
            return View(new List<Product>());
        }

        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions) ?? new List<Product>();

        foreach (var product in products)
        {
            product.Image = NormalizeImageUrl(product.Image);
        }

        return View(products);
    }

    // GET: Products/ProductDetail/5 - Chi tiết sản phẩm (cho Customer xem)
    public async Task<IActionResult> ProductDetail(int id)
    {
        using var client = new HttpClient();
        var res = await client.GetAsync($"{_urlBase}/api/Products/{id}");
        if (!res.IsSuccessStatusCode)
        {
            TempData["Error"] = "Sản phẩm không tồn tại hoặc đã bị xoá.";
            return RedirectToAction(nameof(Index));
        }

        string json = await res.Content.ReadAsStringAsync();
        Product product = JsonSerializer.Deserialize<Product>(json, _jsonOptions) ?? new Product();
        product.Image = NormalizeImageUrl(product.Image);

        // Get reviews for this product
        try
        {
            var reviewsRes = await client.GetAsync($"{_urlBase}/api/Reviews/GetByProduct/{id}");
            if (reviewsRes.IsSuccessStatusCode)
            {
                var reviewsJson = await reviewsRes.Content.ReadAsStringAsync();
                var reviewsData = JsonSerializer.Deserialize<ProductReviewsDto>(reviewsJson, _jsonOptions);
                ViewBag.Reviews = reviewsData;
            }
        }
        catch
        {
            // If reviews fail, continue without reviews
            ViewBag.Reviews = null;
        }

        // Check if current user can review (has purchased this product)
        var currentUser = GetCurrentUser();
        ViewBag.IsLoggedIn = currentUser != null;
        ViewBag.CanReview = false; // Default to false
        
        if (currentUser != null)
        {
            string? token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                using var authClient = new HttpClient { BaseAddress = new Uri(_urlBase) };
                authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                
                try
                {
                    // Use GetPurchaseHistory instead - it returns flat data with ProductId
                    var historyRes = await authClient.GetAsync($"/api/Orders/GetPurchaseHistory/{currentUser.UserId}");
                    if (historyRes.IsSuccessStatusCode)
                    {
                        var historyJson = await historyRes.Content.ReadAsStringAsync();
                        var history = JsonSerializer.Deserialize<List<PurchaseHistoryItemDto>>(historyJson, _jsonOptions) ?? new List<PurchaseHistoryItemDto>();
                        
                        // Check if user has purchased this product
                        var matchingItems = history
                            .Where(h => h.ProductId == id)
                            .ToList();
                        
                        if (matchingItems.Any())
                        {
                            // More flexible status checking - case insensitive
                            // Accept any order status that contains "Xác Nhận" or "xác nhận"
                            var hasPurchased = matchingItems
                                .Any(h => 
                                {
                                    if (string.IsNullOrEmpty(h.OrderStatus))
                                        return false;
                                    
                                    var status = h.OrderStatus.Trim();
                                    return status.Contains("Xác Nhận", StringComparison.OrdinalIgnoreCase) ||
                                           status.Contains("xác nhận", StringComparison.OrdinalIgnoreCase) ||
                                           status.Equals("Đã Xác Nhận", StringComparison.OrdinalIgnoreCase) ||
                                           status.Equals("Chờ xác nhận", StringComparison.OrdinalIgnoreCase) ||
                                           status.Equals("Đã xác nhận", StringComparison.OrdinalIgnoreCase);
                                });
                            
                            // If no confirmed order found, check if user has ANY order for this product
                            // This is a fallback - if user came from History page, they should be able to review
                            if (!hasPurchased && matchingItems.Any())
                            {
                                // Allow review if user has any order for this product (more lenient)
                                hasPurchased = true;
                            }
                            
                            ViewBag.CanReview = hasPurchased;
                            
                            // Debug logging
                            var statuses = string.Join(", ", matchingItems.Select(h => $"'{h.OrderStatus ?? "null"}'"));
                            System.Diagnostics.Debug.WriteLine($"ProductDetail - UserId: {currentUser.UserId}, ProductId: {id}, HasPurchased: {hasPurchased}, HistoryCount: {history.Count}, MatchingItems: {matchingItems.Count}, Statuses: [{statuses}]");
                        }
                        else
                        {
                            ViewBag.CanReview = false;
                            System.Diagnostics.Debug.WriteLine($"ProductDetail - UserId: {currentUser.UserId}, ProductId: {id}, No matching items found in history");
                        }
                    }
                    else
                    {
                        // API failed but user is logged in
                        ViewBag.CanReview = false;
                        System.Diagnostics.Debug.WriteLine($"ProductDetail - GetPurchaseHistory failed: {historyRes.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    // Error checking orders but user is logged in
                    ViewBag.CanReview = false;
                    // Log error for debugging
                    System.Diagnostics.Debug.WriteLine($"ProductDetail - Error checking purchase history: {ex.Message}");
                }
            }
            else
            {
                // User info exists but no token
                ViewBag.CanReview = false;
                System.Diagnostics.Debug.WriteLine("ProductDetail - User logged in but no JWT token");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("ProductDetail - User not logged in");
        }

        return View(product);
    }

    private UserDto? GetCurrentUser()
    {
        try
        {
            var userJson = HttpContext.Session.GetString("UserInfo");
            if (string.IsNullOrEmpty(userJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<UserDto>(userJson, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
