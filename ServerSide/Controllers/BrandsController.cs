using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;

namespace ServerSide.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandsController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;

    public BrandsController(Prn232ClockShopContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        var brands = await _context.Brands
            .Where(b => b.IsActive == null || b.IsActive == true)
            .OrderBy(b => b.BrandName)
            .ToListAsync();

        return Ok(brands);
    }
}

