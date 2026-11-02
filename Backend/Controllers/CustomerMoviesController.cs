
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

    // GET: /api/movies
    [HttpGet]
    public async Task<IActionResult> GetMovies(
        CancellationToken cancellationToken)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Include(m => m.Genres)
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
                status = m.Status,
                genres = m.Genres.Select(g => new
                {
                    genreId = g.GenreId,
                    genreName = g.GenreName
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = movies,
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
                status = m.Status,
                genres = m.Genres.Select(g => new
                {
                    genreId = g.GenreId,
                    genreName = g.GenreName
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
