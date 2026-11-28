using Backend.Common;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface ICinemaRepository
{
    Task<List<Cinema>> GetAllAsync(int? cinemaId, string? status = null, CancellationToken ct = default);
    Task<Cinema?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Cinema?> GetForUpdateAsync(int id, CancellationToken ct = default);     // có tracking
    Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct = default);
    Task<bool> HasUpcomingShowtimesAsync(int cinemaId, CancellationToken ct = default);
    Task AddAsync(Cinema cinema, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class CinemaRepository : ICinemaRepository
{
    private readonly ApplicationDbContext _db;

    public CinemaRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Cinema>> GetAllAsync(int? cinemaId, string? status = null, CancellationToken ct = default) =>
        _db.Cinemas.AsNoTracking()
            .Where(c => cinemaId == null || c.CinemaID == cinemaId)
            .Where(c => status == null || c.Status == status)
            .OrderBy(c => c.CinemaName)
            .ToListAsync(ct);

    public Task<Cinema?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.CinemaID == id, ct);

    public Task<Cinema?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        _db.Cinemas.FirstOrDefaultAsync(c => c.CinemaID == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) => _db.Cinemas.AnyAsync(c => c.CinemaID == id, ct);

    public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct = default) =>
        _db.Cinemas.AnyAsync(c => c.CinemaName == name && (exceptId == null || c.CinemaID != exceptId), ct);

    public Task<bool> HasUpcomingShowtimesAsync(int cinemaId, CancellationToken ct = default)
    {
        var now = AppClock.LocalNow;
        var today = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        return _db.Showtimes.AnyAsync(s => s.Room.CinemaID == cinemaId
                                           && s.Status != ShowtimeStatus.Cancelled
                                           && (s.Date > today || (s.Date == today && s.StartHour > time)), ct);
    }

    public async Task AddAsync(Cinema cinema, CancellationToken ct = default)
    {
        _db.Cinemas.Add(cinema);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
