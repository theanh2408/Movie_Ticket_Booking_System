using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IBookingRepository
{
    Task<List<Booking>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to);
    Task<Booking?> GetDetailAsync(int id, int? cinemaId);   // cinemaId != null: chỉ trả về nếu đơn thuộc rạp đó
}

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;

    public BookingRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Booking>> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to)
    {
        var q = _db.Bookings.AsNoTracking()
            .Include(b => b.Customer).ThenInclude(c => c.Account)
            .InCinema(cinemaId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            int? idMatch = int.TryParse(k, out var n) ? n : null;
            q = q.Where(b => b.BookingID == idMatch
                             || b.Customer.Account.FullName!.Contains(k)
                             || b.Customer.Account.Phone!.Contains(k)
                             || b.Customer.Account.Email!.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(b => b.Status == status);
        if (from.HasValue) q = q.Where(b => b.CreatedDate >= from);
        if (to.HasValue)   q = q.Where(b => b.CreatedDate < to);

        return q.OrderByDescending(b => b.BookingID).ToListAsync();
    }

    public Task<Booking?> GetDetailAsync(int id, int? cinemaId) =>
        _db.Bookings.AsNoTracking().AsSplitQuery()
            .Where(b => b.BookingID == id)
            .InCinema(cinemaId)
            .Include(b => b.Customer).ThenInclude(c => c.Account)
            .Include(b => b.Payments)
            .Include(b => b.Invoice)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Seat)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync();
}
