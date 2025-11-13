using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using ServerSide.DataDtos;
using ServerSide.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerSide.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize(Roles = "Admin")] // <-- 2. Thêm attribute này để bảo vệ toàn bộ controller
	public class UsersController : ControllerBase
	{
		private readonly Prn232ClockShopContext _context;

		public UsersController(Prn232ClockShopContext context)
		{
			_context = context;
		}

		// GET: api/Users
		[HttpGet]
		public async Task<ActionResult<IEnumerable<User>>> GetUsers()
		{
			return await _context.Users.ToListAsync();
		}

		// GET: api/Users/5
		[HttpGet("{id}")]
		public async Task<ActionResult<User>> GetUser(int id)
		{
			var user = await _context.Users.FindAsync(id);

			if (user == null)
			{
				return NotFound();
			}

			return user;
		}

		// PUT: api/Users/5
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPut("{id}")]
		public async Task<IActionResult> PutUser(int id, UserEditDto userDto)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			// 4. Cập nhật các trường từ DTO
			user.Email = userDto.Email;
			user.FullName = userDto.FullName;
			user.PhoneNumber = userDto.PhoneNumber;
			user.Address = userDto.Address;
			user.RoleId = userDto.RoleId;
			user.IsActive = userDto.IsActive;
			user.UpdatedAt = DateTime.UtcNow;

			// 5. Xử lý mật khẩu (CHỈ CẬP NHẬT NẾU CÓ NHẬP)
			if (!string.IsNullOrEmpty(userDto.Password))
			{
				// Gán thẳng mật khẩu plain-text (theo logic của bạn)
				user.PasswordHash = userDto.Password;
			}

			_context.Entry(user).State = EntityState.Modified;

			try
			{
				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!_context.Users.Any(e => e.UserId == id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}
			return NoContent();
		}

		// POST: api/Users
		// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
		[HttpPost]
		public async Task<ActionResult<User>> PostUser(UserCreateDto userDto)
		{

			// 4. Chuyển đổi DTO sang Model "User"
			var user = new User
			{
				Username = userDto.Username,
				PasswordHash = userDto.Password,
				Email = userDto.Email,
				FullName = userDto.FullName,
				RoleId = userDto.RoleId,
				PhoneNumber = userDto.PhoneNumber,
				Address = userDto.Address,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			var userResponseDto = new
			{
				user.UserId,
				user.Username,
				user.FullName,
				user.RoleId
			};

			return CreatedAtAction("GetUser", new { id = user.UserId }, userResponseDto);
		}

		// DELETE: api/Users/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteUser(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}

			_context.Users.Remove(user);
			await _context.SaveChangesAsync();

			return NoContent();
		}

		private bool UserExists(int id)
		{
			return _context.Users.Any(e => e.UserId == id);
		}
	}
}
