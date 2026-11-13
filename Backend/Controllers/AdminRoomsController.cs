using Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/rooms")]
[Authorize(Roles = "ADMIN")]
public class AdminRoomsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AdminRoomsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRooms(CancellationToken cancellationToken)
    {
        var rooms = await _context.Rooms
            .AsNoTracking()
            .Include(r => r.Cinema)
            .Include(r => r.Seats)
            .Select(r => new
            {
                roomId = r.RoomId,
                roomName = r.RoomName,
                cinemaId = r.CinemaId,
                cinemaName = r.Cinema != null ? r.Cinema.CinemaName : null,
                totalSeats = r.Seats.Count,
                status = r.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = rooms,
            message = "Lấy danh sách phòng chiếu thành công."
        });
    }

    [HttpGet("genres")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGenres(CancellationToken cancellationToken)
    {
        var genres = await _context.Genres
            .AsNoTracking()
            .Select(g => new
            {
                genreId = g.GenreId,
                genreName = g.GenreName
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = genres,
            message = "Lấy danh sách thể loại thành công."
        });
    }
}
