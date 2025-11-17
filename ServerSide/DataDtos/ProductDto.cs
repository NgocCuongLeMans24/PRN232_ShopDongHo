namespace ServerSide.DataDtos;

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public int BrandId { get; set; }
    public int CategoryId { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int? SupplierId { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public BrandDto? Brand { get; set; }
    public CategoryDto? Category { get; set; }
    public SupplierDto? Supplier { get; set; }
}

public class BrandDto
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = null!;
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
}

public class SupplierDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

