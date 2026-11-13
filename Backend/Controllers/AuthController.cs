using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return Conflict(new
                {
                    success = false,
                    data = (object?)null,
                    message = result.Message
                });
            }

            return StatusCode(201, new
            {
                success = true,
                data = result.User,
                message = result.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đăng ký tài khoản thất bại.");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Đã xảy ra lỗi máy chủ trong quá trình đăng ký."
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);

            if (result is null)
            {
                return Unauthorized(new
                {
                    success = false,
                    data = (object?)null,
                    message = "Email hoặc mật khẩu không chính xác."
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    accessToken = result.Value.Token,
                    tokenType = "Bearer",
                    expiresIn = result.Value.ExpiresIn,
                    user = result.Value.User
                },
                message = "Đăng nhập thành công."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đăng nhập thất bại.");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Đã xảy ra lỗi máy chủ trong quá trình đăng nhập."
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Token không hợp lệ."
            });
        }

        var profile = await _authService.GetProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy người dùng."
            });
        }

        return Ok(new
        {
            success = true,
            data = profile,
            message = "Lấy thông tin người dùng thành công."
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new
        {
            success = true,
            data = (object?)null,
            message = "Đăng xuất thành công."
        });
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Token không hợp lệ."
            });
        }

        bool updated = await _authService.UpdateProfileAsync(userId, request);

        if (!updated)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy người dùng."
            });
        }

        return Ok(new
        {
            success = true,
            data = new
            {
                userId,
                fullName = request.FullName.Trim(),
                phone = request.Phone.Trim(),
                dob = request.Dob,
                gender = request.Gender
            },
            message = "Cập nhật hồ sơ thành công."
        });
    }

    [Authorize]
    [HttpPatch("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Token không hợp lệ."
            });
        }

        var result = await _authService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword);

        if (result == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy người dùng."
            });
        }

        if (!result.Value)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                message = "Mật khẩu hiện tại không chính xác."
            });
        }

        return Ok(new
        {
            success = true,
            data = (object?)null,
            message = "Đổi mật khẩu thành công."
        });
    }
}
