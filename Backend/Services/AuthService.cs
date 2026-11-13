using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message, object? User)> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        var existingAccount = await _context.Accounts
            .AnyAsync(a => a.Email != null && a.Email.ToLower() == email);

        if (existingAccount)
        {
            return (false, "Email đã được sử dụng.", null);
        }

        var existingPhone = await _context.Accounts
            .AnyAsync(a => a.Phone == phone);

        if (existingPhone)
        {
            return (false, "Số điện thoại đã được sử dụng.", null);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var account = new Account
        {
            FullName = request.FullName.Trim(),
            Phone = phone,
            Email = email,
            Dob = request.Dob.HasValue ? DateOnly.FromDateTime(request.Dob.Value) : null,
            Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
            PasswordHash = passwordHash,
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

        var user = new
        {
            userId = account.UserId,
            customerId = customer.CustomerId,
            fullName = account.FullName,
            phone = account.Phone,
            email = account.Email,
            dob = account.Dob.HasValue ? account.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            gender = account.Gender,
            role = account.Role
        };

        return (true, "Đăng ký tài khoản thành công.", user);
    }

    public async Task<(string Token, object User, int ExpiresIn)?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var account = await _context.Accounts
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.Email != null && a.Email.ToLower() == email);

        if (account == null || string.IsNullOrEmpty(account.PasswordHash))
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
        {
            return null;
        }

        // Đảm bảo account role Customer luôn có bản ghi Customer
        if (account.Role == "CUSTOMER" && account.Customer == null)
        {
            var customer = new Customer { UserId = account.UserId };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            account.Customer = customer;
        }

        var customerId = account.Customer?.CustomerId ?? 0;

        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expiresMinutes = int.Parse(_configuration["Jwt:ExpiresMinutes"] ?? "1440");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserId.ToString()),
            new(ClaimTypes.Name, account.FullName ?? ""),
            new(ClaimTypes.Email, account.Email ?? ""),
            new(ClaimTypes.Role, account.Role ?? "CUSTOMER"),
            new("CustomerId", customerId.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        var user = new
        {
            userId = account.UserId,
            customerId = customerId,
            fullName = account.FullName,
            email = account.Email,
            phone = account.Phone,
            role = account.Role
        };

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            user,
            expiresMinutes * 60
        );
    }

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (account == null) return false;

        account.FullName = request.FullName.Trim();
        account.Phone = request.Phone.Trim();
        account.Dob = request.Dob.HasValue ? DateOnly.FromDateTime(request.Dob.Value) : null;
        account.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool?> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (account == null) return null;

        if (string.IsNullOrWhiteSpace(account.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(currentPassword, account.PasswordHash))
        {
            return false;
        }

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object?> GetProfileAsync(int userId)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (account == null) return null;

        return new
        {
            userId = account.UserId,
            customerId = account.Customer?.CustomerId ?? 0,
            fullName = account.FullName,
            phone = account.Phone,
            email = account.Email,
            dob = account.Dob.HasValue ? account.Dob.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
            gender = account.Gender,
            role = account.Role
        };
    }
}
