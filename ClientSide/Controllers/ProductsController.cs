using ClientSide.DataDtos;
using ClientSide.Models;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClientSide.Controllers;

public class ProductsController : Controller
{
    private readonly string _urlBase = MyTools.getUrl();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

        return View(product);
    }
}
