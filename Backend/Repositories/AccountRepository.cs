using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Account?> GetByIdAsync(int userId, CancellationToken ct = default);          // có tracking, dùng để sửa
    Task<Account?> GetDetailAsync(int userId, CancellationToken ct = default);        // không tracking, dùng để trả về
    Task<AuthState?> GetAuthStateAsync(int userId, CancellationToken ct = default);   // đọc nhẹ (Status, Role, CinemaID) để kiểm tra JWT
    Task<bool> EmailExistsAsync(string email, int? exceptUserId = null, CancellationToken ct = default);
    Task<(List<Account> Items, int Total)> SearchAsync(string? keyword, string? role, string? status, int? cinemaId, PageQuery paging, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    void RemoveManager(Manager manager);
    void RemoveStaff(Staff staff);
    Task SaveAsync(CancellationToken ct = default);
}

/// <summary>Trạng thái hiện tại của tài khoản trong DB, dùng để thu hồi JWT sau khi xóa mềm / đổi quyền / đổi rạp.</summary>
public record AuthState(string? Status, string? Role, int? CinemaId);

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _db;

    public AccountRepository(ApplicationDbContext db) => _db = db;

    private IQueryable<Account> WithProfile() => _db.Accounts
        .Include(a => a.Manager).ThenInclude(m => m!.Cinema)
        .Include(a => a.Staff).ThenInclude(s => s!.Cinema)
        .Include(a => a.Customer);

    public Task<Account?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.Email == email, ct);

    public Task<Account?> GetByIdAsync(int userId, CancellationToken ct = default) =>
        WithProfile().FirstOrDefaultAsync(a => a.UserID == userId, ct);

    public Task<Account?> GetDetailAsync(int userId, CancellationToken ct = default) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.UserID == userId, ct);

    public async Task<AuthState?> GetAuthStateAsync(int userId, CancellationToken ct = default)
    {
        var row = await _db.Accounts.AsNoTracking()
            .Where(a => a.UserID == userId)
            .Select(a => new { a.Status, a.Role, CinemaId = a.Manager!.CinemaID ?? a.Staff!.CinemaID })
            .FirstOrDefaultAsync(ct);
        return row == null ? null : new AuthState(row.Status, row.Role, row.CinemaId);
    }

    public Task<bool> EmailExistsAsync(string email, int? exceptUserId = null, CancellationToken ct = default) =>
        _db.Accounts.AnyAsync(a => a.Email == email && (exceptUserId == null || a.UserID != exceptUserId), ct);

    public async Task<(List<Account> Items, int Total)> SearchAsync(string? keyword, string? role, string? status, int? cinemaId, PageQuery paging, CancellationToken ct = default)
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

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Account> ordered = paging.GetSortKey() switch
        {
            null or "id" => q.By(a => a.UserID, desc),
            "fullname"   => q.By(a => a.FullName, desc),
            "email"      => q.By(a => a.Email, desc),
            "role"       => q.By(a => a.Role, desc),
            "status"     => q.By(a => a.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, fullName, email, role, status")
        };
        return await ordered.ThenBy(a => a.UserID).ToPagedAsync(paging, ct);
    }

    public async Task AddAsync(Account account, CancellationToken ct = default)
    {
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync(ct);
    }

    public void RemoveManager(Manager manager) => _db.Managers.Remove(manager);
    public void RemoveStaff(Staff staff) => _db.Staffs.Remove(staff);

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
