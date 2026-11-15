namespace Backend.DTOs;

public class DailyStatDto
{
    public DateOnly Date { get; set; }
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class NamedCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class StatsRangeDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int? CinemaId { get; set; }
}

public class RevenueStatsDto : StatsRangeDto
{
    public decimal TotalRevenue { get; set; }
    public int PaymentCount { get; set; }
    public List<DailyStatDto> Daily { get; set; } = new();
}

public class TicketStatsDto : StatsRangeDto
{
    public int TotalTickets { get; set; }
    public List<DailyStatDto> Daily { get; set; } = new();
    public List<NamedCountDto> ByMovie { get; set; } = new();
}

public class BookingStatsDto : StatsRangeDto
{
    public int TotalBookings { get; set; }
    public List<NamedCountDto> ByStatus { get; set; } = new();
    public List<DailyStatDto> Daily { get; set; } = new();
}

public class OccupancyStatsDto : StatsRangeDto
{
    public int ShowtimeCount { get; set; }
    public int TotalSeats { get; set; }
    public int SoldSeats { get; set; }
    public double? OccupancyRate { get; set; }   // %, null nếu không có ghế để tính
    public string? Note { get; set; }
}

public class MovieRevenueItemDto
{
    public int? MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int PaymentCount { get; set; }
}

public class MovieRevenueStatsDto : StatsRangeDto
{
    public decimal TotalRevenue { get; set; }
    public List<MovieRevenueItemDto> Movies { get; set; } = new();
}

public class ShowtimePerformanceDto
{
    public int ShowtimeID { get; set; }
    public string? MovieTitle { get; set; }
    public string? CinemaName { get; set; }
    public string? RoomName { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int TotalSeats { get; set; }
    public int TicketsSold { get; set; }
    public decimal Revenue { get; set; }
    public double? OccupancyRate { get; set; }
}
