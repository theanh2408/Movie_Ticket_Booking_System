using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// GET /api/movies?search=&genreId=&status=&page=1&pageSize=10
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        [FromQuery] string? search,
        [FromQuery] int? genreId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .AsQueryable();

        // Lọc theo search (tiêu đề phim)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(m => m.Title != null && m.Title.ToLower().Contains(searchLower));
        }

        // Lọc theo genreId
        if (genreId.HasValue)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
        }

        // Lọc theo status (NOW_SHOWING, COMING_SOON, etc.)
        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusUpper = status.Trim().ToUpper();
            query = query.Where(m => m.Status != null && m.Status.ToUpper() == statusUpper);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var movies = await query
            .OrderByDescending(m => m.ReleaseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                movieId = m.MovieId,
                title = m.Title,
                description = m.Description,
                durationMinutes = m.Duration,
                posterUrl = m.PosterUrl,
                trailerUrl = m.TrailerUrl,
                ageRating = m.AgeRating,
                releaseDate = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.ToString("yyyy-MM-dd") : null,
                status = m.Status,
                genres = m.MovieGenres.Select(mg => new
                {
                    genreId = mg.GenreId,
                    name = mg.Genre != null ? mg.Genre.GenreName : null
                }).ToList()
            })
            .ToListAsync();

        var result = new
        {
            items = movies,
            page,
            pageSize,
            totalItems,
            totalPages
        };

        return Ok(ApiResponse.Ok(result, "Lấy danh sách phim thành công"));
    }

    /// <summary>
    /// GET /api/movies/{movieId}
    /// </summary>
    [HttpGet("{movieId:int}")]
    public async Task<IActionResult> GetMovieById(int movieId)
    {
        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MovieId == movieId);

        if (movie == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy phim với ID đã cung cấp"));
        }

        var movieDetail = new
        {
            movieId = movie.MovieId,
            title = movie.Title,
            description = movie.Description,
            durationMinutes = movie.Duration,
            posterUrl = movie.PosterUrl,
            trailerUrl = movie.TrailerUrl,
            ageRating = movie.AgeRating,
            releaseDate = movie.ReleaseDate.HasValue ? movie.ReleaseDate.Value.ToString("yyyy-MM-dd") : null,
            status = movie.Status,
            genres = movie.MovieGenres.Select(mg => new
            {
                genreId = mg.GenreId,
                name = mg.Genre != null ? mg.Genre.GenreName : null
            }).ToList()
        };

        return Ok(ApiResponse.Ok(movieDetail, "Lấy chi tiết phim thành công"));
    }
}
