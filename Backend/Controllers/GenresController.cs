using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly AppDbContext _context;

    public GenresController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/genres
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await _context.Genres
            .AsNoTracking()
            .Select(g => new
            {
                genreId = g.GenreId,
                name = g.GenreName
            })
            .ToListAsync();

        return Ok(ApiResponse.Ok(genres, "Lấy danh sách thể loại thành công"));
    }
}
