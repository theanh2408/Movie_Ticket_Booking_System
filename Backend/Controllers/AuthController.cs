using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MovieTicketBooking.Api.DTOs;
using MovieTicketBooking.Api.Services;
using System.Security.Claims;

namespace MovieTicketBooking.Api.Controllers;

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
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
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
        catch (SqlException ex)
            when (ex.Number is 2601 or 2627)
        {
            _logger.LogWarning(
                ex, "Duplicate account data during registration.");

            return Conflict(new
            {
                success = false,
                data = (object?)null,
                message = "Email or phone already exists"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed.");

            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "An unexpected error occurred"
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
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
                    message = "Invalid email or password"
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
                message = "Login successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed.");

            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "An unexpected error occurred"
            });
        }
    }


    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userId, out var parsedUserId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Invalid token"
            });
        }

        var profile = await _authService.GetProfileAsync(
            parsedUserId);

        if (profile == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "User not found"
            });
        }

        return Ok(new
        {
            success = true,
            data = profile,
            message = "User retrieved successfully"
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // JWT stateless: frontend cần xóa token khi đăng xuất.
        return Ok(new
        {
            success = true,
            data = (object?)null,
            message = "Logout successful"
        });
    }

    [Authorize]
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile(
    [FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Invalid user token"
            });
        }

        bool updated = await _authService.UpdateProfileAsync(userId, request);

        if (!updated)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "User not found"
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
            message = "Profile updated successfully"
        });
    }


    [Authorize]
    [HttpPatch("me/password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new
            {
                success = false,
                data = (object?)null,
                message = "Invalid user token"
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
                message = "User not found"
            });
        }

        if (!result.Value)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                message = "Current password is incorrect"
            });
        }

        return Ok(new
        {
            success = true,
            data = (object?)null,
            message = "Password changed successfully"
        });
    }
}