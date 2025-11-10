using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerSide.DataDtos;
using ServerSide.Models;
using ServerSide.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly Prn232ClockShopContext _context;
    private readonly IConfiguration _conf;

    public AuthController(Prn232ClockShopContext context, IConfiguration conf)
    {
        _context = context;
        _conf = conf;
    }

    [HttpGet("current-user")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name;
        if (username == null) return Unauthorized();

        var user = _context.Users
            .Include(r => r.Role)
            .FirstOrDefault(u => u.Username == username);
        if (user == null) return NotFound();

        return Ok(new { user.UserId, user.Username, user.FullName, user.Role.RoleName });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserDto userDto)
    {
        var acc = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == userDto.Username);

        if (acc == null || acc.PasswordHash != userDto.PasswordHash)
        {
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
        }

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, acc.Username),
        new Claim(ClaimTypes.NameIdentifier, acc.UserId.ToString()),
        new Claim(ClaimTypes.Role, acc.Role.RoleName)
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_conf["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _conf["Jwt:Issuer"],
            audience: _conf["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(double.Parse(_conf["Jwt:ExpireInDays"])),
            signingCredentials: creds
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            expiration = token.ValidTo,
            user = new { acc.UserId, acc.Username, acc.FullName, acc.Role.RoleName }
        });
    }
}
