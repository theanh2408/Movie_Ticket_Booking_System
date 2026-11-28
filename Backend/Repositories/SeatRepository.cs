using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface ISeatRepository
{
    Task<List<Seat>> GetByRoomAsync(int roomId, CancellationToken ct = default);
    Task<Seat?> GetByIdAsync(int seatId, CancellationToken ct = default);   // có tracking, kèm Room
    Task<bool> ExistsAsync(int roomId, string row, string column, int? exceptSeatId, CancellationToken ct = default);
    Task<int> CountByRoomAsync(int roomId, CancellationToken ct = default);
    Task<bool> HasShowtimeSeatsAsync(int seatId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct = default);
    Task RemoveAsync(Seat seat, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class SeatRepository : ISeatRepository
{
    private readonly ApplicationDbContext _db;

    public SeatRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Seat>> GetByRoomAsync(int roomId, CancellationToken ct = default) =>
        _db.Seats.AsNoTracking().Where(s => s.RoomID == roomId).ToListAsync(ct);

    public Task<Seat?> GetByIdAsync(int seatId, CancellationToken ct = default) =>
        _db.Seats.Include(s => s.Room).FirstOrDefaultAsync(s => s.SeatID == seatId, ct);

    public Task<bool> ExistsAsync(int roomId, string row, string column, int? exceptSeatId, CancellationToken ct = default) =>
        _db.Seats.AnyAsync(s => s.RoomID == roomId && s.Row == row && s.Column == column
                                && (exceptSeatId == null || s.SeatID != exceptSeatId), ct);

    public Task<int> CountByRoomAsync(int roomId, CancellationToken ct = default) => _db.Seats.CountAsync(s => s.RoomID == roomId, ct);

    public Task<bool> HasShowtimeSeatsAsync(int seatId, CancellationToken ct = default) => _db.ShowtimeSeats.AnyAsync(x => x.SeatID == seatId, ct);

    public async Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken ct = default)
    {
        _db.Seats.AddRange(seats);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Seat seat, CancellationToken ct = default)
    {
        _db.Seats.Remove(seat);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
