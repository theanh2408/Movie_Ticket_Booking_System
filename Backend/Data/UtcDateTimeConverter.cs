using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend.Data;

/// <summary>
/// Mọi cột DateTime lưu theo UTC. Đọc lên gắn Kind = Utc để JSON trả về có chữ Z (frontend tự đổi sang giờ máy);
/// ghi xuống thì nếu nhận giờ Local (client gửi kèm múi giờ) sẽ đổi về UTC.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
