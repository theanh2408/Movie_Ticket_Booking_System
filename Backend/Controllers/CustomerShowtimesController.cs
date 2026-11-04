
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/showtimes")]
public class CustomerShowtimesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerShowtimesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/showtimes
    // GET: api/showtimes?movieId=1&cinemaId=1&date=2026-11-03
    [HttpGet]
    public async Task<IActionResult> GetShowtimes(
        [FromQuery] int? movieId,
        [FromQuery] int? cinemaId,
        [FromQuery] DateOnly? date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                message = "Tham số phân trang không hợp lệ."
            });
        }

        var query = _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r!.Cinema)
            .Where(s =>
                s.Movie != null &&
                s.Room != null &&
                s.Room.Cinema != null);

        // Filter by movie
        if (movieId.HasValue)
        {
            query = query.Where(s => s.MovieId == movieId.Value);
        }

        // Filter by cinema
        if (cinemaId.HasValue)
        {
            query = query.Where(s =>
                s.Room!.CinemaId == cinemaId.Value);
        }

        // Filter by date
        if (date.HasValue)
        {
            query = query.Where(s => s.Date == date.Value);
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartHour)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                showtimeId = s.ShowtimeId,
                movieId = s.MovieId,
                movieTitle = s.Movie!.Title,
                cinemaId = s.Room!.CinemaId,
                cinemaName = s.Room.Cinema!.CinemaName,
                roomId = s.RoomId,
                roomName = s.Room.RoomName,

                startTime = s.Date.HasValue && s.StartHour.HasValue
                    ? s.Date.Value.ToDateTime(s.StartHour.Value)
                    : (DateTime?)null,

                basePrice = s.BasePrice,
                status = s.Status
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                items,
                page,
                pageSize,
                totalItems,
                totalPages = (int)Math.Ceiling(
                    totalItems / (double)pageSize)
            },
            message = "Lấy danh sách suất chiếu thành công."
        });
    }

    // GET: api/showtimes/1
    [HttpGet("{showtimeId:int}")]
    public async Task<IActionResult> GetShowtimeById(int showtimeId)
    {
        var showtime = await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r!.Cinema)
            .Where(s => s.ShowtimeId == showtimeId)
            .Select(s => new
            {
                showtimeId = s.ShowtimeId,
                movieId = s.MovieId,

                movieTitle = s.Movie != null
                    ? s.Movie.Title
                    : null,

                movieDuration = s.Movie != null
                    ? s.Movie.Duration
                    : (int?)null,

                cinemaId = s.Room != null
                    ? s.Room.CinemaId
                    : null,

                cinemaName = s.Room != null &&
                             s.Room.Cinema != null
                    ? s.Room.Cinema.CinemaName
                    : null,

                roomId = s.RoomId,

                roomName = s.Room != null
                    ? s.Room.RoomName
                    : null,

                startTime = s.Date.HasValue && s.StartHour.HasValue
                    ? s.Date.Value.ToDateTime(s.StartHour.Value)
                    : (DateTime?)null,

                basePrice = s.BasePrice,
                status = s.Status
            })
            .FirstOrDefaultAsync();

        if (showtime == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy suất chiếu."
            });
        }

        return Ok(new
        {
            success = true,
            data = showtime,
            message = "Lấy chi tiết suất chiếu thành công."
        });
    }

    // GET: api/showtimes/1/seats
    [HttpGet("{showtimeId:int}/seats")]
    public async Task<IActionResult> GetShowtimeSeats(int showtimeId)
    {
        // Check showtime existence
        var exists = await _context.Showtimes
            .AnyAsync(s => s.ShowtimeId == showtimeId);

        if (!exists)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy suất chiếu."
            });
        }

        var now = DateTime.UtcNow;

        // Get seats and calculate their actual status
        var seats = await _context.ShowtimeSeats
            .AsNoTracking()
            .Where(ss =>
                ss.ShowtimeId == showtimeId &&
                ss.Seat != null)
            .OrderBy(ss => ss.Seat!.Row)
            .ThenBy(ss => ss.Seat!.Column)
            .Select(ss => new
            {
                seatId = ss.SeatId,

                seatNumber =
                    $"{ss.Seat!.Row}{ss.Seat.Column}",

                // Priority:
                // 1. BOOKED: ticket already exists
                // 2. HELD: active and unexpired payment attempt
                // 3. Original seat status
                status = _context.Tickets.Any(t =>
                    t.ShowtimeSeatId == ss.ShowtimeSeatId &&
                    (t.Status == "VALID" || t.Status == "PRINTED"))
                    ? "BOOKED"

                    : _context.Tickets.Any(t =>
                        t.ShowtimeSeatId == ss.ShowtimeSeatId &&
                        t.Status == "HELD" &&
                        t.Booking != null &&
                        t.Booking.Status == "PENDING" &&
                        t.Booking.ExpiredAt > now)
                        ? "HELD"

                        : ss.Status,

                price = ss.Price
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                showtimeId,
                seats
            },
            message = "Lấy sơ đồ ghế thành công."
        });
    }
}
