using ClientSide.Models;
using ClientSide.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClientSide.Controllers
{
    public class ProductsController : Controller
    {
        string urlBase = MyTools.getUrl();

        public async Task<IActionResult> Index()
        {
            HttpClient client = new HttpClient();
            List<Product> products = new List<Product>();
            using (HttpResponseMessage res = await client.GetAsync(urlBase + "/api/Products/"))
            {
                using (HttpContent cont = res.Content)
                {
                    string result = await cont.ReadAsStringAsync();
                    List<Product> pd = JsonSerializer.Deserialize<List<Product>>(
                        result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    products = pd;
                }
            }

            ViewBag.products = products;
            return View();
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            HttpClient client = new HttpClient();
            var res = await client.GetAsync(urlBase + "/api/Products/" + id);
            if (!res.IsSuccessStatusCode)
                return NotFound();

            string json = await res.Content.ReadAsStringAsync();
            Product product = JsonSerializer.Deserialize<Product>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new Product();

            return View(product);
        }
    }
}
