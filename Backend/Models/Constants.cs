namespace Backend.Models;

// Các giá trị chuỗi lưu trong cột Status/Role... Nếu nhóm thống nhất giá trị khác, chỉ cần sửa ở file này.
// Giá trị có chú thích "MỞ RỘNG" là giá trị Admin tự thêm, KHÔNG có trong tài liệu yêu cầu: cần Customer cùng hiểu và xử lý.

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
    public const string AdminOrManager = "Admin,Manager";
    public const string AdminManagerStaff = "Admin,Manager,Staff";
    public static readonly string[] All = { Admin, Manager, Staff, Customer };
}

public static class CinemaStatus
{
    public const string Opened = "Opened";
    public const string Closed = "Closed";
}

public static class AccountStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public static class MovieStatus
{
    public const string ComingSoon = "ComingSoon";
    public const string NowShowing = "NowShowing";
    public const string Ended = "Ended";
    public const string Inactive = "Inactive";   // MỞ RỘNG ngoài tài liệu: ngừng hiển thị (Customer cần ẩn phim này)
    public static readonly string[] All = { ComingSoon, NowShowing, Ended, Inactive };
}

// MỞ RỘNG: tài liệu chưa quy định trạng thái phòng chiếu; Admin dùng Active / Inactive.
public static class RoomStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public static class ShowtimeStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Cancelled = "Cancelled";
}

public static class ShowtimeSeatStatus
{
    public const string Available = "Available";
    public const string Held = "Held";
    public const string Booked = "Booked";
    public const string Disabled = "Disabled";   // MỞ RỘNG ngoài tài liệu: ghế bị khóa/hỏng, Customer không được chọn
}

public static class TicketStatus
{
    public const string Unused = "Unused";      // vé hợp lệ, chưa vào phòng chiếu
    public const string Used = "Used";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";    // quá giờ chiếu mà chưa dùng
}

public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";   // đã thanh toán thành công
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string RefundPending = "RefundPending";   // MỞ RỘNG: đã thanh toán nhưng suất chiếu bị hủy, chờ hoàn tiền
}

public static class PaymentStatus
{
    public const string Success = "Success";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";   // MỞ RỘNG ngoài tài liệu: khách chủ động hủy giao dịch tại cổng thanh toán
}

public static class InvoiceStatus
{
    public const string Issued = "Issued";      // MỞ RỘNG: trạng thái hóa đơn bình thường (tài liệu chỉ nêu Cancelled)
    public const string Cancelled = "Cancelled";
}

public static class PromotionStatus
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
}

public static class DiscountTypes
{
    public const string Percent = "Percent";
    public const string Fixed = "Fixed";
}
