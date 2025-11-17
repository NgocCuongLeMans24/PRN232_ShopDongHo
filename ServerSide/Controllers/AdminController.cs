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

			var query = _context.Users
								.Include(u => u.Role)
								.AsQueryable();

			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(u => u.FullName.Contains(searchTerm) ||
										 u.Username.Contains(searchTerm));
			}

			if (roleFilter != "All")
			{
				query = query.Where(u => u.Role != null && u.Role.RoleName == roleFilter);
			}

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

			var totalCount = await query.CountAsync();

			var users = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

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


		[HttpGet("GetOrdersPaged")]
		public async Task<IActionResult> GetOrdersPaged(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 10,
			[FromQuery] string searchTerm = "",
			[FromQuery] string statusFilter = "All")
		{

			var query = _context.Orders
								.Include(o => o.Customer)
								.AsQueryable();

			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(o => o.OrderCode.Contains(searchTerm) ||
										 (o.Customer != null && o.Customer.FullName.Contains(searchTerm)));
			}

			if (statusFilter != "All")
			{
				query = query.Where(o => o.OrderStatus == statusFilter);
			}

			query = query.OrderByDescending(o => o.CreatedAt);

			var totalCount = await query.CountAsync();

			var orders = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var orderDtos = orders.Select(o => new
			{
				o.OrderId,
				o.OrderCode,
				o.CustomerId,
				o.OrderStatus,
				o.PaymentStatus,
				o.PaymentMethod,
				o.Note,
				o.ProcessedBy,
				o.CreatedAt,
				o.UpdatedAt,
				CustomerName = o.Customer?.FullName,
				CustomerPhoneNumber = o.Customer?.PhoneNumber
			}).ToList();

			return Ok(new
			{
				Orders = orderDtos,
				TotalCount = totalCount,
				PageNumber = pageNumber,
				PageSize = pageSize,
				SearchTerm = searchTerm,
				CustomerPhoneNumber = (string)null,
				StatusFilter = statusFilter
			});
		}


		[HttpGet("GetProductsPaged")]
		public async Task<IActionResult> GetProductsPaged(
				[FromQuery] int pageNumber = 1,
				[FromQuery] int pageSize = 10,
				[FromQuery] string searchTerm = "",
				[FromQuery] int brandId = 0,     
				[FromQuery] int categoryId = 0)  
		{
			var query = _context.Products
								.Include(p => p.Brand)
								.Include(p => p.Category)
								.AsQueryable();

			if (!string.IsNullOrEmpty(searchTerm))
			{
				query = query.Where(p => p.ProductName.Contains(searchTerm) ||
										 p.ProductCode.Contains(searchTerm));
			}
			if (brandId > 0)
			{
				query = query.Where(p => p.BrandId == brandId);
			}

			if (categoryId > 0)
			{
				query = query.Where(p => p.CategoryId == categoryId);
			}

			query = query.OrderByDescending(p => p.CreatedAt);

			var totalCount = await query.CountAsync();

			var products = await query
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var productDtos = products.Select(p => new
			{
				p.ProductId,
				p.ProductCode,
				p.ProductName,
				p.Price,
				p.StockQuantity,
				p.IsActive,
				p.CreatedAt,
				BrandName = p.Brand.BrandName,
				CategoryName = p.Category.CategoryName
			}).ToList();

			return Ok(new
			{
				Products = productDtos,
				TotalCount = totalCount,
				PageNumber = pageNumber,
				PageSize = pageSize,
				SearchTerm = searchTerm,
				BrandId = brandId,           
				CategoryId = categoryId  
			});
		}
	}
}