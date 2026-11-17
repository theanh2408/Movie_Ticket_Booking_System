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

public class CinemaDto
{
    public int CinemaID { get; set; }
    public string? CinemaName { get; set; }
    public string? Address { get; set; }
    public string? Status { get; set; }
}

public class RoomUpsertDto
{
    [Required(ErrorMessage = "Tên phòng không được để trống"), StringLength(100)]
    public string RoomName { get; set; } = string.Empty;

    /// <summary>Admin bắt buộc chọn rạp. Quản lý: tự lấy rạp của mình.</summary>
    public int? CinemaId { get; set; }

    /// <summary>Active | Inactive (mặc định Active)</summary>
    public string? Status { get; set; }
}

public class RoomResponseDto
{
    public int RoomID { get; set; }
    public string? RoomName { get; set; }
    public string? Status { get; set; }
    public int? CinemaID { get; set; }
    public string? CinemaName { get; set; }
    public int SeatCount { get; set; }   // sức chứa = số ghế của phòng
}

public class SeatCreateDto
{
    [Required(ErrorMessage = "Hàng ghế không được để trống"), StringLength(20)]
    public string Row { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số ghế không được để trống"), StringLength(20)]
    public string Column { get; set; } = string.Empty;
}

/// <summary>Tạo hàng loạt: RowCount hàng (A, B, C...) x SeatsPerRow ghế (1, 2, 3...)</summary>
public class SeatBulkCreateDto
{
    [Range(1, 26, ErrorMessage = "Số hàng từ 1 đến 26")]
    public int RowCount { get; set; }

    [Range(1, 50, ErrorMessage = "Số ghế mỗi hàng từ 1 đến 50")]
    public int SeatsPerRow { get; set; }
}

public class SeatResponseDto
{
    public int SeatID { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public int? RoomID { get; set; }
}

public class SeatMapItemDto
{
    public int SeatId { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    public string SeatCode { get; set; } = string.Empty;
    public int? ShowtimeSeatId { get; set; }   // chỉ có khi xem theo suất chiếu
    public string? Status { get; set; }        // chỉ có khi xem theo suất chiếu
    public decimal? Price { get; set; }
}

public class SeatMapRowDto
{
    public string Row { get; set; } = string.Empty;
    public List<SeatMapItemDto> Seats { get; set; } = new();
}

public class SeatMapDto
{
    public int RoomId { get; set; }
    public string? RoomName { get; set; }
    public int? ShowtimeId { get; set; }
    public List<SeatMapRowDto> Rows { get; set; } = new();
}
