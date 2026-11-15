using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

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
