using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to);
    Task<Payment?> GetByIdAsync(int id, int? cinemaId);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _db;

    public PaymentRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Payment>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to)
    {
        var q = _db.Payments.AsNoTracking()
            .Include(p => p.Booking).ThenInclude(b => b.Invoice)
            .InCinema(cinemaId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            int? idMatch = int.TryParse(k, out var n) ? n : null;
            q = q.Where(p => p.TransactionNo!.Contains(k) || p.BookingID == idMatch || p.PaymentID == idMatch);
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.PaymentStatus == status);
        if (from.HasValue) q = q.Where(p => p.PaymentDate >= from);
        if (to.HasValue)   q = q.Where(p => p.PaymentDate < to);

        return q.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }

    public Task<Payment?> GetByIdAsync(int id, int? cinemaId) =>
        _db.Payments.AsNoTracking()
            .Include(p => p.Booking).ThenInclude(b => b.Invoice)
            .Where(p => p.PaymentID == id)
            .InCinema(cinemaId)
            .FirstOrDefaultAsync();
}

public interface IInvoiceRepository
{
    Task<List<Invoice>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to);
    Task<Invoice?> GetByIdAsync(int id, int? cinemaId);   // có tracking, kèm Booking
    Task SaveAsync();
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _db;

    public InvoiceRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Invoice>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Booking).ThenInclude(b => b.Customer).ThenInclude(c => c.Account)
            .InCinema(cinemaId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            int? idMatch = int.TryParse(k, out var n) ? n : null;
            q = q.Where(i => i.InvoiceID == idMatch || i.BookingID == idMatch);
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(i => i.Status == status);
        if (from.HasValue) q = q.Where(i => i.CreateDate >= from);
        if (to.HasValue)   q = q.Where(i => i.CreateDate < to);

        return q.OrderByDescending(i => i.InvoiceID).ToListAsync();
    }

    public Task<Invoice?> GetByIdAsync(int id, int? cinemaId) =>
        _db.Invoices
            .Include(i => i.Booking)
            .Where(i => i.InvoiceID == id)
            .InCinema(cinemaId)
            .FirstOrDefaultAsync();

    public Task SaveAsync() => _db.SaveChangesAsync();
}

public record PaymentRow(DateTime? Date, decimal Amount, int? MovieId, string? MovieTitle);
public record SoldTicketRow(DateTime? Date, int? MovieId, string? MovieTitle);
public record BookingRow(DateTime? Date, string? Status);
public record OccupancyRow(int ShowtimeId, int TotalSeats, int SoldSeats);
public record ShowtimePerfRow(int ShowtimeId, string? MovieTitle, string? CinemaName, string? RoomName,
                              DateOnly? Date, TimeOnly? StartTime, int TotalSeats, int TicketsSold, decimal Revenue);

public interface IStatisticsRepository
{
    Task<List<PaymentRow>> GetSuccessfulPaymentsAsync(int? cinemaId, DateTime from, DateTime to);
    Task<List<PaymentRow>> GetSuccessfulPaymentsByMovieAsync(int? cinemaId, DateTime from, DateTime to);
    Task<List<SoldTicketRow>> GetSoldTicketsAsync(int? cinemaId, DateTime from, DateTime to);
    Task<List<BookingRow>> GetBookingsAsync(int? cinemaId, DateTime from, DateTime to);
    Task<List<OccupancyRow>> GetOccupancyAsync(int? cinemaId, DateOnly from, DateOnly to);
    Task<List<ShowtimePerfRow>> GetShowtimePerformanceAsync(int? cinemaId, DateOnly from, DateOnly to);
}

public class StatisticsRepository : IStatisticsRepository
{
    private readonly ApplicationDbContext _db;

    public StatisticsRepository(ApplicationDbContext db) => _db = db;

    private IQueryable<Payment> SuccessfulPayments(int? cinemaId, DateTime from, DateTime to) =>
        _db.Payments.AsNoTracking()
            .Where(p => p.PaymentStatus == PayStatus.Success && p.PaymentDate >= from && p.PaymentDate < to)
            .InCinema(cinemaId);

    public async Task<List<PaymentRow>> GetSuccessfulPaymentsAsync(int? cinemaId, DateTime from, DateTime to)
    {
        var rows = await SuccessfulPayments(cinemaId, from, to)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync();
        return rows.Select(r => new PaymentRow(r.PaymentDate, r.Amount ?? 0, null, null)).ToList();
    }

    public async Task<List<PaymentRow>> GetSuccessfulPaymentsByMovieAsync(int? cinemaId, DateTime from, DateTime to)
    {
        // Doanh thu của 1 đơn được tính cho phim của vé đầu tiên trong đơn (1 đơn = 1 suất chiếu)
        var rows = await SuccessfulPayments(cinemaId, from, to)
            .Select(p => new
            {
                p.PaymentDate,
                p.Amount,
                MovieId = p.Booking.Tickets.Select(t => t.ShowtimeSeat.Showtime.MovieID).FirstOrDefault(),
                Title = p.Booking.Tickets.Select(t => t.ShowtimeSeat.Showtime.Movie.Title).FirstOrDefault()
            })
            .ToListAsync();
        return rows.Select(r => new PaymentRow(r.PaymentDate, r.Amount ?? 0, r.MovieId, r.Title)).ToList();
    }

    public async Task<List<SoldTicketRow>> GetSoldTicketsAsync(int? cinemaId, DateTime from, DateTime to)
    {
        var q = _db.Tickets.AsNoTracking()
            .Where(t => t.Status != TicketStatus.Cancelled
                        && t.Booking.Status == BookingStatus.Confirmed
                        && t.Booking.CreatedDate >= from && t.Booking.CreatedDate < to);

        if (cinemaId.HasValue) q = q.Where(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId);

        var rows = await q.Select(t => new
        {
            t.Booking.CreatedDate,
            MovieId = t.ShowtimeSeat.Showtime.MovieID,
            Title = t.ShowtimeSeat.Showtime.Movie.Title
        }).ToListAsync();

        return rows.Select(r => new SoldTicketRow(r.CreatedDate, r.MovieId, r.Title)).ToList();
    }

    public async Task<List<BookingRow>> GetBookingsAsync(int? cinemaId, DateTime from, DateTime to)
    {
        var rows = await _db.Bookings.AsNoTracking()
            .Where(b => b.CreatedDate >= from && b.CreatedDate < to)
            .InCinema(cinemaId)
            .Select(b => new { b.CreatedDate, b.Status })
            .ToListAsync();
        return rows.Select(r => new BookingRow(r.CreatedDate, r.Status)).ToList();
    }

    public async Task<List<OccupancyRow>> GetOccupancyAsync(int? cinemaId, DateOnly from, DateOnly to)
    {
        var q = _db.Showtimes.AsNoTracking()
            .Where(s => s.Status != ShowtimeStatus.Cancelled && s.Date >= from && s.Date <= to);
        if (cinemaId.HasValue) q = q.Where(s => s.Room.CinemaID == cinemaId);

        var rows = await q.Select(s => new
        {
            s.ShowtimeID,
            Total = s.ShowtimeSeats.Count(x => x.Status != SeatStatus.Disabled),
            Sold = _db.Tickets.Count(t => t.ShowtimeSeat.ShowtimeID == s.ShowtimeID && t.Status != TicketStatus.Cancelled)
        }).ToListAsync();

        return rows.Select(r => new OccupancyRow(r.ShowtimeID, r.Total, r.Sold)).ToList();
    }

    public async Task<List<ShowtimePerfRow>> GetShowtimePerformanceAsync(int? cinemaId, DateOnly from, DateOnly to)
    {
        var q = _db.Showtimes.AsNoTracking()
            .Where(s => s.Status != ShowtimeStatus.Cancelled && s.Date >= from && s.Date <= to);
        if (cinemaId.HasValue) q = q.Where(s => s.Room.CinemaID == cinemaId);

        var rows = await q.OrderByDescending(s => s.Date).ThenByDescending(s => s.StartHour)
            .Select(s => new
            {
                s.ShowtimeID,
                MovieTitle = s.Movie.Title,
                CinemaName = s.Room.Cinema.CinemaName,
                RoomName = s.Room.RoomName,
                s.Date,
                s.StartHour,
                Total = s.ShowtimeSeats.Count(x => x.Status != SeatStatus.Disabled),
                Sold = _db.Tickets.Count(t => t.ShowtimeSeat.ShowtimeID == s.ShowtimeID && t.Status != TicketStatus.Cancelled),
                Revenue = _db.Tickets
                    .Where(t => t.ShowtimeSeat.ShowtimeID == s.ShowtimeID && t.Status != TicketStatus.Cancelled)
                    .Sum(t => t.Price) ?? 0m
            }).ToListAsync();

        return rows.Select(r => new ShowtimePerfRow(r.ShowtimeID, r.MovieTitle, r.CinemaName, r.RoomName,
                                                    r.Date, r.StartHour, r.Total, r.Sold, r.Revenue)).ToList();
    }
}

/// <summary>
/// Lọc dữ liệu theo rạp. cinemaId = null nghĩa là không giới hạn (Admin).
/// Booking/Payment/Invoice không có cột CinemaID nên suy ra từ vé: Ticket -> Showtime_Seat -> Showtime -> Room -> Cinema.
/// </summary>
internal static class ScopeExtensions
{
    public static IQueryable<Booking> InCinema(this IQueryable<Booking> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(b => b.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));

    public static IQueryable<Payment> InCinema(this IQueryable<Payment> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(p => p.Booking.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));

    public static IQueryable<Invoice> InCinema(this IQueryable<Invoice> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(i => i.Booking.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));
}
