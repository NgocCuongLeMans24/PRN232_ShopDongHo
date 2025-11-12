using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServerSide.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServerSide.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class RolesController : ControllerBase
	{
		private readonly Prn232ClockShopContext _context;

		public RolesController(Prn232ClockShopContext context)
		{
			_context = context;
		}

		// GET: api/Roles
		[HttpGet]
		public async Task<IActionResult> GetRoles()
		{
			var roles = await _context.Roles
									.Select(r => new { r.RoleId, r.RoleName })
									.ToListAsync();
			return Ok(roles);
		}
	}
}