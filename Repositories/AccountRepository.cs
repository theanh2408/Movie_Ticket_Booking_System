using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account?> GetByIdAsync(int userId);          // có tracking, dùng để sửa
    Task<Account?> GetDetailAsync(int userId);        // không tracking, dùng để trả về
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
        .Include(a => a.Manager).ThenInclude(m => m.Cinema)
        .Include(a => a.Staff).ThenInclude(s => s.Cinema)
        .Include(a => a.Customer);

    public Task<Account?> GetByEmailAsync(string email) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.Email == email);

    public Task<Account?> GetByIdAsync(int userId) =>
        WithProfile().FirstOrDefaultAsync(a => a.UserID == userId);

    public Task<Account?> GetDetailAsync(int userId) =>
        WithProfile().AsNoTracking().FirstOrDefaultAsync(a => a.UserID == userId);

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
