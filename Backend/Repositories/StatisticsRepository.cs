using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IStatisticsRepository
{
    Task<List<PaymentRow>> GetSuccessfulPaymentsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<PaymentRow>> GetSuccessfulPaymentsByMovieAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<SoldTicketRow>> GetSoldTicketsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<BookingRow>> GetBookingsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<OccupancyRow>> GetOccupancyAsync(int? cinemaId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<List<ShowtimePerfRow>> GetShowtimePerformanceAsync(int? cinemaId, DateOnly from, DateOnly to, CancellationToken ct = default);
}

public record PaymentRow(DateTime? Date, decimal Amount, int? MovieId, string? MovieTitle);

public record SoldTicketRow(DateTime? Date, int? MovieId, string? MovieTitle);

public record BookingRow(DateTime? Date, string? Status);

public record OccupancyRow(int ShowtimeId, int TotalSeats, int SoldSeats);

public record ShowtimePerfRow(int ShowtimeId, string? MovieTitle, string? CinemaName, string? RoomName,
                              DateOnly? Date, TimeOnly? StartTime, int TotalSeats, int TicketsSold, decimal Revenue);

public class StatisticsRepository : IStatisticsRepository
{
    private readonly ApplicationDbContext _db;

    public StatisticsRepository(ApplicationDbContext db) => _db = db;

    private IQueryable<Payment> SuccessfulPayments(int? cinemaId, DateTime from, DateTime to) =>
        _db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PaymentStatus.Success && p.PaymentDate >= from && p.PaymentDate < to)
            .InCinema(cinemaId);

    public async Task<List<PaymentRow>> GetSuccessfulPaymentsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await SuccessfulPayments(cinemaId, from, to)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync(ct);
        return rows.Select(r => new PaymentRow(r.PaymentDate, r.Amount ?? 0, null, null)).ToList();
    }

    public async Task<List<PaymentRow>> GetSuccessfulPaymentsByMovieAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // Một truy vấn JOIN Payment -> Ticket duy nhất (thay cho subquery tương quan chạy riêng cho từng giao dịch).
        var query =
            from p in _db.Payments.AsNoTracking()
            where p.PaymentStatus == PaymentStatus.Success && p.PaymentDate >= from && p.PaymentDate < to
            join t in _db.Tickets on p.BookingID equals t.BookingID
            select new
            {
                p.PaymentID,
                p.PaymentDate,
                p.Amount,
                CinemaId = t.ShowtimeSeat.Showtime.Room.CinemaID,
                MovieId = t.ShowtimeSeat.Showtime.MovieID,
                Title = t.ShowtimeSeat.Showtime.Movie.Title
            };

        if (cinemaId.HasValue) query = query.Where(x => x.CinemaId == cinemaId);

        var rows = await query.ToListAsync(ct);

        // Một đơn có nhiều vé nên mỗi giao dịch xuất hiện nhiều dòng: chỉ giữ 1 dòng cho mỗi giao dịch
        // (một đơn thuộc đúng 1 suất chiếu nên các dòng cùng phim).
        return rows
            .GroupBy(x => x.PaymentID)
            .Select(g => g.First())
            .Select(x => new PaymentRow(x.PaymentDate, x.Amount ?? 0, x.MovieId, x.Title))
            .ToList();
    }

    public async Task<List<SoldTicketRow>> GetSoldTicketsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        // BUG-09: điều kiện đếm vé THỐNG NHẤT cho TicketsSold / Occupancy / ShowtimePerformance:
        //   đơn đã thanh toán (Confirmed) VÀ suất chiếu chưa bị hủy -> không đếm vé Held/chưa thanh toán và suất đã hủy.
        var q = _db.Tickets.AsNoTracking()
            .Where(t => t.Booking.Status == BookingStatus.Confirmed
                        && t.ShowtimeSeat.Showtime.Status != ShowtimeStatus.Cancelled
                        && t.Booking.CreatedDate >= from && t.Booking.CreatedDate < to);

        if (cinemaId.HasValue) q = q.Where(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId);

        var rows = await q.Select(t => new
        {
            t.Booking.CreatedDate,
            MovieId = t.ShowtimeSeat.Showtime.MovieID,
            Title = t.ShowtimeSeat.Showtime.Movie.Title
        }).ToListAsync(ct);

        return rows.Select(r => new SoldTicketRow(r.CreatedDate, r.MovieId, r.Title)).ToList();
    }

    public async Task<List<BookingRow>> GetBookingsAsync(int? cinemaId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await _db.Bookings.AsNoTracking()
            .Where(b => b.CreatedDate >= from && b.CreatedDate < to)
            .InCinema(cinemaId)
            .Select(b => new { b.CreatedDate, b.Status })
            .ToListAsync(ct);
        return rows.Select(r => new BookingRow(r.CreatedDate, r.Status)).ToList();
    }

    // ---- Các suất chiếu trong khoảng ngày (giờ địa phương), chưa bị hủy ----
    private IQueryable<Showtime> ShowtimesIn(int? cinemaId, DateOnly from, DateOnly to)
    {
        var q = _db.Showtimes.AsNoTracking()
            .Where(s => s.Status != ShowtimeStatus.Cancelled && s.Date >= from && s.Date <= to);
        if (cinemaId.HasValue) q = q.Where(s => s.Room.CinemaID == cinemaId);
        return q;
    }

    /// <summary>Tổng số ghế dùng được (không tính ghế Disabled) của từng suất: 1 truy vấn GROUP BY cho tất cả suất.</summary>
    private async Task<Dictionary<int, int>> SeatTotalsAsync(IQueryable<Showtime> showtimes, CancellationToken ct = default)
    {
        var rows = await (
            from ss in _db.ShowtimeSeats.AsNoTracking()
            join s in showtimes on ss.ShowtimeID equals (int?)s.ShowtimeID
            where ss.Status != ShowtimeSeatStatus.Disabled
            group ss by s.ShowtimeID into g
            select new { Id = g.Key, Count = g.Count() }).ToListAsync(ct);
        return rows.ToDictionary(x => x.Id, x => x.Count);
    }

    /// <summary>
    /// Số vé đã bán của từng suất: 1 truy vấn GROUP BY. Điều kiện đếm vé THỐNG NHẤT (BUG-09):
    /// đơn đã thanh toán (Confirmed) và suất chưa bị hủy (đã loại ở ShowtimesIn).
    /// </summary>
    private async Task<Dictionary<int, int>> SoldCountsAsync(IQueryable<Showtime> showtimes, CancellationToken ct = default)
    {
        var rows = await (
            from t in _db.Tickets.AsNoTracking()
            join ss in _db.ShowtimeSeats on t.ShowtimeSeatID equals (int?)ss.ShowtimeSeatID
            join s in showtimes on ss.ShowtimeID equals (int?)s.ShowtimeID
            where t.Booking.Status == BookingStatus.Confirmed
            group t by s.ShowtimeID into g
            select new { Id = g.Key, Count = g.Count() }).ToListAsync(ct);
        return rows.ToDictionary(x => x.Id, x => x.Count);
    }

    public async Task<List<OccupancyRow>> GetOccupancyAsync(int? cinemaId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var showtimes = ShowtimesIn(cinemaId, from, to);

        // 3 truy vấn cố định (danh sách suất, tổng ghế, vé đã bán) bất kể có bao nhiêu suất chiếu
        var ids = await showtimes.Select(s => s.ShowtimeID).ToListAsync(ct);
        var totals = await SeatTotalsAsync(showtimes, ct: ct);
        var sold = await SoldCountsAsync(showtimes, ct: ct);

        return ids.Select(id => new OccupancyRow(id, totals.GetValueOrDefault(id), sold.GetValueOrDefault(id))).ToList();
    }

    public async Task<List<ShowtimePerfRow>> GetShowtimePerformanceAsync(int? cinemaId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var showtimes = ShowtimesIn(cinemaId, from, to);

        var info = await showtimes
            .OrderByDescending(s => s.Date).ThenByDescending(s => s.StartHour)
            .Select(s => new
            {
                s.ShowtimeID,
                MovieTitle = s.Movie.Title,
                CinemaName = s.Room.Cinema.CinemaName,
                RoomName = s.Room.RoomName,
                s.Date,
                s.StartHour
            })
            .ToListAsync(ct);

        var totals = await SeatTotalsAsync(showtimes, ct: ct);
        var sold = await SoldCountsAsync(showtimes, ct: ct);

        // Doanh thu THỰC THU = tổng Payment.Success của các đơn có vé ở suất này (Payment.Amount là số tiền khách
        // thực trả, đã trừ khuyến mãi). Một đơn có nhiều vé nên Distinct để mỗi giao dịch chỉ cộng 1 lần.
        var revenueRows = await (
            from t in _db.Tickets.AsNoTracking()
            join ss in _db.ShowtimeSeats on t.ShowtimeSeatID equals (int?)ss.ShowtimeSeatID
            join s in showtimes on ss.ShowtimeID equals (int?)s.ShowtimeID
            join p in _db.Payments on t.BookingID equals p.BookingID
            where p.PaymentStatus == PaymentStatus.Success
            select new { s.ShowtimeID, p.PaymentID, p.Amount }).Distinct().ToListAsync(ct);

        var revenue = revenueRows
            .GroupBy(x => x.ShowtimeID)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount ?? 0m));

        return info.Select(x => new ShowtimePerfRow(
            x.ShowtimeID, x.MovieTitle, x.CinemaName, x.RoomName, x.Date, x.StartHour,
            totals.GetValueOrDefault(x.ShowtimeID),
            sold.GetValueOrDefault(x.ShowtimeID),
            revenue.GetValueOrDefault(x.ShowtimeID))).ToList();
    }
}
