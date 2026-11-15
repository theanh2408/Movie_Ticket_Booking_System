using Backend.Models;

namespace Backend.Repositories;

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
