using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/movies")]
public class CustomerMoviesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerMoviesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/genres và /api/movies/genres
    [HttpGet("/api/genres")]
    [HttpGet("genres")]
    public async Task<IActionResult> GetGenres(CancellationToken cancellationToken)
    {
        var genres = await _context.Genres
            .AsNoTracking()
            .Select(g => new
            {
                genreId = g.GenreId,
                genreName = g.GenreName,
                name = g.GenreName
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = genres,
            message = "Lấy danh sách thể loại thành công."
        });
    }

    // GET: /api/movies
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? name,
        [FromQuery] string? status,
        [FromQuery] int? genreId,
        CancellationToken cancellationToken)
    {
        var query = _context.Movies
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(m => m.Title != null && m.Title.Contains(name.Trim()));

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusUpper = status.Trim().ToUpperInvariant();
            if (statusUpper is "UPCOMING" or "COMING_SOON")
            {
                query = query.Where(m => m.Status == "UPCOMING" || m.Status == "COMING_SOON");
            }
            else
            {
                query = query.Where(m => m.Status == statusUpper);
            }
        }

        if (genreId.HasValue)
            query = query.Where(m => m.Genres.Any(g => g.GenreId == genreId.Value));

        var movies = await query
            .Select(m => new
            {
                movieId = m.MovieId,
                title = m.Title,
                description = m.Description,
                duration = m.Duration,
                posterUrl = m.PosterUrl,
                trailerUrl = m.TrailerUrl,
                ageRating = m.AgeRating,
                releaseDate = m.ReleaseDate,
                status = m.Status == "COMING_SOON" ? "UPCOMING" : m.Status,
                genres = m.Genres.Select(g => new
                {
                    genreId = g.GenreId,
                    genreName = g.GenreName,
                    name = g.GenreName
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                items = movies,
                totalItems = movies.Count,
                page = 1,
                pageSize = movies.Count,
                totalPages = 1
            },
            message = "Lấy danh sách phim thành công."
        });
    }

    // GET: /api/movies/{movieId}
    [HttpGet("{movieId:int}")]
    public async Task<IActionResult> GetMovieById(
        int movieId,
        CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .Where(m => m.MovieId == movieId)
            .Select(m => new
            {
                movieId = m.MovieId,
                title = m.Title,
                description = m.Description,
                duration = m.Duration,
                posterUrl = m.PosterUrl,
                trailerUrl = m.TrailerUrl,
                ageRating = m.AgeRating,
                releaseDate = m.ReleaseDate,
                status = m.Status == "COMING_SOON" ? "UPCOMING" : m.Status,
                genres = m.Genres.Select(g => new
                {
                    genreId = g.GenreId,
                    genreName = g.GenreName,
                    name = g.GenreName
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (movie == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = "Không tìm thấy phim."
            });
        }

        return Ok(new
        {
            success = true,
            data = movie,
            message = "Lấy thông tin phim thành công."
        });
    }
}
