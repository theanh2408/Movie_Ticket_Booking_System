namespace Backend.Models;

// Các giá trị chuỗi lưu trong cột Status/Role... Nếu nhóm thống nhất giá trị khác, chỉ cần sửa ở file này.

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
    public const string AdminOrManager = "Admin,Manager";
    public static readonly string[] All = { Admin, Manager, Staff, Customer };
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
    public const string Inactive = "Inactive";   // ngừng hiển thị
    public static readonly string[] All = { ComingSoon, NowShowing, Ended, Inactive };
}

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

public static class SeatStatus
{
    public const string Available = "Available";
    public const string Held = "Held";
    public const string Booked = "Booked";
    public const string Disabled = "Disabled";
}

public static class TicketStatus
{
    public const string Booked = "Booked";
    public const string Used = "Used";
    public const string Cancelled = "Cancelled";
}

public static class BookingStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";   // đã thanh toán thành công
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

public static class PayStatus
{
    public const string Success = "Success";
    public const string Pending = "Pending";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class InvoiceStatus
{
    public const string Issued = "Issued";
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
