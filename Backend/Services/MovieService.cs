
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public class MovieService
{
    private readonly MovieRepository _movieRepository;

    public MovieService(MovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<Movie> CreateAsync(
        CreateMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim();
        var posterUrl = request.PosterUrl?.Trim();
        var trailerUrl = request.TrailerUrl?.Trim();
        var ageRating = request.AgeRating?.Trim();
        var status = request.Status?.Trim();

        if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            throw new ArgumentException("Tên phim không hợp lệ.");

        if (!request.Duration.HasValue || request.Duration.Value <= 0)
            throw new ArgumentException("Thời lượng phải lớn hơn 0.");

        if (!IsValidHttpUrl(posterUrl))
            throw new ArgumentException("PosterURL không hợp lệ.");

        if (!IsValidHttpUrl(trailerUrl))
            throw new ArgumentException("TrailerURL không hợp lệ.");

        var allowedRatings = new[] { "P", "K", "T13", "T16", "T18" };

        if (string.IsNullOrWhiteSpace(ageRating) ||
            !allowedRatings.Contains(ageRating))
            throw new ArgumentException("AgeRating không hợp lệ.");

        var allowedStatuses = new[]
        {
            "UPCOMING",
            "NOW_SHOWING",
            "ENDED"
        };

        if (string.IsNullOrWhiteSpace(status) ||
            !allowedStatuses.Contains(status))
            throw new ArgumentException("Status không hợp lệ.");

        if (posterUrl.Length > 500 || trailerUrl.Length > 500)
            throw new ArgumentException("URL không được vượt quá 500 ký tự.");

        var movie = new Movie
        {
            Title = title,
            Description = request.Description?.Trim(),
            Duration = request.Duration.Value,
            PosterUrl = posterUrl,
            TrailerUrl = trailerUrl,
            AgeRating = ageRating,
            ReleaseDate = request.ReleaseDate,
            Status = status
        };

        return await _movieRepository.AddAsync(
            movie,
            request.GenreIds,
            cancellationToken);
    }

    private static bool IsValidHttpUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 500
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
