using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IPromotionRepository
{
    Task<(List<Promotion> Items, int Total)> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default);
    Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default);          // có tracking
    Task<Promotion?> GetByCodeAsync(string code, CancellationToken ct = default);   // không tracking
    Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken ct = default);
    Task<bool> IsUsedAsync(int promotionId, CancellationToken ct = default);
    Task<int> CountUsageAsync(int promotionId, CancellationToken ct = default);
    Task<int> CountCustomerUsageAsync(int promotionId, int customerId, CancellationToken ct = default);
    Task<bool> BookingHasPromotionAsync(int bookingId, CancellationToken ct = default);
    Task AddAsync(Promotion promotion, CancellationToken ct = default);
    Task RemoveAsync(Promotion promotion, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class PromotionRepository : IPromotionRepository
{
    private readonly ApplicationDbContext _db;

    public PromotionRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Promotion> Items, int Total)> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default)
    {
        var q = _db.Promotions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(p => p.Code!.Contains(k) || p.PromotionName!.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.Status == status);
        var desc = paging.IsDesc(true);
        IOrderedQueryable<Promotion> ordered = paging.GetSortKey() switch
        {
            null or "id" => q.By(p => p.PromotionID, desc),
            "code"       => q.By(p => p.Code, desc),
            "startdate"  => q.By(p => p.StartDate, desc),
            "enddate"    => q.By(p => p.EndDate, desc),
            "status"     => q.By(p => p.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, code, startDate, endDate, status")
        };
        return await ordered.ThenBy(p => p.PromotionID).ToPagedAsync(paging, ct);
    }

    public Task<Promotion?> GetByIdAsync(int id, CancellationToken ct = default) => _db.Promotions.FirstOrDefaultAsync(p => p.PromotionID == id, ct);

    public Task<Promotion?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        _db.Promotions.AsNoTracking().FirstOrDefaultAsync(p => p.Code == code, ct);

    public Task<bool> CodeExistsAsync(string code, int? exceptId = null, CancellationToken ct = default) =>
        _db.Promotions.AnyAsync(p => p.Code == code && (exceptId == null || p.PromotionID != exceptId), ct);

    public Task<bool> IsUsedAsync(int promotionId, CancellationToken ct = default) => _db.BookingPromotions.AnyAsync(x => x.PromotionID == promotionId, ct);

    /// <summary>Chỉ đếm lượt dùng trên các đơn còn hiệu lực (Confirmed / Pending); đơn Cancelled / Expired không tính.</summary>
    public Task<int> CountUsageAsync(int promotionId, CancellationToken ct = default) =>
        _db.BookingPromotions.CountAsync(x => x.PromotionID == promotionId
            && (x.Booking.Status == BookingStatus.Confirmed || x.Booking.Status == BookingStatus.Pending), ct);

    /// <summary>Số lần một khách hàng đã dùng mã (cũng chỉ tính đơn Confirmed / Pending).</summary>
    public Task<int> CountCustomerUsageAsync(int promotionId, int customerId, CancellationToken ct = default) =>
        _db.BookingPromotions.CountAsync(x => x.PromotionID == promotionId
            && x.Booking.CustomerID == customerId
            && (x.Booking.Status == BookingStatus.Confirmed || x.Booking.Status == BookingStatus.Pending), ct);

    /// <summary>Đơn đặt vé này đã áp dụng một khuyến mãi nào chưa.</summary>
    public Task<bool> BookingHasPromotionAsync(int bookingId, CancellationToken ct = default) =>
        _db.BookingPromotions.AnyAsync(x => x.BookingID == bookingId, ct);

    public async Task AddAsync(Promotion promotion, CancellationToken ct = default)
    {
        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Promotion promotion, CancellationToken ct = default)
    {
        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
