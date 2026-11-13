using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/showtimes")]
[Authorize(Roles = "ADMIN")]
public class AdminShowtimesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminShowtimesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllShowtimes(CancellationToken cancellationToken)
    {
        var showtimes = await _context.Showtimes
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
                .ThenInclude(r => r!.Cinema)
            .OrderByDescending(s => s.Date)
            .ThenByDescending(s => s.StartHour)
            .Select(s => new
            {
                showtimeId = s.ShowtimeId,
                movieId = s.MovieId,
                movieTitle = s.Movie != null ? s.Movie.Title : null,
                roomId = s.RoomId,
                roomName = s.Room != null ? s.Room.RoomName : null,
                cinemaId = s.Room != null ? s.Room.CinemaId : null,
                cinemaName = s.Room != null && s.Room.Cinema != null ? s.Room.Cinema.CinemaName : null,
                date = s.Date,
                startHour = s.StartHour,
                basePrice = s.BasePrice,
                status = s.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = showtimes,
            message = "Lấy danh sách suất chiếu thành công."
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateShowtime([FromBody] CreateShowtimeRequest request, CancellationToken cancellationToken)
    {
        var movieExists = await _context.Movies.AnyAsync(m => m.MovieId == request.MovieId, cancellationToken);
        if (!movieExists)
            return BadRequest(new { success = false, message = "Phim không tồn tại." });

        var room = await _context.Rooms
            .Include(r => r.Seats)
            .FirstOrDefaultAsync(r => r.RoomId == request.RoomId, cancellationToken);

        if (room == null)
            return BadRequest(new { success = false, message = "Phòng chiếu không tồn tại." });

        var showtime = new Showtime
        {
            MovieId = request.MovieId,
            RoomId = request.RoomId,
            Date = request.Date,
            StartHour = request.StartHour,
            BasePrice = request.BasePrice,
            Status = "ACTIVE"
        };

        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync(cancellationToken);

        // Tự động sinh danh sách ShowtimeSeats tương ứng với các ghế trong phòng
        var showtimeSeats = room.Seats.Select(seat => new ShowtimeSeat
        {
            ShowtimeId = showtime.ShowtimeId,
            SeatId = seat.SeatId,
            Price = request.BasePrice,
            Status = "AVAILABLE"
        }).ToList();

        _context.ShowtimeSeats.AddRange(showtimeSeats);
        await _context.SaveChangesAsync(cancellationToken);

        return StatusCode(201, new
        {
            success = true,
            data = new
            {
                showtimeId = showtime.ShowtimeId,
                movieId = showtime.MovieId,
                roomId = showtime.RoomId,
                date = showtime.Date,
                startHour = showtime.StartHour,
                basePrice = showtime.BasePrice,
                totalSeats = showtimeSeats.Count
            },
            message = "Tạo suất chiếu và khởi tạo ghế thành công."
        });
    }

    [HttpDelete("{showtimeId:int}")]
    public async Task<IActionResult> CancelShowtime(int showtimeId, CancellationToken cancellationToken)
    {
        var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId, cancellationToken);
        if (showtime == null)
            return NotFound(new { success = false, message = "Không tìm thấy suất chiếu." });

        if (showtime.Status == "CANCELLED")
            return BadRequest(new { success = false, message = "Suất chiếu này đã bị hủy từ trước." });

        // Nghiệp vụ: Không thể hủy suất chiếu nếu đã có khách thanh toán vé thành công
        var hasPaidTickets = await _context.Tickets
            .Include(t => t.ShowtimeSeat)
            .Include(t => t.Booking)
            .AnyAsync(t => t.ShowtimeSeat!.ShowtimeId == showtimeId && t.Booking!.Status == "PAID", cancellationToken);

        if (hasPaidTickets)
        {
            return BadRequest(new
            {
                success = false,
                message = "Không thể hủy suất chiếu này vì đã có khách hàng mua vé thành công."
            });
        }

        // Hủy các đơn pending đang giữ chỗ (nếu có)
        var pendingTickets = await _context.Tickets
            .Include(t => t.ShowtimeSeat)
            .Include(t => t.Booking)
            .Where(t => t.ShowtimeSeat!.ShowtimeId == showtimeId && t.Booking!.Status == "PENDING")
            .ToListAsync(cancellationToken);

        foreach (var pt in pendingTickets)
        {
            if (pt.Booking != null) pt.Booking.Status = "CANCELLED";
        }
        _context.Tickets.RemoveRange(pendingTickets);

        showtime.Status = "CANCELLED";
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new { showtimeId, status = showtime.Status },
            message = "Đã hủy suất chiếu thành công."
        });
    }
}
