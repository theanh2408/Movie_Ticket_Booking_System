using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IShowtimeRepository
{
    Task<(List<Showtime> Items, int Total)> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword, PageQuery paging, CancellationToken ct = default);
    Task<Showtime?> GetByIdAsync(int id, CancellationToken ct = default);   // có tracking, kèm Movie + Room + Cinema
    Task<List<Showtime>> GetRoomShowtimesAsync(int roomId, DateOnly from, DateOnly to, int? exceptShowtimeId, CancellationToken ct = default);
    Task<int> CountActiveTicketsAsync(int showtimeId, CancellationToken ct = default);
    Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId, CancellationToken ct = default);          // kèm Seat
    Task<ShowtimeSeat?> GetShowtimeSeatAsync(int showtimeId, int seatId, CancellationToken ct = default);    // có tracking
    Task<bool> ShowtimeSeatHasTicketAsync(int showtimeSeatId, CancellationToken ct = default);
    Task<List<Ticket>> GetActiveTicketsAsync(int showtimeId, CancellationToken ct = default);                 // có tracking, kèm Booking
    Task<bool> ShowtimeHasAnyTicketAsync(int showtimeId, CancellationToken ct = default);                     // kể cả vé đã hủy
    Task<List<ShowtimeSeat>> GetShowtimeSeatsByIdsAsync(int showtimeId, List<int> seatIds, CancellationToken ct = default);   // có tracking, kèm Seat
    void RemoveShowtimeSeats(IEnumerable<ShowtimeSeat> items);
    void AddShowtimeSeats(IEnumerable<ShowtimeSeat> items);
    Task<List<ShowtimeSeat>> GetShowtimeSeatsTrackedAsync(int showtimeId, CancellationToken ct = default);    // có tracking
    Task ExecuteInTransactionAsync(Func<Task> work, System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task AddAsync(Showtime showtime, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class ShowtimeRepository : IShowtimeRepository
{
    private readonly ApplicationDbContext _db;

    public ShowtimeRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Showtime> Items, int Total)> SearchAsync(int? cinemaId, int? movieId, int? roomId, DateOnly? date, string? status, string? keyword, PageQuery paging, CancellationToken ct = default)
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

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Showtime> ordered = paging.GetSortKey() switch
        {
            null or "date" => desc
                ? q.OrderByDescending(s => s.Date).ThenByDescending(s => s.StartHour)
                : q.OrderBy(s => s.Date).ThenBy(s => s.StartHour),
            "id"           => q.By(s => s.ShowtimeID, desc),
            "price"        => q.By(s => s.BasePrice, desc),
            "status"       => q.By(s => s.Status, desc),
            _ => throw PagingExtensions.UnknownSort("date, id, price, status")
        };
        return await ordered.ThenBy(s => s.ShowtimeID).ToPagedAsync(paging, ct);
    }

    public Task<Showtime?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room).ThenInclude(r => r.Cinema)
            .FirstOrDefaultAsync(s => s.ShowtimeID == id, ct);

    public Task<List<Showtime>> GetRoomShowtimesAsync(int roomId, DateOnly from, DateOnly to, int? exceptShowtimeId, CancellationToken ct = default) =>
        _db.Showtimes.AsNoTracking()
            .Include(s => s.Movie)
            .Where(s => s.RoomID == roomId
                        && s.Status != ShowtimeStatus.Cancelled
                        && s.Date >= from && s.Date <= to
                        && (exceptShowtimeId == null || s.ShowtimeID != exceptShowtimeId))
            .ToListAsync(ct);

    public Task<int> CountActiveTicketsAsync(int showtimeId, CancellationToken ct = default) =>
        _db.Tickets.CountAsync(t => t.ShowtimeSeat.ShowtimeID == showtimeId && t.Status != TicketStatus.Cancelled, ct);

    public Task<List<ShowtimeSeat>> GetShowtimeSeatsAsync(int showtimeId, CancellationToken ct = default) =>
        _db.ShowtimeSeats.AsNoTracking().Include(x => x.Seat).Where(x => x.ShowtimeID == showtimeId).ToListAsync(ct);

    public Task<ShowtimeSeat?> GetShowtimeSeatAsync(int showtimeId, int seatId, CancellationToken ct = default) =>
        _db.ShowtimeSeats.FirstOrDefaultAsync(x => x.ShowtimeID == showtimeId && x.SeatID == seatId, ct);

    public Task<bool> ShowtimeSeatHasTicketAsync(int showtimeSeatId, CancellationToken ct = default) =>
        _db.Tickets.AnyAsync(t => t.ShowtimeSeatID == showtimeSeatId && t.Status != TicketStatus.Cancelled, ct);

    public Task<List<Ticket>> GetActiveTicketsAsync(int showtimeId, CancellationToken ct = default) =>
        _db.Tickets
            .Include(t => t.Booking)
            .Where(t => t.ShowtimeSeat.ShowtimeID == showtimeId && t.Status != TicketStatus.Cancelled)
            .ToListAsync(ct);

    public Task<List<ShowtimeSeat>> GetShowtimeSeatsTrackedAsync(int showtimeId, CancellationToken ct = default) =>
        _db.ShowtimeSeats.Where(x => x.ShowtimeID == showtimeId).ToListAsync(ct);

    /// <summary>Chạy một nhóm thao tác trong 1 transaction. Có lỗi (exception) thì tự rollback.</summary>
    public async Task ExecuteInTransactionAsync(Func<Task> work,
        System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(isolationLevel, ct);
        await work();
        await tx.CommitAsync(ct);
    }

    public Task<bool> ShowtimeHasAnyTicketAsync(int showtimeId, CancellationToken ct = default) =>
        _db.Tickets.AnyAsync(t => t.ShowtimeSeat.ShowtimeID == showtimeId, ct);

    public Task<List<ShowtimeSeat>> GetShowtimeSeatsByIdsAsync(int showtimeId, List<int> seatIds, CancellationToken ct = default) =>
        _db.ShowtimeSeats.Include(x => x.Seat)
            .Where(x => x.ShowtimeID == showtimeId && seatIds.Contains(x.SeatID!.Value))
            .ToListAsync(ct);

    public void RemoveShowtimeSeats(IEnumerable<ShowtimeSeat> items) => _db.ShowtimeSeats.RemoveRange(items);

    public void AddShowtimeSeats(IEnumerable<ShowtimeSeat> items) => _db.ShowtimeSeats.AddRange(items);

    public async Task AddAsync(Showtime showtime, CancellationToken ct = default)
    {
        _db.Showtimes.Add(showtime);    // kèm ShowtimeSeats -> lưu trong 1 transaction
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
