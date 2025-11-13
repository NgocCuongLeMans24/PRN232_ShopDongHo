using ClientSide.DataDtos;
using System.ComponentModel.DataAnnotations;

namespace ClientSide.ViewModels
{
	public class UserListViewModel
	{
		// Danh sách người dùng cho trang hiện tại
		public List<UserDto> Users { get; set; } = new List<UserDto>();

		// Thông tin phân trang
		public int TotalCount { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 5;

		// Thông tin lọc và tìm kiếm
		public string SearchTerm { get; set; } = string.Empty;
		public string RoleFilter { get; set; } = "All"; // All, Admin, Supplier, Customer

		// Thông tin sắp xếp
		public string SortBy { get; set; } = "FullName";
		public string SortOrder { get; set; } = "asc";

		// Thuộc tính hỗ trợ (tính toán)
		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
		public bool HasPreviousPage => PageNumber > 1;
		public bool HasNextPage => PageNumber < TotalPages;
	}

	public class CreateUserViewModel
	{
		[Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
		[Display(Name = "Tên đăng nhập")]
		public string Username { get; set; }

		[Required(ErrorMessage = "Mật khẩu là bắt buộc")]
		[DataType(DataType.Password)]
		[Display(Name = "Mật khẩu")]
		public string Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Xác nhận mật khẩu")]
		[Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
		public string ConfirmPassword { get; set; }

		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
		[Display(Name = "Email")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		[Display(Name = "Họ và tên")]
		public string FullName { get; set; }

		[Display(Name = "Số điện thoại")]
		public string? PhoneNumber { get; set; }

		[Display(Name = "Địa chỉ")]
		public string? Address { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn vai trò")]
		[Display(Name = "Vai trò")]
		public int RoleId { get; set; }

		[Display(Name = "Active")]
		public bool IsActive { get; set; } = true;
	}

	public class EditUserViewModel
	{
		public int UserId { get; set; }

		[Required]
		[Display(Name = "Tên đăng nhập (Không thể đổi)")]
		public string Username { get; set; }

		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress]
		public string Email { get; set; }

		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		[Display(Name = "Họ và tên")]
		public string FullName { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Mật khẩu mới (Để trống nếu không đổi)")]
		public string? Password { get; set; }

		[DataType(DataType.Password)]
		[Display(Name = "Xác nhận mật khẩu")]
		[Compare("Password", ErrorMessage = "Mật khẩu và mật khẩu xác nhận không khớp.")]
		public string? ConfirmPassword { get; set; }

		[Display(Name = "Số điện thoại")]
		public string? PhoneNumber { get; set; }

		[Display(Name = "Địa chỉ")]
		public string? Address { get; set; }

		[Required(ErrorMessage = "Vui lòng chọn vai trò")]
		[Display(Name = "Vai trò")]
		public int RoleId { get; set; }

		[Display(Name = "Active")]
		public bool IsActive { get; set; }
	}
}
