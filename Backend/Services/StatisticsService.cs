using System.Text;
using Backend.Common;
using Backend.DTOs;
using Backend.Repositories;

namespace Backend.Services;

public interface IStatisticsService
{
    Task<RevenueStatsDto> RevenueAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<TicketStatsDto> TicketsSoldAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<BookingStatsDto> BookingsAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<OccupancyStatsDto> OccupancyAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<MovieRevenueStatsDto> RevenueByMovieAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<List<ShowtimePerformanceDto>> ShowtimePerformanceAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
    Task<(byte[] Content, string FileName)> ExportCsvAsync(string type, DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default);
}

/// <summary>
/// Admin: xem toàn chuỗi hoặc lọc theo rạp. Quản lý: tự động giới hạn trong rạp của mình.
/// Mặc định thống kê 30 ngày gần nhất.
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IStatisticsRepository _repo;
    private readonly ICurrentUser _current;

    public StatisticsService(IStatisticsRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<RevenueStatsDto> RevenueAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSuccessfulPaymentsAsync(r.CinemaId, r.FromDt, r.ToDt, ct: ct);

        return new RevenueStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalRevenue = rows.Sum(x => x.Amount),
            PaymentCount = rows.Count,
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(AppClock.ToLocal(x.Date!.Value)), x.Amount)))
        };
    }

    public async Task<TicketStatsDto> TicketsSoldAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSoldTicketsAsync(r.CinemaId, r.FromDt, r.ToDt, ct: ct);

        return new TicketStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalTickets = rows.Count,
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(AppClock.ToLocal(x.Date!.Value)), 0m))),
            ByMovie = rows.GroupBy(x => x.MovieTitle ?? "(Không xác định)")
                .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList()
        };
    }

    public async Task<BookingStatsDto> BookingsAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetBookingsAsync(r.CinemaId, r.FromDt, r.ToDt, ct: ct);

        return new BookingStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalBookings = rows.Count,
            ByStatus = rows.GroupBy(x => x.Status ?? "(Không xác định)")
                .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(AppClock.ToLocal(x.Date!.Value)), 0m)))
        };
    }

    public async Task<OccupancyStatsDto> OccupancyAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetOccupancyAsync(r.CinemaId, r.From, r.To, ct: ct);

        var total = rows.Sum(x => x.TotalSeats);
        var sold = rows.Sum(x => x.SoldSeats);

        return new OccupancyStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            ShowtimeCount = rows.Count,
            TotalSeats = total,
            SoldSeats = sold,
            OccupancyRate = total == 0 ? null : Math.Round(sold * 100.0 / total, 2),
            Note = total == 0 ? "Không đủ dữ liệu để tính tỷ lệ lấp đầy (không có suất chiếu hoặc không có ghế)" : null
        };
    }

    public async Task<MovieRevenueStatsDto> RevenueByMovieAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSuccessfulPaymentsByMovieAsync(r.CinemaId, r.FromDt, r.ToDt, ct: ct);

        var movies = rows
            .GroupBy(x => new { x.MovieId, Title = x.MovieTitle ?? "(Không xác định)" })
            .Select(g => new MovieRevenueItemDto
            {
                MovieId = g.Key.MovieId,
                MovieTitle = g.Key.Title,
                Revenue = g.Sum(x => x.Amount),
                PaymentCount = g.Count()
            })
            .OrderByDescending(x => x.Revenue).ToList();

        return new MovieRevenueStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalRevenue = movies.Sum(x => x.Revenue),
            Movies = movies
        };
    }

    public async Task<List<ShowtimePerformanceDto>> ShowtimePerformanceAsync(DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetShowtimePerformanceAsync(r.CinemaId, r.From, r.To, ct: ct);

        return rows.Select(x => new ShowtimePerformanceDto
        {
            ShowtimeID = x.ShowtimeId,
            MovieTitle = x.MovieTitle,
            CinemaName = x.CinemaName,
            RoomName = x.RoomName,
            Date = x.Date,
            StartTime = x.StartTime,
            TotalSeats = x.TotalSeats,
            TicketsSold = x.TicketsSold,
            Revenue = x.Revenue,
            OccupancyRate = x.TotalSeats == 0 ? null : Math.Round(x.TicketsSold * 100.0 / x.TotalSeats, 2)
        }).ToList();
    }

    /// <summary>
    /// Xuất báo cáo ra CSV (mở được bằng Excel, có BOM để hiển thị đúng tiếng Việt).
    /// type: revenue | tickets-sold | bookings | occupancy | revenue-by-movie | showtime-performance
    /// </summary>
    public async Task<(byte[] Content, string FileName)> ExportCsvAsync(string type, DateOnly? from, DateOnly? to, int? cinemaId, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var kind = (type ?? "").Trim().ToLowerInvariant();

        switch (kind)
        {
            case "revenue":
                var revenue = await RevenueAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Ngày", "Số giao dịch", "Doanh thu thực thu"));
                foreach (var d in revenue.Daily) sb.AppendLine(Csv(d.Date.ToString("yyyy-MM-dd"), d.Count, d.Amount));
                sb.AppendLine(Csv("TỔNG", revenue.PaymentCount, revenue.TotalRevenue));
                break;

            case "tickets-sold":
                var tickets = await TicketsSoldAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Ngày", "Số vé bán"));
                foreach (var d in tickets.Daily) sb.AppendLine(Csv(d.Date.ToString("yyyy-MM-dd"), d.Count));
                sb.AppendLine(Csv("TỔNG", tickets.TotalTickets));
                sb.AppendLine();
                sb.AppendLine(Csv("Phim", "Số vé bán"));
                foreach (var m in tickets.ByMovie) sb.AppendLine(Csv(m.Name, m.Count));
                break;

            case "bookings":
                var bookings = await BookingsAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Ngày", "Số lượt đặt vé"));
                foreach (var d in bookings.Daily) sb.AppendLine(Csv(d.Date.ToString("yyyy-MM-dd"), d.Count));
                sb.AppendLine(Csv("TỔNG", bookings.TotalBookings));
                sb.AppendLine();
                sb.AppendLine(Csv("Trạng thái", "Số lượt"));
                foreach (var s in bookings.ByStatus) sb.AppendLine(Csv(s.Name, s.Count));
                break;

            case "occupancy":
                var occupancy = await OccupancyAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Số suất chiếu", "Tổng ghế", "Ghế đã bán", "Tỷ lệ lấp đầy (%)"));
                sb.AppendLine(Csv(occupancy.ShowtimeCount, occupancy.TotalSeats, occupancy.SoldSeats, occupancy.OccupancyRate));
                break;

            case "revenue-by-movie":
                var byMovie = await RevenueByMovieAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Phim", "Số giao dịch", "Doanh thu thực thu"));
                foreach (var m in byMovie.Movies) sb.AppendLine(Csv(m.MovieTitle, m.PaymentCount, m.Revenue));
                sb.AppendLine(Csv("TỔNG", "", byMovie.TotalRevenue));
                break;

            case "showtime-performance":
                var perf = await ShowtimePerformanceAsync(from, to, cinemaId, ct: ct);
                sb.AppendLine(Csv("Mã suất", "Phim", "Rạp", "Phòng", "Ngày", "Giờ", "Tổng ghế", "Vé bán", "Doanh thu thực thu", "Tỷ lệ lấp đầy (%)"));
                foreach (var x in perf)
                    sb.AppendLine(Csv(x.ShowtimeID, x.MovieTitle, x.CinemaName, x.RoomName,
                                      x.Date?.ToString("yyyy-MM-dd"), x.StartTime?.ToString("HH:mm"),
                                      x.TotalSeats, x.TicketsSold, x.Revenue, x.OccupancyRate));
                break;

            default:
                throw new BadRequestException(
                    "type chỉ nhận: revenue, tickets-sold, bookings, occupancy, revenue-by-movie, showtime-performance");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return (bytes, $"{kind}_{AppClock.LocalNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string Csv(params object?[] cells) =>
        string.Join(",", cells.Select(c => CsvEscape(Convert.ToString(c, System.Globalization.CultureInfo.InvariantCulture) ?? "")));

    private static string CsvEscape(string value) =>
        value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;

    // ---------- helpers ----------
    private record Resolved(DateOnly From, DateOnly To, int? CinemaId, DateTime FromDt, DateTime ToDt);

    private Resolved Resolve(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var t = to ?? AppClock.LocalToday;
        var f = from ?? t.AddDays(-29);

        if (f > t) throw new BadRequestException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");
        if (t.DayNumber - f.DayNumber > 366) throw new BadRequestException("Khoảng thời gian thống kê tối đa 366 ngày");

        var scope = _current.ScopeCinemaId() ?? cinemaId;   // quản lý luôn bị giới hạn theo rạp của mình
        // Khoảng ngày địa phương -> khoảng UTC, vì Payment/Booking lưu thời gian theo UTC
        return new Resolved(f, t, scope,
            AppClock.ToUtc(f.ToDateTime(TimeOnly.MinValue)),
            AppClock.ToUtc(t.AddDays(1).ToDateTime(TimeOnly.MinValue)));
    }

    private static List<DailyStatDto> BuildDaily(DateOnly from, DateOnly to, IEnumerable<(DateOnly Date, decimal Amount)> items)
    {
        var map = items.GroupBy(i => i.Date)
                       .ToDictionary(g => g.Key, g => (Count: g.Count(), Amount: g.Sum(x => x.Amount)));

        var result = new List<DailyStatDto>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            map.TryGetValue(d, out var v);
            result.Add(new DailyStatDto { Date = d, Count = v.Count, Amount = v.Amount });
        }
        return result;
    }
}
