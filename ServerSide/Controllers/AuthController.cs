using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServerSide.DataDtos;
using ServerSide.Models;
using ServerSide.Utils;
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
    private readonly EmailService _emailService;

    public AuthController(Prn232ClockShopContext context, IConfiguration conf, EmailService emailService)
    {
        _context = context;
        _conf = conf;
        _emailService = emailService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserDto model)
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email || u.Username == model.Username);

        if (existingUser != null)
        {
            if (existingUser.IsActive == true)
            {
                return BadRequest(new { message = "Tên đăng nhập hoặc email đã tồn tại!" });
            }
            else
            {
                var newToken = Guid.NewGuid().ToString();
                existingUser.VerificationToken = newToken;
                existingUser.VerificationTokenExpire = DateTime.UtcNow.AddHours(24);
                await _context.SaveChangesAsync();

                var verifyUrl = $"{_conf["ClientAppUrl"]}/Account/Verify?token={newToken}";
                await _emailService.SendEmailAsync(existingUser.Email,
                    "Xác minh lại tài khoản Clock Shop",
                    $"<p>Xin chào {existingUser.FullName},</p>" +
                    $"<p>Hãy nhấn vào liên kết sau để kích hoạt tài khoản:</p>" +
                    $"<p><a href='{verifyUrl}'>{verifyUrl}</a></p>");

                return Ok(new { message = "Tài khoản chưa xác minh. Đã gửi lại email xác minh mới!" });
            }
        }

        // Nếu chưa tồn tại thì tạo mới
        var token = Guid.NewGuid().ToString();
        var newUser = new User
        {
            Username = model.Username,
            PasswordHash = model.PasswordHash,
            Email = model.Email,
            FullName = model.FullName,
            RoleId = 3,
            IsActive = false,
            VerificationToken = token,
            VerificationTokenExpire = DateTime.UtcNow.AddHours(24),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var verifyLink = $"{_conf["ClientAppUrl"]}/Account/Verify?token={token}";
        await _emailService.SendEmailAsync(model.Email,
            "Xác minh tài khoản Clock Shop",
            $"<p>Xin chào {model.FullName},</p><p>Hãy nhấn vào liên kết sau để kích hoạt tài khoản:</p><p><a href='{verifyLink}'>{verifyLink}</a></p>");

        return Ok(new { message = "Đăng ký thành công! Vui lòng kiểm tra email để xác minh tài khoản." });
    }

    // 🔹 Xác minh email
    [HttpGet("verify")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
        if (user == null || user.VerificationTokenExpire < DateTime.UtcNow)
            return BadRequest(new { message = "Mã xác minh không hợp lệ hoặc đã hết hạn." });

        user.IsActive = true;
        user.VerificationToken = null;
        user.VerificationTokenExpire = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Tài khoản của bạn đã được kích hoạt!" });
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
    public async Task<IActionResult> Login([FromBody] LoginDto userDto)
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
