namespace Backend.Common;

/// <summary>Đổi khoảng "ngày địa phương" người dùng chọn thành khoảng UTC để lọc các cột DateTime (lưu UTC).</summary>
internal static class DateRange
{
    public static DateTime? Start(DateOnly? d) =>
        d.HasValue ? AppClock.ToUtc(d.Value.ToDateTime(TimeOnly.MinValue)) : null;

    /// <summary>Cận trên loại trừ: đầu ngày kế tiếp (giờ địa phương) đổi sang UTC.</summary>
    public static DateTime? EndExclusive(DateOnly? d) =>
        d.HasValue ? AppClock.ToUtc(d.Value.AddDays(1).ToDateTime(TimeOnly.MinValue)) : null;
}
