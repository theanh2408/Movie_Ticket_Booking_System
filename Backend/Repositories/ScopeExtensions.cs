using Backend.Models;

namespace Backend.Repositories;

/// <summary>
/// Lọc dữ liệu theo rạp. cinemaId = null nghĩa là không giới hạn (Admin).
/// Booking/Payment/Invoice không có cột CinemaID nên suy ra từ vé: Ticket -> Showtime_Seat -> Showtime -> Room -> Cinema.
/// </summary>
internal static class ScopeExtensions
{
    // BUG-16: trước đây chỉ lọc qua vé (b.Tickets.Any(...)) nên Quản lý không thấy đơn Pending chưa sinh vé.
    // Nay một đơn thuộc rạp nếu: (a) Booking.ShowtimeID trỏ tới suất chiếu của rạp đó (có ngay từ khi tạo đơn),
    // hoặc (b) có vé thuộc suất chiếu của rạp đó (giữ lại để các đơn cũ chưa có ShowtimeID vẫn hiển thị đúng).
    public static IQueryable<Booking> InCinema(this IQueryable<Booking> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(b => b.Showtime.Room.CinemaID == cinemaId
                           || b.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));

    public static IQueryable<Payment> InCinema(this IQueryable<Payment> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(p => p.Booking.Showtime.Room.CinemaID == cinemaId
                           || p.Booking.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));

    public static IQueryable<Invoice> InCinema(this IQueryable<Invoice> q, int? cinemaId) =>
        cinemaId == null
            ? q
            : q.Where(i => i.Booking.Showtime.Room.CinemaID == cinemaId
                           || i.Booking.Tickets.Any(t => t.ShowtimeSeat.Showtime.Room.CinemaID == cinemaId));
}
