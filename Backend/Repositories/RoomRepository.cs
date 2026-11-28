using Backend.Common;
using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IRoomRepository
{
    Task<(List<Room> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, PageQuery paging, CancellationToken ct = default);
    Task<Room?> GetByIdAsync(int id, CancellationToken ct = default);   // có tracking
    Task<Dictionary<int, int>> GetSeatCountsAsync(List<int> roomIds, CancellationToken ct = default);
    Task<bool> NameExistsAsync(int? cinemaId, string name, int? exceptRoomId, CancellationToken ct = default);
    Task<bool> HasShowtimesAsync(int roomId, CancellationToken ct = default);
    Task<bool> HasUpcomingShowtimesAsync(int roomId, CancellationToken ct = default);
    Task AddAsync(Room room, CancellationToken ct = default);
    Task DeleteAsync(Room room, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _db;

    public RoomRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Room> Items, int Total)> SearchAsync(int? cinemaId, string? keyword, PageQuery paging, CancellationToken ct = default)
    {
        var q = _db.Rooms.AsNoTracking().Include(r => r.Cinema).AsQueryable();
        if (cinemaId.HasValue) q = q.Where(r => r.CinemaID == cinemaId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(r => r.RoomName!.Contains(k));
        }
        var desc = paging.IsDesc(false);
        IOrderedQueryable<Room> ordered = paging.GetSortKey() switch
        {
            null       => q.OrderBy(r => r.CinemaID).ThenBy(r => r.RoomName),   // mặc định: theo rạp rồi theo tên phòng
            "id"       => q.By(r => r.RoomID, desc),
            "name"     => q.By(r => r.RoomName, desc),
            "cinemaid" => q.By(r => r.CinemaID, desc),
            "status"   => q.By(r => r.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, name, cinemaId, status")
        };
        return await ordered.ThenBy(r => r.RoomID).ToPagedAsync(paging, ct);
    }

    public Task<Room?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.RoomID == id, ct);

    public async Task<Dictionary<int, int>> GetSeatCountsAsync(List<int> roomIds, CancellationToken ct = default)
    {
        var rows = await _db.Seats.AsNoTracking()
            .Where(s => roomIds.Contains(s.RoomID!.Value))
            .GroupBy(s => s.RoomID)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.Where(r => r.RoomId != null).ToDictionary(r => r.RoomId!.Value, r => r.Count);
    }

    public Task<bool> NameExistsAsync(int? cinemaId, string name, int? exceptRoomId, CancellationToken ct = default) =>
        _db.Rooms.AnyAsync(r => r.CinemaID == cinemaId && r.RoomName == name
                                && (exceptRoomId == null || r.RoomID != exceptRoomId), ct);

    public Task<bool> HasShowtimesAsync(int roomId, CancellationToken ct = default) => _db.Showtimes.AnyAsync(s => s.RoomID == roomId, ct);

    /// <summary>Phòng còn suất chiếu chưa diễn ra (từ hôm nay trở đi) và chưa bị hủy.</summary>
    public Task<bool> HasUpcomingShowtimesAsync(int roomId, CancellationToken ct = default)
    {
        var now = AppClock.LocalNow;
        var today = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        return _db.Showtimes.AnyAsync(s => s.RoomID == roomId
                                           && s.Status != ShowtimeStatus.Cancelled
                                           && (s.Date > today || (s.Date == today && s.StartHour > time)), ct);
    }

    public async Task AddAsync(Room room, CancellationToken ct = default)
    {
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Room room, CancellationToken ct = default)
    {
        _db.Seats.RemoveRange(_db.Seats.Where(s => s.RoomID == room.RoomID));
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
