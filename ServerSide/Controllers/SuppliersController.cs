using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;

namespace ServerSide.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SuppliersController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;

    public SuppliersController(Prn232ClockShopContext context)
    {
        _context = context;
    }

    // GET: api/Suppliers - Lấy danh sách tất cả suppliers (để hiển thị trong dropdown)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.SupplierName)
            .ToListAsync();

        return Ok(suppliers);
    }

    // GET: api/Suppliers/5 - Lấy chi tiết supplier
    [HttpGet("{id}")]
    public async Task<ActionResult<Supplier>> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            return NotFound();
        }

        return supplier;
    }
}

