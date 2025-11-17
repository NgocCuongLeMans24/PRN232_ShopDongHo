using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using ClientSide.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClientSide.Controllers;

public class ManagerController : Controller
{
    private readonly string _urlBase = MyTools.getUrl().TrimEnd('/');
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Kiểm tra user có phải Manager không
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

    private bool IsManager(UserDto? user) =>
        user != null && string.Equals(user.RoleName, "Manager", StringComparison.OrdinalIgnoreCase);

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

    // GET: Manager/Products - Danh sách tất cả sản phẩm (Manager quản lý tất cả)
    public async Task<IActionResult> Products()
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền truy cập trang này. Chỉ Manager mới được phép.";
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

        var response = await client.GetAsync("/api/Manager/Products");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải danh sách sản phẩm.";
            return View(new List<Product>());
        }

        var content = await response.Content.ReadAsStringAsync();
        
        // Deserialize trực tiếp vào Product model (JSON property names sẽ match)
        var products = JsonSerializer.Deserialize<List<Product>>(content, _jsonOptions) ?? new List<Product>();

        foreach (var product in products)
        {
            product.Image = NormalizeImageUrl(product.Image);
        }

        return View(products);
    }

    // GET: Manager/Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
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

        var response = await client.GetAsync($"/api/Manager/Products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải thông tin sản phẩm.";
            return RedirectToAction("Products");
        }

        var content = await response.Content.ReadAsStringAsync();
        
        // Deserialize từ ProductDto (API trả về)
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        if (root.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ.";
            return RedirectToAction("Products");
        }

        var product = new Product
        {
            ProductId = root.GetProperty("productId").GetInt32(),
            ProductCode = root.GetProperty("productCode").GetString() ?? "",
            ProductName = root.GetProperty("productName").GetString() ?? "",
            BrandId = root.GetProperty("brandId").GetInt32(),
            CategoryId = root.GetProperty("categoryId").GetInt32(),
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Image = root.TryGetProperty("image", out var img) ? img.GetString() : null,
            Price = root.GetProperty("price").GetDecimal(),
            StockQuantity = root.TryGetProperty("stockQuantity", out var sq) ? sq.GetInt32() : 0,
            SupplierId = root.TryGetProperty("supplierId", out var sid) && sid.ValueKind != JsonValueKind.Null ? sid.GetInt32() : null,
            IsActive = root.TryGetProperty("isActive", out var active) && active.ValueKind != JsonValueKind.Null ? active.GetBoolean() : true,
            CreatedAt = root.TryGetProperty("createdAt", out var ca) && ca.ValueKind != JsonValueKind.Null ? ca.GetDateTime() : null,
            UpdatedAt = root.TryGetProperty("updatedAt", out var ua) && ua.ValueKind != JsonValueKind.Null ? ua.GetDateTime() : null,
            Brand = root.TryGetProperty("brand", out var brand) && brand.ValueKind != JsonValueKind.Null ? new Brand
            {
                BrandId = brand.GetProperty("brandId").GetInt32(),
                BrandName = brand.GetProperty("brandName").GetString() ?? ""
            } : null!,
            Category = root.TryGetProperty("category", out var cat) && cat.ValueKind != JsonValueKind.Null ? new Category
            {
                CategoryId = cat.GetProperty("categoryId").GetInt32(),
                CategoryName = cat.GetProperty("categoryName").GetString() ?? ""
            } : null!,
            Supplier = root.TryGetProperty("supplier", out var sup) && sup.ValueKind != JsonValueKind.Null ? new Supplier
            {
                SupplierId = sup.GetProperty("supplierId").GetInt32(),
                SupplierName = sup.GetProperty("supplierName").GetString() ?? "",
                ContactPerson = sup.TryGetProperty("contactPerson", out var cp) ? cp.GetString() : null,
                Email = sup.TryGetProperty("email", out var email) ? email.GetString() : null,
                PhoneNumber = sup.TryGetProperty("phoneNumber", out var phone) ? phone.GetString() : null
            } : null
        };

        ViewBag.DisplayImageUrl = NormalizeImageUrl(product.Image);
        return View(product);
    }

    // GET: Manager/Products/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền tạo sản phẩm.";
            return RedirectToAction("Products");
        }

        await PopulateSelectLists();
        return View(new ProductCreateViewModel());
    }

    // POST: Manager/Products/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model)
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền tạo sản phẩm.";
            return RedirectToAction("Products");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            ViewBag.CurrentImageUrl = NormalizeImageUrl(model.Image);
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
                imageUrl = uploadData?.RelativeUrl ?? uploadData?.Url;
                model.Image = imageUrl;
            }
            else
            {
                var errorMsg = await uploadResponse.Content.ReadAsStringAsync();
                TempData["Error"] = $"Lỗi khi tải ảnh lên: {errorMsg}";
                await PopulateSelectLists();
                ViewBag.CurrentImageUrl = NormalizeImageUrl(model.Image);
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
            SupplierId = model.SupplierId, // Nullable, chỉ để hiển thị thông tin nhà cung cấp
            IsActive = model.IsActive
        };

        var requestPayload = JsonSerializer.Serialize(productData);
        var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/Manager/Products", content);

        if (response.IsSuccessStatusCode)
        {
            TempData["Success"] = "Thêm sản phẩm mới thành công!";
            return RedirectToAction("Products");
        }

        // Chỉ đọc error message nếu có content
        string apiError = "Lỗi không xác định";
        if (response.Content != null)
        {
            try
            {
                apiError = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                apiError = $"HTTP {(int)response.StatusCode}: {response.StatusCode}";
            }
        }
        else
        {
            apiError = $"HTTP {(int)response.StatusCode}: {response.StatusCode}";
        }
        
        TempData["Error"] = $"Không thể thêm sản phẩm. Chi tiết: {apiError}";
        await PopulateSelectLists();
        ViewBag.CurrentImageUrl = NormalizeImageUrl(model.Image);
        return View(model);
    }

    // GET: Manager/Products/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
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

        var response = await client.GetAsync($"/api/Manager/Products/{id}");
        if (!response.IsSuccessStatusCode)
        {
            TempData["Error"] = "Không thể tải thông tin sản phẩm.";
            return RedirectToAction("Products");
        }

        var content = await response.Content.ReadAsStringAsync();
        
        // Deserialize từ ProductDto (API trả về)
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        
        if (root.ValueKind != JsonValueKind.Object)
        {
            TempData["Error"] = "Dữ liệu sản phẩm không hợp lệ.";
            return RedirectToAction("Products");
        }

        await PopulateSelectLists();
        var imageUrl = root.TryGetProperty("image", out var img) ? img.GetString() : null;
        var viewModel = new ProductCreateViewModel
        {
            ProductId = id, // Lưu ID để dùng khi submit
            ProductCode = root.GetProperty("productCode").GetString() ?? "",
            ProductName = root.GetProperty("productName").GetString() ?? "",
            BrandId = root.GetProperty("brandId").GetInt32(),
            CategoryId = root.GetProperty("categoryId").GetInt32(),
            Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
            Image = imageUrl,
            Price = root.GetProperty("price").GetDecimal(),
            StockQuantity = root.GetProperty("stockQuantity").GetInt32(),
            SupplierId = root.TryGetProperty("supplierId", out var sid) && sid.ValueKind != JsonValueKind.Null ? sid.GetInt32() : null,
            IsActive = root.TryGetProperty("isActive", out var active) && active.ValueKind != JsonValueKind.Null ? active.GetBoolean() : true
        };

        ViewBag.CurrentImageUrl = NormalizeImageUrl(imageUrl);

        return View(viewModel);
    }

    // POST: Manager/Products/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductCreateViewModel model)
    {
        // Đảm bảo ID được lấy từ route hoặc model
        var productId = model.ProductId ?? id;
        
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
        {
            TempData["Error"] = "Bạn không có quyền chỉnh sửa sản phẩm.";
            return RedirectToAction("Products");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectLists();
            ViewBag.CurrentImageUrl = NormalizeImageUrl(model.Image);
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
                imageUrl = uploadData?.RelativeUrl ?? uploadData?.Url;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    model.Image = imageUrl;
                }
            }
            else
            {
                // Nếu upload ảnh thất bại, vẫn tiếp tục với ảnh cũ
                var errorMsg = await uploadResponse.Content.ReadAsStringAsync();
                TempData["Warning"] = $"Cảnh báo: Không thể upload ảnh mới. Sẽ giữ ảnh cũ. Chi tiết: {errorMsg}";
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
            SupplierId = model.SupplierId, // Nullable, chỉ để hiển thị thông tin nhà cung cấp
            IsActive = model.IsActive
        };

        var requestPayload = JsonSerializer.Serialize(productData);
        var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
        
        // Debug: Log request
        System.Diagnostics.Debug.WriteLine($"PUT Request to: /api/Manager/Products/{productId}");
        System.Diagnostics.Debug.WriteLine($"Payload: {requestPayload}");
        
        var response = await client.PutAsync($"/api/Manager/Products/{productId}", content);

        // Debug: Log response
        System.Diagnostics.Debug.WriteLine($"Response Status: {response.StatusCode}");
        System.Diagnostics.Debug.WriteLine($"IsSuccessStatusCode: {response.IsSuccessStatusCode}");

        if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            TempData["Success"] = "Cập nhật sản phẩm thành công!";
            return RedirectToAction("Products");
        }

        // Chỉ đọc error message nếu có content
        string apiError = "Lỗi không xác định";
        if (response.Content != null)
        {
            try
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error Content: {errorContent}");
                
                // Thử parse JSON error message
                try
                {
                    using var errorDoc = JsonDocument.Parse(errorContent);
                    if (errorDoc.RootElement.TryGetProperty("message", out var messageProp))
                    {
                        apiError = messageProp.GetString() ?? errorContent;
                    }
                    else
                    {
                        apiError = errorContent;
                    }
                }
                catch
                {
                    apiError = errorContent;
                }
            }
            catch (Exception ex)
            {
                apiError = $"HTTP {(int)response.StatusCode}: {response.StatusCode} - {ex.Message}";
            }
        }
        else
        {
            apiError = $"HTTP {(int)response.StatusCode}: {response.StatusCode}";
        }
        
        TempData["Error"] = $"Không thể cập nhật sản phẩm. Chi tiết: {apiError}";
        await PopulateSelectLists();
        ViewBag.CurrentImageUrl = NormalizeImageUrl(model.Image);
        return View(model);
    }

    // POST: Manager/Products/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = GetCurrentUser();
        if (!IsManager(currentUser))
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

        var response = await client.DeleteAsync($"/api/Manager/Products/{id}");
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
        var supplierResponse = await client.GetAsync($"{_urlBase}/api/Suppliers"); // API để lấy danh sách suppliers

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

        var suppliers = new List<Supplier>();
        if (supplierResponse.IsSuccessStatusCode)
        {
            var supplierJson = await supplierResponse.Content.ReadAsStringAsync();
            suppliers = JsonSerializer.Deserialize<List<Supplier>>(supplierJson, _jsonOptions) ?? new List<Supplier>();
        }

        ViewBag.BrandList = brands
            .Select(b => new SelectListItem { Value = b.BrandId.ToString(), Text = b.BrandName })
            .ToList();

        ViewBag.CategoryList = categories
            .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
            .ToList();

        ViewBag.SupplierList = suppliers
            .Select(s => new SelectListItem { Value = s.SupplierId.ToString(), Text = s.SupplierName })
            .ToList();
    }

    private class UploadResponse
    {
        public string? Url { get; set; }
        public string? RelativeUrl { get; set; }
        public string? FileName { get; set; }
    }
}

