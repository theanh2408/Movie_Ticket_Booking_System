using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public class MovieService
{
    private readonly MovieRepository _movieRepository;
    private readonly IFileStorageService _fileStorageService;

    public MovieService(
        MovieRepository movieRepository,
        IFileStorageService fileStorageService)
    {
        _movieRepository = movieRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Movie> CreateAsync(
        CreateMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title?.Trim();
        var ageRating = request.AgeRating?.Trim();
        var status = request.Status?.Trim();

        if (string.IsNullOrWhiteSpace(title) || title.Length > 255)
            throw new ArgumentException("Tên phim không hợp lệ.");

        if (!request.Duration.HasValue || request.Duration.Value <= 0)
            throw new ArgumentException("Thời lượng phải lớn hơn 0.");

        var allowedRatings = new[] { "P", "K", "T13", "T16", "T18", "C13", "C16", "C18" };
        if (string.IsNullOrWhiteSpace(ageRating) || !allowedRatings.Contains(ageRating))
            throw new ArgumentException("AgeRating không hợp lệ.");

        var allowedStatuses = new[] { "UPCOMING", "NOW_SHOWING", "ENDED", "COMING_SOON" };
        if (string.IsNullOrWhiteSpace(status) || !allowedStatuses.Contains(status))
            throw new ArgumentException("Status không hợp lệ.");

        if (status == "COMING_SOON")
        {
            status = "UPCOMING";
        }

        string? savedPosterUrl = null;
        string? savedTrailerUrl = null;

        try
        {
            // 1. Xử lý Poster: Ưu tiên file upload, sau đó tới URL truyền vào
            if (request.PosterFile != null)
            {
                savedPosterUrl = await _fileStorageService.SavePosterAsync(
                    request.PosterFile, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.PosterUrl))
            {
                var url = request.PosterUrl.Trim();
                if (!IsValidUrl(url))
                    throw new ArgumentException("PosterURL không đúng định dạng URL hợp lệ.");
                savedPosterUrl = url;
            }
            else
            {
                throw new ArgumentException("Vui lòng cung cấp file ảnh Poster hoặc đường dẫn PosterURL.");
            }

            // 2. Xử lý Trailer: Ưu tiên file upload, sau đó tới URL truyền vào
            if (request.TrailerFile != null)
            {
                savedTrailerUrl = await _fileStorageService.SaveTrailerAsync(
                    request.TrailerFile, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.TrailerUrl))
            {
                var url = request.TrailerUrl.Trim();
                if (!IsValidUrl(url))
                    throw new ArgumentException("TrailerURL không đúng định dạng URL hợp lệ.");
                savedTrailerUrl = url;
            }
            else
            {
                throw new ArgumentException("Vui lòng cung cấp file video Trailer hoặc đường dẫn TrailerURL.");
            }

            var movie = new Movie
            {
                Title = title,
                Description = request.Description?.Trim(),
                Duration = request.Duration.Value,
                PosterUrl = savedPosterUrl,
                TrailerUrl = savedTrailerUrl,
                AgeRating = ageRating,
                ReleaseDate = request.ReleaseDate,
                Status = status
            };

            return await _movieRepository.AddAsync(
                movie,
                request.GenreIds,
                cancellationToken);
        }
        catch
        {
            // Nếu có lỗi trong quá trình lưu DB, dọn dẹp các file vừa tải lên để tránh rác server
            if (savedPosterUrl != null && savedPosterUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                _fileStorageService.DeleteFile(savedPosterUrl);
            }

            if (savedTrailerUrl != null && savedTrailerUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                _fileStorageService.DeleteFile(savedTrailerUrl);
            }

            throw;
        }
    }

    public async Task<Movie?> UpdateAsync(
        int movieId,
        UpdateMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        var movie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            return null;
        }

        var oldPosterUrl = movie.PosterUrl;
        var oldTrailerUrl = movie.TrailerUrl;

        string? newPosterUploaded = null;
        string? newTrailerUploaded = null;

        try
        {
            // 1. Cập nhật thông tin cơ bản nếu có truyền vào
            if (!string.IsNullOrWhiteSpace(request.Title))
                movie.Title = request.Title.Trim();

            if (request.Description != null)
                movie.Description = request.Description.Trim();

            if (request.Duration.HasValue && request.Duration.Value > 0)
                movie.Duration = request.Duration.Value;

            if (!string.IsNullOrWhiteSpace(request.AgeRating))
            {
                var allowedRatings = new[] { "P", "K", "T13", "T16", "T18", "C13", "C16", "C18" };
                if (!allowedRatings.Contains(request.AgeRating.Trim()))
                    throw new ArgumentException("AgeRating không hợp lệ.");
                movie.AgeRating = request.AgeRating.Trim();
            }

            if (request.ReleaseDate.HasValue)
                movie.ReleaseDate = request.ReleaseDate.Value;

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var statusUpper = request.Status.Trim().ToUpperInvariant();
                movie.Status = statusUpper == "COMING_SOON" ? "UPCOMING" : statusUpper;
            }

            // 2. Xử lý Poster cập nhật
            if (request.PosterFile != null)
            {
                newPosterUploaded = await _fileStorageService.SavePosterAsync(
                    request.PosterFile, cancellationToken);
                movie.PosterUrl = newPosterUploaded;
            }
            else if (!string.IsNullOrWhiteSpace(request.PosterUrl))
            {
                var url = request.PosterUrl.Trim();
                if (!IsValidUrl(url))
                    throw new ArgumentException("PosterURL không đúng định dạng URL hợp lệ.");
                movie.PosterUrl = url;
            }

            // 3. Xử lý Trailer cập nhật
            if (request.TrailerFile != null)
            {
                newTrailerUploaded = await _fileStorageService.SaveTrailerAsync(
                    request.TrailerFile, cancellationToken);
                movie.TrailerUrl = newTrailerUploaded;
            }
            else if (!string.IsNullOrWhiteSpace(request.TrailerUrl))
            {
                var url = request.TrailerUrl.Trim();
                if (!IsValidUrl(url))
                    throw new ArgumentException("TrailerURL không đúng định dạng URL hợp lệ.");
                movie.TrailerUrl = url;
            }

            var updatedMovie = await _movieRepository.UpdateAsync(
                movie,
                request.GenreIds,
                cancellationToken);

            // 4. Nếu cập nhật DB thành công, tiến hành xóa file cũ vật lý đã bị thay thế
            if (newPosterUploaded != null ||
                (!string.IsNullOrWhiteSpace(request.PosterUrl) && request.PosterUrl != oldPosterUrl))
            {
                _fileStorageService.DeleteFile(oldPosterUrl);
            }

            if (newTrailerUploaded != null ||
                (!string.IsNullOrWhiteSpace(request.TrailerUrl) && request.TrailerUrl != oldTrailerUrl))
            {
                _fileStorageService.DeleteFile(oldTrailerUrl);
            }

            return updatedMovie;
        }
        catch
        {
            // Dọn dẹp file mới vừa upload nếu DB update thất bại
            if (newPosterUploaded != null)
            {
                _fileStorageService.DeleteFile(newPosterUploaded);
            }

            if (newTrailerUploaded != null)
            {
                _fileStorageService.DeleteFile(newTrailerUploaded);
            }

            throw;
        }
    }

    public async Task<bool> DeleteAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var movie = await _movieRepository.GetByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            return false;
        }

        if (movie.Showtimes != null && movie.Showtimes.Any())
        {
            throw new InvalidOperationException(
                "Không thể xóa phim này vì đã có lịch chiếu trong hệ thống. Vui lòng chuyển trạng thái phim thành ENDED thay vì xóa.");
        }

        var posterToDelete = movie.PosterUrl;
        var trailerToDelete = movie.TrailerUrl;

        await _movieRepository.DeleteAsync(movie, cancellationToken);

        // Xóa file vật lý tương ứng trên server nếu là file cục bộ
        _fileStorageService.DeleteFile(posterToDelete);
        _fileStorageService.DeleteFile(trailerToDelete);

        return true;
    }

    public async Task<Movie?> GetByIdAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _movieRepository.GetByIdAsync(movieId, cancellationToken);
    }

    private static bool IsValidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 500)
            return false;

        // Cho phép đường dẫn tương đối nội bộ /uploads/...
        if (value.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Cho phép đường dẫn HTTP/HTTPS tuyệt đối
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
