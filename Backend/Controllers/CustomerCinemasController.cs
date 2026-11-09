
using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/cinemas")]
public class CustomerCinemasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerCinemasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/cinemas
    [HttpGet]
    public async Task<IActionResult> GetCinemas(
        CancellationToken cancellationToken)
    {
        var cinemas = await _context.Cinemas
            .AsNoTracking()
            .Select(c => new
            {
                cinemaId = c.CinemaId,
                cinemaName = c.CinemaName,
                address = c.Address,
                status = c.Status
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = cinemas,
            message = "Lấy danh sách rạp thành công."
        });
    }


    // GET: /api/cinemas/{cinemaId}
    [HttpGet("{cinemaId:int}")]
    public async Task<IActionResult> GetCinemaById(
        int cinemaId,
        CancellationToken cancellationToken)
    {
        var cinema = await _context.Cinemas
            .AsNoTracking()
            .Where(c => c.CinemaId == cinemaId)
            .Select(c => new
            {
                cinemaId = c.CinemaId,
                cinemaName = c.CinemaName,
                address = c.Address,
                status = c.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cinema == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy rạp."
            });
        }

        return Ok(new
        {
            success = true,
            data = cinema,
            message = "Lấy thông tin rạp thành công."
        });
    }

    // GET: /api/cinemas/{cinemaId}/rooms
    [HttpGet("{cinemaId:int}/rooms")]
    public async Task<IActionResult> GetRoomsByCinema(
        int cinemaId,
        CancellationToken cancellationToken)
    {
        var cinemaExists = await _context.Cinemas
            .AnyAsync(c => c.CinemaId == cinemaId, cancellationToken);

        if (!cinemaExists)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy rạp."
            });
        }

        var rooms = await _context.Rooms
            .AsNoTracking()
            .Where(r => r.CinemaId == cinemaId)
            .Select(r => new
            {
                roomId = r.RoomId,
                roomName = r.RoomName,
                status = r.Status,
                cinemaId = r.CinemaId
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = rooms,
            message = "Lấy danh sách phòng chiếu thành công."
        });
    }
}
