using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IPromotionRepository
{
    Task<List<Promotion>> SearchAsync(string? keyword, string? status);
    Task<Promotion?> GetByIdAsync(int id);          // có tracking
    Task<Promotion?> GetByCodeAsync(string code);   // không tracking
    Task<bool> CodeExistsAsync(string code, int? exceptId = null);
    Task<bool> IsUsedAsync(int promotionId);
    Task<int> CountUsageAsync(int promotionId);
    Task AddAsync(Promotion promotion);
    Task RemoveAsync(Promotion promotion);
    Task SaveAsync();
}

public class PromotionRepository : IPromotionRepository
{
    private readonly ApplicationDbContext _db;

    public PromotionRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Promotion>> SearchAsync(string? keyword, string? status)
    {
        var q = _db.Promotions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(p => p.Code!.Contains(k) || p.PromotionName!.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.Status == status);
        return q.OrderByDescending(p => p.PromotionID).ToListAsync();
    }

    public Task<Promotion?> GetByIdAsync(int id) => _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id);

    public Task<Promotion?> GetByCodeAsync(string code) =>
        _db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code);

    public Task<bool> CodeExistsAsync(string code, int? exceptId = null) =>
        _db.Promotions.AnyAsync(p => p.Code == code && (exceptId == null || p.PromotionID != exceptId));

    public Task<bool> IsUsedAsync(int promotionId) => _db.BookingPromotions.AnyAsync(x => x.PromotionID == promotionId);

    public Task<int> CountUsageAsync(int promotionId) => _db.BookingPromotions.CountAsync(x => x.PromotionID == promotionId);

    public async Task AddAsync(Promotion promotion)
    {
        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Promotion promotion)
    {
        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
