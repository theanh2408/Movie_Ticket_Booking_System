using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShowtimesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShowtimesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/showtimes?movieId=&cinemaId=&date=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetShowtimes(
        [FromQuery] int? movieId,
        [FromQuery] int? cinemaId,
        [FromQuery] string? date)
    {
        var query = _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r!.Cinema)
            .AsNoTracking()
            .AsQueryable();

        if (movieId.HasValue)
        {
            query = query.Where(s => s.MovieId == movieId.Value);
        }

        if (cinemaId.HasValue)
        {
            query = query.Where(s => s.Room != null && s.Room.CinemaId == cinemaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsedDate))
        {
            query = query.Where(s => s.Date == parsedDate);
        }

        var showtimes = await query
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartHour)
            .Select(s => new
            {
                showtimeId = s.ShowtimeId,
                movieId = s.MovieId,
                movieTitle = s.Movie != null ? s.Movie.Title : null,
                posterUrl = s.Movie != null ? s.Movie.PosterUrl : null,
                cinemaId = s.Room != null ? s.Room.CinemaId : null,
                cinemaName = s.Room != null && s.Room.Cinema != null ? s.Room.Cinema.CinemaName : null,
                roomId = s.RoomId,
                roomName = s.Room != null ? s.Room.RoomName : null,
                date = s.Date.HasValue ? s.Date.Value.ToString("yyyy-MM-dd") : null,
                startTime = s.StartHour.HasValue ? s.StartHour.Value.ToString("HH:mm") : null,
                startHour = s.StartHour.HasValue ? s.StartHour.Value.ToString("HH:mm") : null,
                basePrice = s.BasePrice,
                status = s.Status
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(showtimes, "Lấy danh sách suất chiếu thành công"));
    }

    /// <summary>
    /// GET /api/showtimes/{showtimeId}
    /// </summary>
    [HttpGet("{showtimeId:int}")]
    public async Task<IActionResult> GetShowtimeById(int showtimeId)
    {
        var showtime = await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r!.Cinema)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

        if (showtime == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy suất chiếu với ID đã cung cấp"));
        }

        var result = new
        {
            showtimeId = showtime.ShowtimeId,
            movieId = showtime.MovieId,
            movieTitle = showtime.Movie != null ? showtime.Movie.Title : null,
            posterUrl = showtime.Movie != null ? showtime.Movie.PosterUrl : null,
            cinemaId = showtime.Room != null ? showtime.Room.CinemaId : null,
            cinemaName = showtime.Room != null && showtime.Room.Cinema != null ? showtime.Room.Cinema.CinemaName : null,
            roomId = showtime.RoomId,
            roomName = showtime.Room != null ? showtime.Room.RoomName : null,
            date = showtime.Date.HasValue ? showtime.Date.Value.ToString("yyyy-MM-dd") : null,
            startTime = showtime.StartHour.HasValue ? showtime.StartHour.Value.ToString("HH:mm") : null,
            startHour = showtime.StartHour.HasValue ? showtime.StartHour.Value.ToString("HH:mm") : null,
            basePrice = showtime.BasePrice,
            status = showtime.Status
        };

        return Ok(ApiResponse.Ok(result, "Lấy chi tiết suất chiếu thành công"));
    }

    /// <summary>
    /// GET /api/showtimes/{showtimeId}/seats
    /// </summary>
    [HttpGet("{showtimeId:int}/seats")]
    public async Task<IActionResult> GetShowtimeSeats(int showtimeId)
    {
        var showtime = await _context.Showtimes
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

        if (showtime == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy suất chiếu với ID đã cung cấp"));
        }

        var showtimeSeats = await _context.ShowtimeSeats
            .Include(ss => ss.Seat)
            .AsNoTracking()
            .Where(ss => ss.ShowtimeId == showtimeId)
            .ToListAsync();

        var seatList = showtimeSeats
            .OrderBy(ss => ss.Seat?.Row)
            .ThenBy(ss => int.TryParse(ss.Seat?.Column, out var c) ? c : 0)
            .Select(ss => new
            {
                seatId = ss.SeatId ?? ss.ShowtimeSeatId,
                showtimeSeatId = ss.ShowtimeSeatId,
                seatNumber = $"{ss.Seat?.Row}{ss.Seat?.Column}",
                status = ss.Status ?? "AVAILABLE",
                price = ss.Price ?? showtime.BasePrice
            })
            .ToList();

        var result = new
        {
            showtimeId = showtime.ShowtimeId,
            seats = seatList
        };

        return Ok(ApiResponse.Ok(result, "Lấy sơ đồ ghế thành công"));
    }
}
