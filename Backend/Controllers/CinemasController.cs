using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CinemasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CinemasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/cinemas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCinemas()
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
            .ToListAsync();

        return Ok(ApiResponse.Ok(cinemas, "Lấy danh sách rạp thành công"));
    }

    /// <summary>
    /// GET /api/cinemas/{cinemaId}
    /// </summary>
    [HttpGet("{cinemaId:int}")]
    public async Task<IActionResult> GetCinemaById(int cinemaId)
    {
        var cinema = await _context.Cinemas
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CinemaId == cinemaId);

        if (cinema == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy rạp với ID đã cung cấp"));
        }

        var result = new
        {
            cinemaId = cinema.CinemaId,
            cinemaName = cinema.CinemaName,
            address = cinema.Address,
            status = cinema.Status
        };

        return Ok(ApiResponse.Ok(result, "Lấy thông tin rạp thành công"));
    }

    /// <summary>
    /// GET /api/cinemas/{cinemaId}/rooms
    /// </summary>
    [HttpGet("{cinemaId:int}/rooms")]
    public async Task<IActionResult> GetRoomsByCinemaId(int cinemaId)
    {
        var cinemaExists = await _context.Cinemas.AnyAsync(c => c.CinemaId == cinemaId);
        if (!cinemaExists)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy rạp với ID đã cung cấp"));
        }

        var rooms = await _context.Rooms
            .AsNoTracking()
            .Where(r => r.CinemaId == cinemaId)
            .Select(r => new
            {
                roomId = r.RoomId,
                cinemaId = r.CinemaId,
                roomName = r.RoomName,
                status = r.Status
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(rooms, "Lấy danh sách phòng chiếu thành công"));
    }
}
