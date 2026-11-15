using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class BookingListItemDto
{
    public int BookingID { get; set; }
    public int? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? CreatedDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
}

public class TicketDto
{
    public int TicketID { get; set; }
    public string? SeatCode { get; set; }
    public decimal? Price { get; set; }
    public string? Status { get; set; }
}
public class BookingDetailDto
{
    public int BookingID { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public int? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public string? MovieTitle { get; set; }
    public string? CinemaName { get; set; }
    public string? RoomName { get; set; }
    public DateOnly? ShowDate { get; set; }
    public TimeOnly? StartTime { get; set; }

    public List<TicketDto> Tickets { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public int? InvoiceID { get; set; }
}

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

public class PromotionUpsertDto
{
    [Required(ErrorMessage = "Mã khuyến mãi không được để trống"), StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên chương trình không được để trống"), StringLength(255)]
    public string PromotionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Percent (giảm theo %) | Fixed (giảm số tiền cố định)</summary>
    [Required(ErrorMessage = "Loại giảm giá không được để trống")]
    public string DiscountType { get; set; } = string.Empty;

    [Range(0.01, 1000000000, ErrorMessage = "Mức giảm phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000000)]
    public decimal? MinOrderAmount { get; set; }

    [Range(0, 1000000000)]
    public decimal? MaxDiscountAmount { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsagePerCustomer { get; set; }

    /// <summary>Active | Inactive (mặc định Active)</summary>
    public string? Status { get; set; }
}

public class PromotionResponseDto
{
    public int PromotionID { get; set; }
    public string? Code { get; set; }
    public string? PromotionName { get; set; }
    public string? Description { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int? UsagePerCustomer { get; set; }
    public string? Status { get; set; }
}

public class ApplyPromotionDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, 1000000000, ErrorMessage = "Giá trị đơn phải lớn hơn 0")]
    public decimal OrderAmount { get; set; }
}

public class ApplyPromotionResultDto
{
    public string? Code { get; set; }
    public string? PromotionName { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
