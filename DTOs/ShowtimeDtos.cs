using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateShowtimeDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phim")]
    public int MovieId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng chiếu")]
    public int RoomId { get; set; }

    [Required(ErrorMessage = "Ngày chiếu không được để trống")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
    public TimeOnly? StartHour { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Giá vé cơ bản phải lớn hơn 0")]
    public decimal BasePrice { get; set; }
}

public class UpdateShowtimeTimeDto
{
    [Required(ErrorMessage = "Ngày chiếu không được để trống")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
    public TimeOnly? StartHour { get; set; }
}

public class CancelShowtimeDto
{
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Đánh dấu hủy do sự cố không thể khắc phục (chỉ Admin được dùng khi còn dưới 60 phút)</summary>
    public bool IsIncident { get; set; }
}

public class ShowtimeResponseDto
{
    public int ShowtimeID { get; set; }
    public int? CinemaID { get; set; }
    public string? CinemaName { get; set; }
    public int? RoomID { get; set; }
    public string? RoomName { get; set; }
    public int? MovieID { get; set; }
    public string? MovieTitle { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public decimal? BasePrice { get; set; }
    public string? Status { get; set; }
    public int? BookedTickets { get; set; }   // chỉ có ở API chi tiết / sau khi sửa / hủy
}
