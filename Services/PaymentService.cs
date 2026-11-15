using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IPaymentService
{
    Task<List<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to);
    Task<PaymentDto> GetAsync(int id);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly ICurrentUser _current;

    public PaymentService(IPaymentRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<List<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var list = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to));
        return list.Select(ToDto).ToList();
    }

    public async Task<PaymentDto> GetAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id, _current.ScopeCinemaId())
                ?? throw new NotFoundException("Không tìm thấy giao dịch");
        return ToDto(p);
    }

    public static PaymentDto ToDto(Payment p) => new()
    {
        PaymentID = p.PaymentID,
        TransactionNo = p.TransactionNo,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod,
        PaymentStatus = p.PaymentStatus,
        PaymentDate = p.PaymentDate,
        BookingID = p.BookingID,
        InvoiceID = p.Booking?.Invoice?.InvoiceID
    };
}

public interface IInvoiceService
{
    Task<List<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to);
    Task<InvoiceDetailDto> GetAsync(int id);
    Task<InvoiceDetailDto> CancelAsync(int id);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly IBookingRepository _bookings;
    private readonly ICurrentUser _current;

    public InvoiceService(IInvoiceRepository repo, IBookingRepository bookings, ICurrentUser current)
    {
        _repo = repo;
        _bookings = bookings;
        _current = current;
    }

    public async Task<List<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var list = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to));
        return list.Select(i => new InvoiceListItemDto
        {
            InvoiceID = i.InvoiceID,
            CreateDate = i.CreateDate,
            TotalPrice = i.TotalPrice,
            Status = i.Status,
            BookingID = i.BookingID,
            CustomerName = i.Booking?.Customer?.Account?.FullName
        }).ToList();
    }

    public async Task<InvoiceDetailDto> GetAsync(int id)
    {
        var inv = await _repo.GetByIdAsync(id, _current.ScopeCinemaId())
                  ?? throw new NotFoundException("Không tìm thấy hóa đơn");
        return await BuildDetailAsync(inv);
    }

    /// <summary>Hủy hóa đơn = đổi trạng thái thành Cancelled (không xóa khỏi DB). Chỉ khi đơn đặt vé đã bị khách hủy.</summary>
    public async Task<InvoiceDetailDto> CancelAsync(int id)
    {
        var inv = await _repo.GetByIdAsync(id, null) ?? throw new NotFoundException("Không tìm thấy hóa đơn");

        if (inv.Status == InvoiceStatus.Cancelled)
            throw new ConflictException("Hóa đơn này đã được hủy trước đó");
        if (inv.Booking?.Status != BookingStatus.Cancelled)
            throw new ConflictException("Chỉ được hủy hóa đơn khi khách hàng đã hủy vé (đơn đặt vé ở trạng thái Cancelled)");

        inv.Status = InvoiceStatus.Cancelled;
        await _repo.SaveAsync();
        return await BuildDetailAsync(inv);
    }

    private async Task<InvoiceDetailDto> BuildDetailAsync(Invoice inv)
    {
        var booking = inv.BookingID.HasValue ? await _bookings.GetDetailAsync(inv.BookingID.Value, null) : null;
        return new InvoiceDetailDto
        {
            InvoiceID = inv.InvoiceID,
            CreateDate = inv.CreateDate,
            TotalPrice = inv.TotalPrice,
            Status = inv.Status,
            Booking = booking == null ? null : BookingService.ToDetail(booking)
        };
    }
}

public interface IStatisticsService
{
    Task<RevenueStatsDto> RevenueAsync(DateOnly? from, DateOnly? to, int? cinemaId);
    Task<TicketStatsDto> TicketsSoldAsync(DateOnly? from, DateOnly? to, int? cinemaId);
    Task<BookingStatsDto> BookingsAsync(DateOnly? from, DateOnly? to, int? cinemaId);
    Task<OccupancyStatsDto> OccupancyAsync(DateOnly? from, DateOnly? to, int? cinemaId);
    Task<MovieRevenueStatsDto> RevenueByMovieAsync(DateOnly? from, DateOnly? to, int? cinemaId);
    Task<List<ShowtimePerformanceDto>> ShowtimePerformanceAsync(DateOnly? from, DateOnly? to, int? cinemaId);
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

    public async Task<RevenueStatsDto> RevenueAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSuccessfulPaymentsAsync(r.CinemaId, r.FromDt, r.ToDt);

        return new RevenueStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalRevenue = rows.Sum(x => x.Amount),
            PaymentCount = rows.Count,
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(x.Date!.Value), x.Amount)))
        };
    }

    public async Task<TicketStatsDto> TicketsSoldAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSoldTicketsAsync(r.CinemaId, r.FromDt, r.ToDt);

        return new TicketStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalTickets = rows.Count,
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(x.Date!.Value), 0m))),
            ByMovie = rows.GroupBy(x => x.MovieTitle ?? "(Không xác định)")
                .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList()
        };
    }

    public async Task<BookingStatsDto> BookingsAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetBookingsAsync(r.CinemaId, r.FromDt, r.ToDt);

        return new BookingStatsDto
        {
            From = r.From, To = r.To, CinemaId = r.CinemaId,
            TotalBookings = rows.Count,
            ByStatus = rows.GroupBy(x => x.Status ?? "(Không xác định)")
                .Select(g => new NamedCountDto { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList(),
            Daily = BuildDaily(r.From, r.To, rows.Where(x => x.Date.HasValue)
                .Select(x => (DateOnly.FromDateTime(x.Date!.Value), 0m)))
        };
    }

    public async Task<OccupancyStatsDto> OccupancyAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetOccupancyAsync(r.CinemaId, r.From, r.To);

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

    public async Task<MovieRevenueStatsDto> RevenueByMovieAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetSuccessfulPaymentsByMovieAsync(r.CinemaId, r.FromDt, r.ToDt);

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

    public async Task<List<ShowtimePerformanceDto>> ShowtimePerformanceAsync(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var r = Resolve(from, to, cinemaId);
        var rows = await _repo.GetShowtimePerformanceAsync(r.CinemaId, r.From, r.To);

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

    // ---------- helpers ----------
    private record Resolved(DateOnly From, DateOnly To, int? CinemaId, DateTime FromDt, DateTime ToDt);

    private Resolved Resolve(DateOnly? from, DateOnly? to, int? cinemaId)
    {
        var t = to ?? DateOnly.FromDateTime(DateTime.Today);
        var f = from ?? t.AddDays(-29);

        if (f > t) throw new BadRequestException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc");
        if (t.DayNumber - f.DayNumber > 366) throw new BadRequestException("Khoảng thời gian thống kê tối đa 366 ngày");

        var scope = _current.ScopeCinemaId() ?? cinemaId;   // quản lý luôn bị giới hạn theo rạp của mình
        return new Resolved(f, t, scope, f.ToDateTime(TimeOnly.MinValue), t.AddDays(1).ToDateTime(TimeOnly.MinValue));
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
