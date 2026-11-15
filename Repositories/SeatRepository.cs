using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByRoomAsync(int roomId);
    Task<Seat?> GetByIdAsync(int seatId);   // có tracking, kèm Room
    Task<bool> ExistsAsync(int roomId, string row, string column, int? exceptSeatId);
    Task<int> CountByRoomAsync(int roomId);
    Task<bool> HasShowtimeSeatsAsync(int seatId);
    Task AddRangeAsync(IEnumerable<Seat> seats);
    Task RemoveAsync(Seat seat);
    Task SaveAsync();
}

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _db;

    public SeatRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Seat>> GetByRoomAsync(int roomId) =>
        _db.Seats.AsNoTracking().Where(s => s.RoomID == roomId).ToListAsync();

    public Task<Seat?> GetByIdAsync(int seatId) =>
        _db.Seats.Include(s => s.Room).FirstOrDefaultAsync(s => s.SeatID == seatId);

    public Task<bool> ExistsAsync(int roomId, string row, string column, int? exceptSeatId) =>
        _db.Seats.AnyAsync(s => s.RoomID == roomId && s.Row == row && s.Column == column
                                && (exceptSeatId == null || s.SeatID != exceptSeatId));

    public Task<int> CountByRoomAsync(int roomId) => _db.Seats.CountAsync(s => s.RoomID == roomId);

    public Task<bool> HasShowtimeSeatsAsync(int seatId) => _db.ShowtimeSeats.AnyAsync(x => x.SeatID == seatId);

    public async Task AddRangeAsync(IEnumerable<Seat> seats)
    {
        _db.Seats.AddRange(seats);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Seat seat)
    {
        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
