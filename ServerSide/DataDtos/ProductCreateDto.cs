using System.ComponentModel.DataAnnotations;

namespace ServerSide.DataDtos;

public class ProductCreateDto
{
    [Required]
    [MaxLength(50)]
    public string ProductCode { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = null!;

    [Required]
    public int BrandId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public string? Description { get; set; }

    [Url]
    public string? Image { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
    public int StockQuantity { get; set; }

    public bool? IsActive { get; set; } = true;
}

