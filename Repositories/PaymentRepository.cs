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
