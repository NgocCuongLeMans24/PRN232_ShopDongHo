using System.ComponentModel.DataAnnotations;

namespace ClientSide.ViewModels;

public class ProductCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm")]
    [MaxLength(50)]
    public string ProductCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    public int CategoryId { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Hình ảnh sản phẩm")]
    public IFormFile? ImageFile { get; set; }

    public string? Image { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn phải lớn hơn hoặc bằng 0")]
    public int StockQuantity { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;
}

