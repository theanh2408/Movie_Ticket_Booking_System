namespace Backend.Common;

/// <summary>
/// QUY ƯỚC THỜI GIAN CỦA HỆ THỐNG (BUG-15) - dùng chung cho Admin và Customer:
///  - Mọi cột DateTime (CreatedDate, PaymentDate, CreateDate, ExpiredAt, StartDate/EndDate khuyến mãi, AppliedAt...)
///    lưu theo UTC, và API trả về dạng UTC có chữ Z.
///  - Riêng Showtime.Date + Showtime.Start_hour là GIỜ ĐỊA PHƯƠNG của rạp (Việt Nam, UTC+7).
/// Nên: so sánh với suất chiếu dùng LocalNow / LocalToday; còn lại dùng UtcNow.
/// KHÔNG dùng DateTime.Now / DateTime.Today ở bất kỳ đâu vì kết quả phụ thuộc múi giờ của máy chủ.
/// </summary>
public static class AppClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh" })   // Windows / Linux
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
    }

    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Giờ hiện tại theo giờ địa phương của rạp (UTC+7), dùng để so với Showtime.Date + Start_hour.</summary>
    public static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateOnly LocalToday => DateOnly.FromDateTime(LocalNow);

    public static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToUtc(DateTime local) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zone);
}
