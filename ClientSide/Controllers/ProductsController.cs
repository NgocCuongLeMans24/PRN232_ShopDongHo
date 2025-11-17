using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
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

        return View(product);
    }
}
