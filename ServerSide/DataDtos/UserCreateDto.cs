using System.ComponentModel.DataAnnotations;

namespace ServerSide.DataDtos
{
	public class UserCreateDto
	{
		[Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
		public string Username { get; set; }

		[Required(ErrorMessage = "Mật khẩu là bắt buộc")]
		[MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
		public string Password { get; set; }

		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress]
		public string Email { get; set; }

		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		public string FullName { get; set; }

		[Required(ErrorMessage = "Vai trò là bắt buộc")]
		public int RoleId { get; set; }

		// Các trường tùy chọn (optional)
		public string? PhoneNumber { get; set; }
		public string? Address { get; set; }
	}

	public class UserEditDto
	{
		[Required(ErrorMessage = "Email là bắt buộc")]
		[EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
		public string Email { get; set; }

		[Required(ErrorMessage = "Họ tên là bắt buộc")]
		public string FullName { get; set; }

		public string? PhoneNumber { get; set; }

		public string? Address { get; set; }

		[Required(ErrorMessage = "Vai trò là bắt buộc")]
		public int RoleId { get; set; }

		public bool? IsActive { get; set; }

		[MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
		public string? Password { get; set; }
	}
}
