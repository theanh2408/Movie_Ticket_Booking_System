
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

    public MoviesController(MovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMovie(
        [FromBody] CreateMovieRequest request,
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
}
