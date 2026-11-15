using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IRoomRepository
{
    Task<List<Room>> SearchAsync(int? cinemaId, string? keyword);
    Task<Room?> GetByIdAsync(int id);   // có tracking
    Task<Dictionary<int, int>> GetSeatCountsAsync(List<int> roomIds);
    Task<bool> NameExistsAsync(int? cinemaId, string name, int? exceptRoomId);
    Task<bool> HasShowtimesAsync(int roomId);
    Task AddAsync(Room room);
    Task DeleteAsync(Room room);
    Task SaveAsync();
}

public class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _db;

    public RoomRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Room>> SearchAsync(int? cinemaId, string? keyword)
    {
        var q = _db.Rooms.AsNoTracking().Include(r => r.Cinema).AsQueryable();
        if (cinemaId.HasValue) q = q.Where(r => r.CinemaID == cinemaId);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(r => r.RoomName!.Contains(k));
        }
        return q.OrderBy(r => r.CinemaID).ThenBy(r => r.RoomName).ToListAsync();
    }

    public Task<Room?> GetByIdAsync(int id) =>
        _db.Rooms.Include(r => r.Cinema).FirstOrDefaultAsync(r => r.RoomID == id);

    public async Task<Dictionary<int, int>> GetSeatCountsAsync(List<int> roomIds)
    {
        var rows = await _db.Seats.AsNoTracking()
            .Where(s => roomIds.Contains(s.RoomID!.Value))
            .GroupBy(s => s.RoomID)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.Where(r => r.RoomId != null).ToDictionary(r => r.RoomId!.Value, r => r.Count);
    }

    public Task<bool> NameExistsAsync(int? cinemaId, string name, int? exceptRoomId) =>
        _db.Rooms.AnyAsync(r => r.CinemaID == cinemaId && r.RoomName == name
                                && (exceptRoomId == null || r.RoomID != exceptRoomId));

    public Task<bool> HasShowtimesAsync(int roomId) => _db.Showtimes.AnyAsync(s => s.RoomID == roomId);

    public async Task AddAsync(Room room)
    {
        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Room room)
    {
        _db.Seats.RemoveRange(_db.Seats.Where(s => s.RoomID == room.RoomID));
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
