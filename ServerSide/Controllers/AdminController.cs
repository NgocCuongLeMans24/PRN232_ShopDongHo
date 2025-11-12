using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServerSide.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize(Roles = "Admin")]
	public class AdminController : ControllerBase
	{
		private readonly Prn232ClockShopContext _context;

		public AdminController(Prn232ClockShopContext context)
		{
			_context = context;
		}

		[HttpGet("GetUsersPaged")]
		public async Task<IActionResult> GetUsersPaged(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 5,
			[FromQuery] string searchTerm = "",
			[FromQuery] string roleFilter = "All",
			[FromQuery] string sortBy = "FullName",
			[FromQuery] string sortOrder = "asc")
		{
			// 1. Bắt đầu với IQueryable
			var query = _context.Users
								.Include(u => u.Role)
								.AsQueryable();

			// 2. Lọc (Filter)
			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(u => u.FullName.Contains(searchTerm) ||
										 u.Username.Contains(searchTerm));
			}

			if (roleFilter != "All")
			{
				query = query.Where(u => u.Role.RoleName == roleFilter);
			}

			// 3. Sắp xếp (Sort)
			if (sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase))
			{
				query = sortBy switch
				{
					"Username" => query.OrderBy(u => u.Username),
					_ => query.OrderBy(u => u.FullName)
				};
			}
			else
			{
				query = sortBy switch
				{
					"Username" => query.OrderByDescending(u => u.Username),
					_ => query.OrderByDescending(u => u.FullName)
				};
			}

			// 4. Lấy tổng số lượng
			var totalCount = await query.CountAsync();

			// 5. Phân trang
			var users = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// 6. Chuyển đổi sang DTO
			var userDtos = users.Select(u => new
			{
				u.UserId,
				u.Username,
				u.FullName,
				RoleName = u.Role?.RoleName,
				u.Email,
				u.PhoneNumber,
				u.Address,
				u.IsActive,
				u.CreatedAt
			}).ToList();

			// 7. Trả về đối tượng JSON phức tạp
			return Ok(new
			{
				Users = userDtos,
				TotalCount = totalCount,
				PageNumber = pageNumber,
				PageSize = pageSize,
				RoleFilter = roleFilter,
				SearchTerm = searchTerm,
				SortBy = sortBy,
				SortOrder = sortOrder
			});
		}
	}
}