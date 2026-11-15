using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

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
