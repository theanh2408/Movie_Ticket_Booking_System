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

    public async Task AddAsync(Showtime showtime)
    {
        _db.Showtimes.Add(showtime);    // kèm ShowtimeSeats -> lưu trong 1 transaction
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
