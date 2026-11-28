using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IBookingRepository
{
    Task<(List<Booking> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default);
    Task<Booking?> GetDetailAsync(int id, int? cinemaId, CancellationToken ct = default);   // cinemaId != null: chỉ trả về nếu đơn thuộc rạp đó
    Task<Booking?> GetForCheckInAsync(int bookingId, CancellationToken ct = default);       // có tracking, kèm vé + suất chiếu
    Task<BookingQR?> GetQrByTokenAsync(string token, CancellationToken ct = default);
    Task<int?> GetBookingIdByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;

    public BookingRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Booking> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default)
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

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Booking> ordered = paging.GetSortKey() switch
        {
            null or "id"  => q.By(b => b.BookingID, desc),
            "createddate" => q.By(b => b.CreatedDate, desc),
            "totalamount" => q.By(b => b.TotalAmount, desc),
            "status"      => q.By(b => b.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, createdDate, totalAmount, status")
        };
        return await ordered.ThenBy(b => b.BookingID).ToPagedAsync(paging, ct);
    }

    public Task<Booking?> GetDetailAsync(int id, int? cinemaId, CancellationToken ct = default) =>
        _db.Bookings.AsNoTracking().AsSplitQuery()
            .Where(b => b.BookingID == id)
            .InCinema(cinemaId)
            .Include(b => b.Customer).ThenInclude(c => c.Account)
            .Include(b => b.Payments)
            .Include(b => b.Invoice)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Seat)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(ct);

    public Task<Booking?> GetForCheckInAsync(int bookingId, CancellationToken ct = default) =>
        _db.Bookings.AsSplitQuery()
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Seat)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(b => b.BookingID == bookingId, ct);

    public Task<BookingQR?> GetQrByTokenAsync(string token, CancellationToken ct = default) =>
        _db.BookingQRs.AsNoTracking().FirstOrDefaultAsync(q => q.Token == token, ct);

    public Task<int?> GetBookingIdByBarcodeAsync(string barcode, CancellationToken ct = default) =>
        _db.Tickets.AsNoTracking().Where(t => t.Barcode == barcode).Select(t => t.BookingID).FirstOrDefaultAsync(ct);

    public Task<bool> CustomerExistsAsync(int customerId, CancellationToken ct = default) => _db.Customers.AnyAsync(c => c.CustomerID == customerId, ct);

    public async Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Add(booking);   // kèm vé, thanh toán, hóa đơn, QR, khuyến mãi -> lưu trong 1 lần
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
