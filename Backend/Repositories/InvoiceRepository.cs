using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IInvoiceRepository
{
    Task<(List<Invoice> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(int id, int? cinemaId, CancellationToken ct = default);   // có tracking, kèm Booking
    Task SaveAsync(CancellationToken ct = default);
}

public class InvoiceRepository : IInvoiceRepository
{
    private readonly ApplicationDbContext _db;

    public InvoiceRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Invoice> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Booking).ThenInclude(b => b.Customer).ThenInclude(c => c.Account)
            .InCinema(cinemaId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            int? idMatch = int.TryParse(k, out var n) ? n : null;
            // mã hóa đơn, mã đơn, tên / email / SĐT khách hàng, mã vé (TicketID hoặc mã vạch)
            q = q.Where(i => i.InvoiceID == idMatch || i.BookingID == idMatch
                             || i.Booking.Customer.Account.FullName!.Contains(k)
                             || i.Booking.Customer.Account.Email!.Contains(k)
                             || i.Booking.Customer.Account.Phone!.Contains(k)
                             || i.Booking.Tickets.Any(t => t.TicketID == idMatch || t.Barcode == k));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(i => i.Status == status);
        if (from.HasValue) q = q.Where(i => i.CreateDate >= from);
        if (to.HasValue)   q = q.Where(i => i.CreateDate < to);

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Invoice> ordered = paging.GetSortKey() switch
        {
            null or "id"  => q.By(i => i.InvoiceID, desc),
            "createdate"  => q.By(i => i.CreateDate, desc),
            "totalprice"  => q.By(i => i.TotalPrice, desc),
            "status"      => q.By(i => i.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, createDate, totalPrice, status")
        };
        return await ordered.ThenBy(i => i.InvoiceID).ToPagedAsync(paging, ct);
    }

    public Task<Invoice?> GetByIdAsync(int id, int? cinemaId, CancellationToken ct = default) =>
        _db.Invoices
            .Include(i => i.Booking)
            .Where(i => i.InvoiceID == id)
            .InCinema(cinemaId)
            .FirstOrDefaultAsync(ct);

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
