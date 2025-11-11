using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;

namespace ServerSide.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;

    public CategoriesController(Prn232ClockShopContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .Where(c => c.IsActive == null || c.IsActive == true)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories);
    }
}

