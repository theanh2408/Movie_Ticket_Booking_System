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

    // MinimumLength = 9 để chặn chuỗi rỗng "" (RegularExpression bỏ qua chuỗi rỗng); null (không nhập) vẫn hợp lệ
    [StringLength(15, MinimumLength = 9, ErrorMessage = "Số điện thoại phải từ 9 đến 15 chữ số")]
    [RegularExpression(@"^\+?\d{9,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Email không được để trống"), EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(255)]
    public string Email { get; set; } = string.Empty;

    public DateOnly? DOB { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }

    [MinLength(6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string? Password { get; set; }

    /// <summary>Manager | Staff | Customer (không được tạo thêm Admin). Bị bỏ qua ở API khách hàng / nhân viên.</summary>
    public string? Role { get; set; }

    /// <summary>Bắt buộc khi vai trò là Manager hoặc Staff (khách hàng không gắn rạp)</summary>
    public int? CinemaId { get; set; }
}

/// <summary>Dùng cho sửa tài khoản / khách hàng / nhân viên. Không cho sửa email.</summary>
public class UpdateAccountDto
{
    [Required(ErrorMessage = "Họ tên không được để trống"), StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    // MinimumLength = 9 để chặn chuỗi rỗng "" (RegularExpression bỏ qua chuỗi rỗng); null (không nhập) vẫn hợp lệ
    [StringLength(15, MinimumLength = 9, ErrorMessage = "Số điện thoại phải từ 9 đến 15 chữ số")]
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

    /// <summary>Thời điểm theo UTC (ví dụ 2026-11-01T00:00:00Z)</summary>
    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime? StartDate { get; set; }

    /// <summary>Thời điểm theo UTC (ví dụ 2026-12-31T23:59:59Z)</summary>
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

    /// <summary>Khách hàng đang đặt vé. Bắt buộc khi mã có giới hạn số lần dùng mỗi khách (UsagePerCustomer).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Đơn đặt vé đang thao tác (nếu đã có): dùng để chặn kết hợp nhiều ưu đãi trên cùng một đơn.</summary>
    public int? BookingId { get; set; }
}

public class ApplyPromotionResultDto
{
    public int PromotionId { get; set; }
    public string? Code { get; set; }
    public string? PromotionName { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}

/// <summary>Đặt vé tại quầy (Staff): luồng giống Customer nhưng thanh toán ngay tại quầy.</summary>
public class CounterBookingDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn suất chiếu")]
    public int ShowtimeId { get; set; }

    /// <summary>Danh sách SeatID (lấy từ GET /api/showtimes/{id}/seat-map), tối đa 10 ghế</summary>
    [Required, MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất 1 ghế")]
    public List<int> SeatIds { get; set; } = new();

    /// <summary>Để trống = khách vãng lai</summary>
    public int? CustomerId { get; set; }

    public string? PromotionCode { get; set; }

    /// <summary>Mặc định Cash (tiền mặt)</summary>
    [StringLength(50)]
    public string? PaymentMethod { get; set; }
}

public class CounterBookingResultDto
{
    public BookingDetailDto Booking { get; set; } = new();
    public int InvoiceId { get; set; }
    public string QrToken { get; set; } = string.Empty;
}

public class TokenDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã QR / mã vé")]
    public string Token { get; set; } = string.Empty;
}

public class TicketCheckResultDto
{
    public bool IsValid { get; set; }
    public string? Reason { get; set; }
    public int? BookingID { get; set; }
    public string? MovieTitle { get; set; }
    public string? CinemaName { get; set; }
    public string? RoomName { get; set; }
    public DateOnly? ShowDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public List<TicketDto> Tickets { get; set; } = new();
}
