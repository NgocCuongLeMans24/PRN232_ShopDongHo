using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientSide.Controllers;

public class ProductsController : Controller
{
    private readonly string _urlBase = MyTools.getUrl();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        var currentUser = GetCurrentUser();
        ViewBag.CurrentUser = currentUser;
        ViewBag.CanManageProducts = UserCanManageProducts(currentUser);

        return View(products);
    }

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

        return View(product);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var currentUser = GetCurrentUser();
        if (!UserCanManageProducts(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền thêm sản phẩm mới.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateSelectLists();
        return View(new ProductCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        var currentUser = GetCurrentUser();
        if (!UserCanManageProducts(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền thêm sản phẩm mới.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Bạn cần đăng nhập để thực hiện thao tác này.";
            return RedirectToAction("Login", "Account");
        }

        // Upload ảnh nếu có
        string? imageUrl = null;
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            using var uploadClient = new HttpClient { BaseAddress = new Uri(_urlBase) };
            uploadClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();
            using var fileStream = model.ImageFile.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(model.ImageFile.ContentType);
            formData.Add(streamContent, "file", model.ImageFile.FileName);

            var uploadResponse = await uploadClient.PostAsync("/api/Upload/image", formData);
            if (uploadResponse.IsSuccessStatusCode)
            {
                var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
                var uploadData = JsonSerializer.Deserialize<UploadResponse>(uploadResult, _jsonOptions);
                imageUrl = uploadData?.Url;
            }
            else
            {
                var errorMsg = await uploadResponse.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi khi tải ảnh lên: {errorMsg}";
                await PopulateSelectLists();
                return View(model);
            }
        }

        // Tạo sản phẩm
        using var client = new HttpClient { BaseAddress = new Uri(_urlBase) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productData = new
        {
            ProductCode = model.ProductCode,
            ProductName = model.ProductName,
            BrandId = model.BrandId,
            CategoryId = model.CategoryId,
            Description = model.Description,
            Image = imageUrl,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            IsActive = model.IsActive
        };

        var requestPayload = JsonSerializer.Serialize(productData);
        var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Products", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Thêm sản phẩm mới thành công!";
            return RedirectToAction(nameof(Index));
        }

        string apiError = await response.Content.ReadAsStringAsync();
        TempData["Error"] = $"Không thể thêm sản phẩm. Chi tiết: {apiError}";
        await PopulateSelectLists();
        return View(model);
    }

    private class UploadResponse
    {
        public string? Url { get; set; }
        public string? FileName { get; set; }
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

    private static bool UserCanManageProducts(UserDto? user) =>
        user != null && (string.Equals(user.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.RoleName, "Supplier", StringComparison.OrdinalIgnoreCase));

    private async Task PopulateSelectLists()
    {
        using var client = new HttpClient();

        var brandResponse = await client.GetAsync($"{_urlBase}/api/Brands");
        var categoryResponse = await client.GetAsync($"{_urlBase}/api/Categories");

        var brands = new List<Brand>();
        if (brandResponse.IsSuccessStatusCode)
        {
            var brandJson = await brandResponse.Content.ReadAsStringAsync();
            brands = JsonSerializer.Deserialize<List<Brand>>(brandJson, _jsonOptions) ?? new List<Brand>();
        }

        var categories = new List<Category>();
        if (categoryResponse.IsSuccessStatusCode)
        {
            var categoryJson = await categoryResponse.Content.ReadAsStringAsync();
            categories = JsonSerializer.Deserialize<List<Category>>(categoryJson, _jsonOptions) ?? new List<Category>();
        }

        ViewBag.BrandList = brands
            .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.BrandName })
            .ToList();

        ViewBag.CategoryList = categories
            .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
            .ToList();
    }
}
