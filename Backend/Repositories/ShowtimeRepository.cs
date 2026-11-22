using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IShowtimeRepository
{
    Task<List<Showtime>> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword);
    Task<Showtime?> GetByIdAsync(int id);   // có tracking, kèm Movie + Room + Cinema
    Task<List<Showtime>> GetRoomShowtimesAsync(int roomId, DateOnly from, DateOnly to, int? exceptShowtimeId);
    Task<int> CountActiveTicketsAsync(int showtimeId);
    Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId);          // kèm Seat
    Task<ShowtimeSeat?> GetShowtimeSeatAsync(int showtimeId, int seatId);    // có tracking
    Task<bool> ShowtimeSeatHasTicketAsync(int showtimeSeatId);
    Task<List<Ticket>> GetActiveTicketsAsync(int showtimeId);                 // có tracking, kèm Booking
    Task<List<ShowtimeSeat>> GetShowtimeSeatsTrackedAsync(int showtimeId);    // có tracking
    Task ExecuteInTransactionAsync(Func<Task> work, System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);
    Task AddAsync(Showtime showtime);
    Task SaveAsync();
}

public class ShowtimeRepository : IShowtimeRepository
{
    private readonly ApplicationDbContext _db;

    public ShowtimeRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Showtime>> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword)
    {
        var q = _db.Showtimes.AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room).ThenInclude(r => r.Cinema)
            .AsQueryable();

        if (cinemaId.HasValue) q = q.Where(s => s.Room.CinemaID == cinemaId);
        if (movieId.HasValue)  q = q.Where(s => s.MovieID == movieId);
        if (roomId.HasValue)   q = q.Where(s => s.RoomID == roomId);
        if (date.HasValue)     q = q.Where(s => s.Date == date);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(s => s.Status == status);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(s => s.Movie.Title!.Contains(k));
        }

        return q.OrderByDescending(s => s.Date).ThenByDescending(s => s.StartHour).ToListAsync();
    }

    public Task<Showtime?> GetByIdAsync(int id) =>
        _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id);

    public Task<List<Showtime>> GetRoomShowtimesAsync(int roomId, DateOnly from, DateOnly to, int? exceptShowtimeId) =>
        _db.Showtimes.AsNoTracking()
            .Include(s => s.Movie)
            .Where(s => s.RoomID == roomId
                        && s.Status != ShowtimeStatus.Cancelled
                        && s.Date >= from && s.Date <= to
                        && (exceptShowtimeId == null || s.ShowtimeID != exceptShowtimeId))
            .ToListAsync();

    public Task<int> CountActiveTicketsAsync(int showtimeId) =>
        _db.Tickets.CountAsync(t => t.ShowtimeSeat.ShowtimeID == showtimeId && t.Status != TicketStatus.Cancelled);

    public Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId) =>
        _db.ShowtimeSeats.AsNoTracking().Include(x => x.Seat).Where(x => x.ShowtimeID == showtimeId).ToListAsync();

    public Task<ShowtimeSeat?> GetShowtimeSeatAsync(int showtimeId, int seatId) =>
        _db.ShowtimeSeats.FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId && x.SeatID == seatId);

    public Task<bool> ShowtimeSeatHasTicketAsync(int showtimeSeatId) =>
        _db.Tickets.AnyAsync(t => t.ShowtimeSeatID == showtimeSeatId && t.Status != TicketStatus.Cancelled);

    public Task<List<Ticket>> GetActiveTicketsAsync(int showtimeId) =>
        _db.Tickets
            .Include(t => t.Booking)
            .Where(t => t.ShowtimeSeat.ShowtimeID == showtimeId && t.Status != TicketStatus.Cancelled)
            .ToListAsync();

    public Task<List<ShowtimeSeat>> GetShowtimeSeatsTrackedAsync(int showtimeId) =>
        _db.ShowtimeSeats.Where(x => x.ShowtimeID == showtimeId).ToListAsync();

    /// <summary>Chạy một nhóm thao tác trong 1 transaction. Có lỗi (exception) thì tự rollback.</summary>
    public async Task ExecuteInTransactionAsync(Func<Task> work,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(isolationLevel);
        await work();
        await tx.CommitAsync();
    }

    public async Task AddAsync(Showtime showtime)
    {
        _db.Showtimes.Add(showtime);    // kèm ShowtimeSeats -> lưu trong 1 transaction
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}

public interface ICinemaRepository
{
    Task<List<Cinema>> GetAllAsync(int? cinemaId, string? status = null);
    Task<Cinema?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public class CinemaRepository : ICinemaRepository
{
    private readonly ApplicationDbContext _db;

    public CinemaRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Cinema>> GetAllAsync(int? cinemaId, string? status = null) =>
        _db.Cinemas.AsNoTracking()
            .Where(c => cinemaId == null || c.CinemaID == cinemaId)
            .Where(c => status == null || c.Status == status)
            .OrderBy(c => c.CinemaName)
            .ToListAsync();

    public Task<Cinema?> GetByIdAsync(int id) =>
        _db.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.CinemaID == id);

    public Task<bool> ExistsAsync(int id) => _db.Cinemas.AnyAsync(c => c.CinemaID == id);
}

public interface IRoomRepository
{
    Task<List<Room>> SearchAsync(int? cinemaId, string? keyword);
    Task<Room?> GetByIdAsync(int id);   // có tracking
    Task<Dictionary<int, int>> GetSeatCountsAsync(List<int> roomIds);
    Task<bool> NameExistsAsync(int? cinemaId, string name, int? exceptRoomId);
    Task<bool> HasShowtimesAsync(int roomId);
    Task<bool> HasUpcomingShowtimesAsync(int roomId);
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

    /// <summary>Phòng còn suất chiếu chưa diễn ra (từ hôm nay trở đi) và chưa bị hủy.</summary>
    public Task<bool> HasUpcomingShowtimesAsync(int roomId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _db.Showtimes.AnyAsync(s => s.RoomID == roomId
                                           && s.Status != ShowtimeStatus.Cancelled
                                           && s.Date >= today);
    }

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
