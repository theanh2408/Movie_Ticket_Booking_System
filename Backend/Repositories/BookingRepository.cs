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
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Seat)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(ss => ss.Showtime).ThenInclude(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync();
}

/// <summary>Trạng thái hiện tại của tài khoản trong DB, dùng để thu hồi JWT sau khi xóa mềm / đổi quyền / đổi rạp.</summary>
public record AuthState(string? Status, string? Role, int? CinemaId);

public interface IAccountRepository
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByIdAsync(int userId);          // có tracking, dùng để sửa
    Task<Account?> GetDetailAsync(int userId);        // không tracking, dùng để trả về
    Task<AuthState?> GetAuthStateAsync(int userId);   // đọc nhẹ (Status, Role, CinemaID) để kiểm tra JWT
    Task<bool> EmailExistsAsync(string email, int? exceptUserId = null);
    Task<List<Account>> SearchAsync(string? keyword, string? role, string? status, int? cinemaId);
    Task AddAsync(Account account);
    void RemoveManager(Manager manager);
    void RemoveStaff(Staff staff);
    Task SaveAsync();
}

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _db;

    public AccountRepository(ApplicationDbContext db) => _db = db;

    private IQueryable<Account> WithProfile() => _db.Accounts
        .Include(a => a.Manager).ThenInclude(m => m!.Cinema)
        .Include(a => a.Staff).ThenInclude(s => s!.Cinema)
        .Include(a => a.Customer);

    public Task<Account?> GetByEmailAsync(string email) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);

    public Task<Account?> GetByIdAsync(int userId) =>
        WithProfile().FirstOrDefaultAsync(a => a.UserID == userId);

    public Task<Account?> GetDetailAsync(int userId) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.UserID == userId);

    public async Task<AuthState?> GetAuthStateAsync(int userId)
    {
        var row = await _db.Accounts.AsNoTracking()
            .Where(a => a.UserID == userId)
            .Select(a => new { a.Status, a.Role, CinemaId = a.Manager!.CinemaID ?? a.Staff!.CinemaID })
            .FirstOrDefaultAsync();
        return row == null ? null : new AuthState(row.Status, row.Role, row.CinemaId);
    }

    public Task<bool> EmailExistsAsync(string email, int? exceptUserId = null) =>
        _db.Accounts.AnyAsync(a => a.Email == email && (exceptUserId == null || a.UserID != exceptUserId));

    public Task<List<Account>> SearchAsync(string? keyword, string? role, string? status, int? cinemaId)
    {
        var q = WithProfile().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(a => a.FullName!.Contains(k) || a.Email!.Contains(k) || a.Phone!.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(role))   q = q.Where(a => a.Role == role);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(a => a.Status == status);
        if (cinemaId.HasValue)
            q = q.Where(a => a.Manager!.CinemaID == cinemaId || a.Staff!.CinemaID == cinemaId);

        return q.OrderByDescending(a => a.UserID).ToListAsync();
    }

    public async Task AddAsync(Account account)
    {
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
    }

    public void RemoveManager(Manager manager) => _db.Managers.Remove(manager);
    public void RemoveStaff(Staff staff) => _db.Staffs.Remove(staff);

    public Task SaveAsync() => _db.SaveChangesAsync();
}

public interface IPromotionRepository
{
    Task<List<Promotion>> SearchAsync(string? keyword, string? status);
    Task<Promotion?> GetByIdAsync(int id);          // có tracking
    Task<Promotion?> GetByCodeAsync(string code);   // không tracking
    Task<bool> CodeExistsAsync(string code, int? exceptId = null);
    Task<bool> IsUsedAsync(int promotionId);
    Task<int> CountUsageAsync(int promotionId);
    Task<int> CountCustomerUsageAsync(int promotionId, int customerId);
    Task<bool> BookingHasPromotionAsync(int bookingId);
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

    /// <summary>Chỉ đếm lượt dùng trên các đơn còn hiệu lực (Confirmed / Pending); đơn Cancelled / Expired không tính.</summary>
    public Task<int> CountUsageAsync(int promotionId) =>
        _db.BookingPromotions.CountAsync(x => x.PromotionID == promotionId
            && (x.Booking.Status == BookingStatus.Confirmed || x.Booking.Status == BookingStatus.Pending));

    /// <summary>Số lần một khách hàng đã dùng mã (cũng chỉ tính đơn Confirmed / Pending).</summary>
    public Task<int> CountCustomerUsageAsync(int promotionId, int customerId) =>
        _db.BookingPromotions.CountAsync(x => x.PromotionID == promotionId
            && x.Booking.CustomerID == customerId
            && (x.Booking.Status == BookingStatus.Confirmed || x.Booking.Status == BookingStatus.Pending));

    /// <summary>Đơn đặt vé này đã áp dụng một khuyến mãi nào chưa.</summary>
    public Task<bool> BookingHasPromotionAsync(int bookingId) =>
        _db.BookingPromotions.AnyAsync(x => x.BookingID == bookingId);

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
