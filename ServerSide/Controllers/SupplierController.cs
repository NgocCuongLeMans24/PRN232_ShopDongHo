using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.DataDtos;
using ServerSide.Models;
using System.Security.Claims;

namespace ServerSide.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SupplierController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;

    public SupplierController(Prn232ClockShopContext context)
    {
        _context = context;
    }

    private int GetCurrentSupplierId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Không thể xác định Supplier");
    }

    private async Task<bool> IsSupplier(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.Role?.RoleName == "Supplier";
    }

    [HttpGet("Products")]
    public async Task<ActionResult<IEnumerable<Product>>> GetMyProducts()
    {
        var supplierId = GetCurrentSupplierId();
        
        if (!await IsSupplier(supplierId))
        {
            return Forbid("Chỉ Supplier mới có quyền truy cập");
        }

        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.SupplierId == supplierId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("Products/{id}")]
    public async Task<ActionResult<Product>> GetMyProduct(int id)
    {
        var supplierId = GetCurrentSupplierId();
        
        if (!await IsSupplier(supplierId))
        {
            return Forbid("Chỉ Supplier mới có quyền truy cập");
        }

        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.SupplierId == supplierId);

        if (product == null)
        {
            return NotFound("Sản phẩm không tồn tại hoặc không thuộc quyền quản lý của bạn");
        }

        return product;
    }

    [HttpPost("Products")]
    public async Task<ActionResult<Product>> CreateProduct(ProductCreateDto productDto)
    {
        var supplierId = GetCurrentSupplierId();
        
        if (!await IsSupplier(supplierId))
        {
            return Forbid("Chỉ Supplier mới có quyền tạo sản phẩm");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var exists = await _context.Products
            .AnyAsync(p => p.ProductCode == productDto.ProductCode);
        if (exists)
        {
            return Conflict(new { message = "Mã sản phẩm đã tồn tại!" });
        }

        var product = new Product
        {
            ProductCode = productDto.ProductCode,
            ProductName = productDto.ProductName,
            BrandId = productDto.BrandId,
            CategoryId = productDto.CategoryId,
            Description = productDto.Description,
            Image = productDto.Image,
            Price = productDto.Price,
            StockQuantity = productDto.StockQuantity,
            IsActive = productDto.IsActive,
            SupplierId = supplierId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Brand).LoadAsync();
        await _context.Entry(product).Reference(p => p.Category).LoadAsync();

        return CreatedAtAction(nameof(GetMyProduct), new { id = product.ProductId }, product);
    }

    [HttpPut("Products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductCreateDto productDto)
    {
        var supplierId = GetCurrentSupplierId();
        
        if (!await IsSupplier(supplierId))
        {
            return Forbid("Chỉ Supplier mới có quyền cập nhật sản phẩm");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id && p.SupplierId == supplierId);

        if (product == null)
        {
            return NotFound("Sản phẩm không tồn tại hoặc không thuộc quyền quản lý của bạn");
        }

        var codeExists = await _context.Products
            .AnyAsync(p => p.ProductCode == productDto.ProductCode && p.ProductId != id);
        if (codeExists)
        {
            return Conflict(new { message = "Mã sản phẩm đã tồn tại!" });
        }

        product.ProductCode = productDto.ProductCode;
        product.ProductName = productDto.ProductName;
        product.BrandId = productDto.BrandId;
        product.CategoryId = productDto.CategoryId;
        product.Description = productDto.Description;
        product.Image = productDto.Image;
        product.Price = productDto.Price;
        product.StockQuantity = productDto.StockQuantity;
        product.IsActive = productDto.IsActive;
        product.UpdatedAt = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("Products/{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var supplierId = GetCurrentSupplierId();
        
        if (!await IsSupplier(supplierId))
        {
            return Forbid("Chỉ Supplier mới có quyền xóa sản phẩm");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id && p.SupplierId == supplierId);

        if (product == null)
        {
            return NotFound("Sản phẩm không tồn tại hoặc không thuộc quyền quản lý của bạn");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.ProductId == id);
    }
}

