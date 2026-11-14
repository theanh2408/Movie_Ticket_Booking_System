using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/admin/movies")]
[Authorize(Roles = "ADMIN")]
public class MoviesController : ControllerBase
{
    private readonly MovieService _movieService;
    private readonly ILogger<MoviesController> _logger;

    public MoviesController(
        MovieService movieService,
        ILogger<MoviesController> logger)
    {
        _movieService = movieService;
        _logger = logger;
    }

    /// <summary>
    /// Thêm phim mới kèm upload poster (tối đa 5MB) và trailer (tối đa 100MB)
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110 * 1024 * 1024)] // 110 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    public async Task<IActionResult> CreateMovie(
        [FromForm] CreateMovieRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var movie = await _movieService.CreateAsync(
                request,
                cancellationToken);

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
                        genres = movie.Genres.Select(g => new
                        {
                            genreId = g.GenreId,
                            genreName = g.GenreName,
                            name = g.GenreName
                        }).ToList()
                    },
                    message = "Thêm phim mới thành công."
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xảy ra khi thêm phim mới.");
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Đã xảy ra lỗi trong quá trình xử lý lưu phim."
            });
        }
    }

    /// <summary>
    /// Cập nhật thông tin phim hoặc thay đổi file poster/trailer mới
    /// </summary>
    [HttpPut("{movieId:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(110 * 1024 * 1024)] // 110 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    public async Task<IActionResult> UpdateMovie(
        int movieId,
        [FromForm] UpdateMovieRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var movie = await _movieService.UpdateAsync(
                movieId,
                request,
                cancellationToken);

            if (movie == null)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    message = $"Không tìm thấy phim với mã ID {movieId}."
                });
            }

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
                    status = movie.Status,
                    genres = movie.Genres.Select(g => new
                    {
                        genreId = g.GenreId,
                        genreName = g.GenreName,
                        name = g.GenreName
                    }).ToList()
                },
                message = "Cập nhật thông tin phim thành công."
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi cập nhật phim ID {MovieId}.", movieId);
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Đã xảy ra lỗi trong quá trình cập nhật phim."
            });
        }
    }

    /// <summary>
    /// Lấy chi tiết thông tin phim dành cho Admin
    /// </summary>
    [HttpGet("{movieId:int}")]
    public async Task<IActionResult> GetMovieById(
        int movieId,
        CancellationToken cancellationToken)
    {
        var movie = await _movieService.GetByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            return NotFound(new
            {
                success = false,
                data = (object?)null,
                message = $"Không tìm thấy phim với mã ID {movieId}."
            });
        }

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
                status = movie.Status,
                genres = movie.Genres.Select(g => new
                {
                    genreId = g.GenreId,
                    genreName = g.GenreName,
                    name = g.GenreName
                }).ToList()
            },
            message = "Lấy thông tin phim thành công."
        });
    }

    /// <summary>
    /// Xóa phim và xóa các file vật lý liên quan (nếu chưa có lịch chiếu)
    /// </summary>
    [HttpDelete("{movieId:int}")]
    public async Task<IActionResult> DeleteMovie(
        int movieId,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _movieService.DeleteAsync(movieId, cancellationToken);
            if (!success)
            {
                return NotFound(new
                {
                    success = false,
                    data = (object?)null,
                    message = $"Không tìm thấy phim với mã ID {movieId}."
                });
            }

            return Ok(new
            {
                success = true,
                data = (object?)null,
                message = "Xóa phim và dọn dẹp file vật lý thành công."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                success = false,
                data = (object?)null,
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xóa phim ID {MovieId}.", movieId);
            return StatusCode(500, new
            {
                success = false,
                data = (object?)null,
                message = "Đã xảy ra lỗi khi xóa phim."
            });
        }
    }
}
