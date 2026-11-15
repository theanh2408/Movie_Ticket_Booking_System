using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Email không được để trống"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? CinemaId { get; set; }
}

public class CreateAccountDto
{
    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Email không được để trống"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public DateOnly? DOB { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string? Password { get; set; }

    /// <summary>Admin | Manager | Staff | Customer (bị bỏ qua ở API khách hàng / nhân viên)</summary>
    public string? Role { get; set; }

    /// <summary>Bắt buộc khi vai trò là Manager hoặc Staff</summary>
    public int? CinemaId { get; set; }
}

/// <summary>Dùng cho sửa tài khoản / khách hàng / nhân viên. Không cho sửa email.</summary>
public class UpdateAccountDto
{
    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    public DateOnly? DOB { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    /// <summary>Chỉ Admin mới đổi được rạp của quản lý/nhân viên</summary>
    public int? CinemaId { get; set; }
}

public class ChangeRoleDto
{
    [Required(ErrorMessage = "Vai trò không được để trống")]
    public string Role { get; set; } = string.Empty;

    public int? CinemaId { get; set; }
}

public class AccountResponseDto
{
    public int UserID { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? DOB { get; set; }
    public string? Gender { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int? CinemaID { get; set; }
    public string? CinemaName { get; set; }
}
