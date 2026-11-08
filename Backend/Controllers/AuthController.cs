using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Dob { get; set; }
        public string? Gender { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Dob { get; set; }
        public string? Gender { get; set; }
    }

    /// <summary>
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse.Fail("Email và mật khẩu là bắt buộc."));
        }

        var normalizedEmail = request.Email.Trim().ToLower();
        var exists = await _context.Accounts.AnyAsync(a => a.Email != null && a.Email.ToLower() == normalizedEmail);
        if (exists)
        {
            return Conflict(ApiResponse.Fail("Email này đã được sử dụng bởi một tài khoản khác."));
        }

        DateOnly? dobParsed = null;
        if (!string.IsNullOrWhiteSpace(request.Dob) && DateOnly.TryParse(request.Dob, out var d))
        {
            dobParsed = d;
        }

        var account = new Account
        {
            FullName = request.FullName?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = normalizedEmail,
            Dob = dobParsed,
            Gender = request.Gender?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "CUSTOMER"
        };

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();

        var customer = new Customer
        {
            UserId = account.UserId
        };
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        var data = new
        {
            userId = account.UserId,
            fullName = account.FullName,
            phone = account.Phone,
            email = account.Email,
            dob = account.Dob?.ToString("yyyy-MM-dd"),
            gender = account.Gender,
            role = account.Role
        };

        return StatusCode(201, ApiResponse.Ok(data, "Đăng ký thành công"));
    }

    /// <summary>
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse.Fail("Vui lòng nhập email và mật khẩu."));
        }

        var normalizedEmail = request.Email.Trim().ToLower();
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email != null && a.Email.ToLower() == normalizedEmail);

        if (account == null || string.IsNullOrEmpty(account.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            return Unauthorized(ApiResponse.Fail("Email hoặc mật khẩu không chính xác."));
        }

        var secretKey = _configuration["JwtSettings:SecretKey"] ?? "Default_Secret_Key_Movie_Ticket_Booking_2026!";
        var issuer = _configuration["JwtSettings:Issuer"] ?? "MovieTicketBookingAPI";
        var audience = _configuration["JwtSettings:Audience"] ?? "MovieTicketBookingClient";
        var expirySeconds = int.TryParse(_configuration["JwtSettings:ExpirySeconds"], out var exp) ? exp : 3600;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, account.Email ?? ""),
            new Claim(ClaimTypes.Name, account.FullName ?? ""),
            new Claim(ClaimTypes.Role, account.Role ?? "CUSTOMER")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expirySeconds),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var data = new
        {
            accessToken = tokenString,
            tokenType = "Bearer",
            expiresIn = expirySeconds,
            user = new
            {
                userId = account.UserId,
                fullName = account.FullName,
                email = account.Email,
                phone = account.Phone,
                role = account.Role
            }
        };

        return Ok(ApiResponse.Ok(data, "Đăng nhập thành công"));
    }

    /// <summary>
    /// POST /api/auth/logout
    /// </summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(ApiResponse.Ok(null, "Đăng xuất thành công"));
    }

    /// <summary>
    /// GET /api/auth/me
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        // Trích xuất userId từ claim hoặc authorization header
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (int.TryParse(userIdClaim, out var userId))
        {
            var account = await _context.Accounts.FindAsync(userId);
            if (account != null)
            {
                var user = new
                {
                    userId = account.UserId,
                    fullName = account.FullName,
                    phone = account.Phone,
                    email = account.Email,
                    dob = account.Dob?.ToString("yyyy-MM-dd"),
                    gender = account.Gender,
                    role = account.Role
                };
                return Ok(ApiResponse.Ok(user, "Lấy thông tin tài khoản thành công"));
            }
        }

        return Unauthorized(ApiResponse.Fail("Chưa đăng nhập hoặc phiên làm việc đã hết hạn."));
    }

    /// <summary>
    /// PATCH /api/auth/me
    /// </summary>
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.Fail("Chưa đăng nhập."));
        }

        var account = await _context.Accounts.FindAsync(userId);
        if (account == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản."));
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
            account.FullName = request.FullName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Phone))
            account.Phone = request.Phone.Trim();

        if (!string.IsNullOrWhiteSpace(request.Gender))
            account.Gender = request.Gender.Trim();

        if (!string.IsNullOrWhiteSpace(request.Dob) && DateOnly.TryParse(request.Dob, out var d))
            account.Dob = d;

        await _context.SaveChangesAsync();

        var data = new
        {
            userId = account.UserId,
            fullName = account.FullName,
            phone = account.Phone,
            email = account.Email,
            dob = account.Dob?.ToString("yyyy-MM-dd"),
            gender = account.Gender,
            role = account.Role
        };

        return Ok(ApiResponse.Ok(data, "Cập nhật thông tin thành công"));
    }
}
