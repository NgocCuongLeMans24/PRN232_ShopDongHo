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
public class ManagerController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;

    public ManagerController(Prn232ClockShopContext context)
    {
        _context = context;
    }

    // Lấy UserID từ JWT token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Không thể xác định người dùng");
    }

    // Kiểm tra user có phải Manager không
    private async Task<bool> IsManager(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);
        return user?.Role?.RoleName == "Manager";
    }

    // GET: api/Manager/Products - Lấy tất cả sản phẩm (Manager quản lý tất cả)
    [HttpGet("Products")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var userId = GetCurrentUserId();
        
        if (!await IsManager(userId))
        {
            return Forbid("Chỉ Manager mới có quyền truy cập");
        }

        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // Xử lý nullable Supplier để tránh lỗi serialization
        var productDtos = products.Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            ProductCode = p.ProductCode,
            ProductName = p.ProductName,
            BrandId = p.BrandId,
            CategoryId = p.CategoryId,
            Description = p.Description,
            Image = p.Image,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            SupplierId = p.SupplierId,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Brand = p.Brand != null ? new BrandDto { BrandId = p.Brand.BrandId, BrandName = p.Brand.BrandName } : null,
            Category = p.Category != null ? new CategoryDto { CategoryId = p.Category.CategoryId, CategoryName = p.Category.CategoryName } : null,
            Supplier = p.Supplier != null ? new SupplierDto
            {
                SupplierId = p.Supplier.SupplierId,
                SupplierName = p.Supplier.SupplierName,
                ContactPerson = p.Supplier.ContactPerson,
                Email = p.Supplier.Email,
                PhoneNumber = p.Supplier.PhoneNumber
            } : null
        }).ToList();

        return Ok(productDtos);
    }

    // GET: api/Manager/Products/5 - Lấy chi tiết sản phẩm
    [HttpGet("Products/{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var userId = GetCurrentUserId();
        
        if (!await IsManager(userId))
        {
            return Forbid("Chỉ Manager mới có quyền truy cập");
        }

        var product = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound(new { message = "Sản phẩm không tồn tại" });
        }

        // Xử lý nullable Supplier để tránh lỗi serialization
        var productDto = new ProductDto
        {
            ProductId = product.ProductId,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            Description = product.Description,
            Image = product.Image,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            SupplierId = product.SupplierId,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Brand = product.Brand != null ? new BrandDto { BrandId = product.Brand.BrandId, BrandName = product.Brand.BrandName } : null,
            Category = product.Category != null ? new CategoryDto { CategoryId = product.Category.CategoryId, CategoryName = product.Category.CategoryName } : null,
            Supplier = product.Supplier != null ? new SupplierDto
            {
                SupplierId = product.Supplier.SupplierId,
                SupplierName = product.Supplier.SupplierName,
                ContactPerson = product.Supplier.ContactPerson,
                Email = product.Supplier.Email,
                PhoneNumber = product.Supplier.PhoneNumber
            } : null
        };

        return Ok(productDto);
    }

    // POST: api/Manager/Products - Tạo sản phẩm mới
    [HttpPost("Products")]
    public async Task<ActionResult<Product>> CreateProduct(ProductCreateDto productDto)
    {
        var userId = GetCurrentUserId();
        
        if (!await IsManager(userId))
        {
            return Forbid("Chỉ Manager mới có quyền tạo sản phẩm");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Kiểm tra mã sản phẩm trùng
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
            SupplierId = productDto.SupplierId, // Nullable, chỉ để hiển thị thông tin nhà cung cấp
            IsActive = productDto.IsActive,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _context.Entry(product).Reference(p => p.Brand).LoadAsync();
        await _context.Entry(product).Reference(p => p.Category).LoadAsync();
        if (product.SupplierId.HasValue)
        {
            await _context.Entry(product).Reference(p => p.Supplier).LoadAsync();
        }

        // Xử lý nullable Supplier để tránh lỗi serialization
        var responseDto = new ProductDto
        {
            ProductId = product.ProductId,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            BrandId = product.BrandId,
            CategoryId = product.CategoryId,
            Description = product.Description,
            Image = product.Image,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            SupplierId = product.SupplierId,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Brand = product.Brand != null ? new BrandDto { BrandId = product.Brand.BrandId, BrandName = product.Brand.BrandName } : null,
            Category = product.Category != null ? new CategoryDto { CategoryId = product.Category.CategoryId, CategoryName = product.Category.CategoryName } : null,
            Supplier = product.Supplier != null ? new SupplierDto
            {
                SupplierId = product.Supplier.SupplierId,
                SupplierName = product.Supplier.SupplierName,
                ContactPerson = product.Supplier.ContactPerson,
                Email = product.Supplier.Email,
                PhoneNumber = product.Supplier.PhoneNumber
            } : null
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, responseDto);
    }

    // PUT: api/Manager/Products/5 - Cập nhật sản phẩm
    [HttpPut("Products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, ProductCreateDto productDto)
    {
        var userId = GetCurrentUserId();
        
        if (!await IsManager(userId))
        {
            return Forbid("Chỉ Manager mới có quyền cập nhật sản phẩm");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound("Sản phẩm không tồn tại");
        }

        // Kiểm tra mã sản phẩm trùng (trừ sản phẩm hiện tại)
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
        product.SupplierId = productDto.SupplierId; // Nullable, chỉ để hiển thị thông tin nhà cung cấp
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

    // DELETE: api/Manager/Products/5 - Xóa sản phẩm
    [HttpDelete("Products/{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var userId = GetCurrentUserId();
        
        if (!await IsManager(userId))
        {
            return Forbid("Chỉ Manager mới có quyền xóa sản phẩm");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound("Sản phẩm không tồn tại");
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

