namespace Backend.Common;

internal static class DateRange
{
    public static DateTime? Start(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue);

    /// <summary>Cận trên loại trừ: đầu ngày kế tiếp của ngày "đến".</summary>
    public static DateTime? EndExclusive(DateOnly? d) => d?.AddDays(1).ToDateTime(TimeOnly.MinValue);
}
