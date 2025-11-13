using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ClientSide.Controllers;

public class SupplierController : Controller
{
    private readonly string _urlBase = MyTools.getUrl();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Kiểm tra user có phải Supplier không
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

    private bool IsSupplier(UserDto? user) =>
        user != null && string.Equals(user.RoleName, "Supplier", StringComparison.OrdinalIgnoreCase);

    // GET: Supplier/Products - Danh sách sản phẩm của Supplier
    public async Task<IActionResult> Products()
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền truy cập trang này. Chỉ Supplier mới được phép.";
            return RedirectToAction("Index", "Products");
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            TempData["Error"] = "Bạn cần đăng nhập để truy cập.";
            return RedirectToAction("Login", "Account");
        }

        using var client = new HttpClient { BaseAddress = new Uri(_urlBase) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/Supplier/Products");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải danh sách sản phẩm.";
            return View(new List<Product>());
        }

        var content = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions) ?? new List<Product>();

        return View(products);
    }

    // GET: Supplier/Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền truy cập.";
            return RedirectToAction("Products");
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        using var client = new HttpClient { BaseAddress = new Uri(_urlBase) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/Supplier/Products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải thông tin sản phẩm.";
            return RedirectToAction("Products");
        }

        var content = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(content, _jsonOptions);

        return View(product);
    }

    // GET: Supplier/Products/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền tạo sản phẩm.";
            return RedirectToAction("Products");
        }

        await PopulateSelectLists();
        return View(new ProductCreateViewModel());
    }

    // POST: Supplier/Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền tạo sản phẩm.";
            return RedirectToAction("Products");
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
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
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
        var response = await client.PostAsync("/api/Supplier/Products", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Thêm sản phẩm mới thành công!";
            return RedirectToAction("Products");
        }

        string apiError = await response.Content.ReadAsStringAsync();
        TempData["Error"] = $"Không thể thêm sản phẩm. Chi tiết: {apiError}";
        await PopulateSelectLists();
        return View(model);
    }

    // GET: Supplier/Products/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền chỉnh sửa sản phẩm.";
            return RedirectToAction("Products");
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        using var client = new HttpClient { BaseAddress = new Uri(_urlBase) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"/api/Supplier/Products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải thông tin sản phẩm.";
            return RedirectToAction("Products");
        }

        var content = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<Product>(content, _jsonOptions);

        if (product == null)
        {
            return RedirectToAction("Products");
        }

        await PopulateSelectLists();
        var viewModel = new ProductCreateViewModel
        {
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            Description = product.Description,
            Image = product.Image,
            Price = product.Price,
            StockQuantity = product.StockQuantity ?? 0,
            IsActive = product.IsActive ?? true
        };

        return View(viewModel);
    }

    // POST: Supplier/Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductCreateViewModel model)
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền chỉnh sửa sản phẩm.";
            return RedirectToAction("Products");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            return View(model);
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        // Upload ảnh mới nếu có
        string? imageUrl = model.Image; // Giữ ảnh cũ nếu không upload mới
        if (model.ImageFile != null && model.ImageFile.Length > 0)
        {
            using var uploadClient = new HttpClient { BaseAddress = new Uri(_urlBase) };
            uploadClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();
            using var fileStream = model.ImageFile.OpenReadStream();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
            formData.Add(streamContent, "file", model.ImageFile.FileName);

            var uploadResponse = await uploadClient.PostAsync("/api/Upload/image", formData);
            if (uploadResponse.IsSuccessStatusCode)
            {
                var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
                var uploadData = JsonSerializer.Deserialize<UploadResponse>(uploadResult, _jsonOptions);
                imageUrl = uploadData?.Url;
            }
        }

        // Cập nhật sản phẩm
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
        var response = await client.PutAsync($"/api/Supplier/Products/{id}", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Products");
        }

        string apiError = await response.Content.ReadAsStringAsync();
        TempData["Error"] = $"Không thể cập nhật sản phẩm. Chi tiết: {apiError}";
        await PopulateSelectLists();
        return View(model);
    }

    // POST: Supplier/Products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsSupplier(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền xóa sản phẩm.";
            return RedirectToAction("Products");
        }

        string? token = HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        using var client = new HttpClient { BaseAddress = new Uri(_urlBase) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"/api/Supplier/Products/{id}");
        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Xóa sản phẩm thành công!";
        }
        else
        {
            var errorMsg = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Không thể xóa sản phẩm. {errorMsg}";
        }

        return RedirectToAction("Products");
    }

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

    private class UploadResponse
    {
        public string? Url { get; set; }
        public string? FileName { get; set; }
    }
}

