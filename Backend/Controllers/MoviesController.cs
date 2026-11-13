using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/movies")]
[Authorize(Roles = "ADMIN")]
public class MoviesController : ControllerBase
{
    private readonly MovieService _movieService;
    private readonly ApplicationDbContext _context;

    public MoviesController(MovieService movieService, ApplicationDbContext context)
    {
        _movieService = movieService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMovies(CancellationToken cancellationToken)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Include(m => m.Genres)
            .OrderByDescending(m => m.MovieId)
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
                genres = m.Genres.Select(g => new { genreId = g.GenreId, genreName = g.GenreName }).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = movies,
            message = "Lấy danh sách phim thành công."
        });
    }

    [HttpGet("{movieId:int}")]
    public async Task<IActionResult> GetMovie(int movieId, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .AsNoTracking()
            .Include(m => m.Genres)
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
                genres = m.Genres.Select(g => new { genreId = g.GenreId, genreName = g.GenreName }).ToList(),
                genreIds = m.Genres.Select(g => g.GenreId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (movie == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phim." });
        }

        return Ok(new
        {
            success = true,
            data = movie,
            message = "Lấy chi tiết phim thành công."
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateMovie(
        [FromBody] CreateMovieRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var movie = await _movieService.CreateAsync(request, cancellationToken);

            return Created(
                $"/api/admin/movies/{movie.MovieId}",
                new
                {
                    success = true,
                    data = new
                    {
                        movieId = movie.MovieId,
                        title = movie.Title,
                        description = movie.Description,
                        duration = movie.Duration,
                        posterUrl = movie.PosterUrl,
                        trailerUrl = movie.TrailerUrl,
                        ageRating = movie.AgeRating,
                        releaseDate = movie.ReleaseDate,
                        status = movie.Status,
                        genreIds = request.GenreIds.Distinct().ToList()
                    },
                    message = "Thêm phim thành công."
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                success = false,
                data = (object?)null,
                message = ex.Message
            });
        }
    }

    [HttpPut("{movieId:int}")]
    public async Task<IActionResult> UpdateMovie(
        int movieId,
        [FromBody] CreateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var movie = await _context.Movies
            .Include(m => m.Genres)
            .FirstOrDefaultAsync(m => m.MovieId == movieId, cancellationToken);

        if (movie == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phim để cập nhật." });
        }

        movie.Title = request.Title?.Trim();
        movie.Description = request.Description?.Trim();
        movie.Duration = request.Duration;
        movie.PosterUrl = request.PosterUrl?.Trim();
        movie.TrailerUrl = request.TrailerUrl?.Trim();
        movie.AgeRating = request.AgeRating?.Trim();
        movie.ReleaseDate = request.ReleaseDate;
        movie.Status = request.Status?.Trim() ?? movie.Status;

        if (request.GenreIds != null && request.GenreIds.Count > 0)
        {
            var genres = await _context.Genres
                .Where(g => request.GenreIds.Contains(g.GenreId))
                .ToListAsync(cancellationToken);
            movie.Genres = genres;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                movieId = movie.MovieId,
                title = movie.Title,
                description = movie.Description,
                duration = movie.Duration,
                posterUrl = movie.PosterUrl,
                trailerUrl = movie.TrailerUrl,
                ageRating = movie.AgeRating,
                releaseDate = movie.ReleaseDate,
                status = movie.Status
            },
            message = "Cập nhật phim thành công."
        });
    }

    [HttpDelete("{movieId:int}")]
    public async Task<IActionResult> DeleteMovie(int movieId, CancellationToken cancellationToken)
    {
        var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == movieId, cancellationToken);
        if (movie == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy phim." });
        }

        // Soft delete / Chuyển trạng thái sang ENDED để không ảnh hưởng dữ liệu đặt vé lịch sử
        movie.Status = "ENDED";
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new { movieId, status = movie.Status },
            message = "Đã chuyển trạng thái phim sang kết thúc (ENDED)."
        });
    }
}
