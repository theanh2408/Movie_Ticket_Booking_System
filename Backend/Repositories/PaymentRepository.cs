using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IPaymentRepository
{
    Task<(List<Payment> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(int id, int? cinemaId, CancellationToken ct = default);
}

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _db;

    public PaymentRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Payment> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, string? status, DateTime? from, DateTime? to, PageQuery paging, CancellationToken ct = default)
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

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Payment> ordered = paging.GetSortKey() switch
        {
            null or "date" => q.By(p => p.PaymentDate, desc),
            "id"           => q.By(p => p.PaymentID, desc),
            "amount"       => q.By(p => p.Amount, desc),
            "status"       => q.By(p => p.PaymentStatus, desc),
            _ => throw PagingExtensions.UnknownSort("date, id, amount, status")
        };
        return await ordered.ThenBy(p => p.PaymentID).ToPagedAsync(paging, ct);
    }

    public Task<Payment?> GetByIdAsync(int id, int? cinemaId, CancellationToken ct = default) =>
        _db.Payments.AsNoTracking()
            .Include(p => p.Booking).ThenInclude(b => b.Invoice)
            .Where(p => p.PaymentID == id)
            .InCinema(cinemaId)
            .FirstOrDefaultAsync(ct);
}
