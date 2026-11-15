using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IMovieService
{
    Task<List<MovieResponseDto>> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating);
    Task<MovieResponseDto?> GetByIdAsync(int id);
    Task<MovieResponseDto> CreateAsync(CreateMovieDto dto);
    Task<MovieResponseDto> UpdateAsync(int id, CreateMovieDto dto);
    Task DeleteAsync(int id);
    Task<MovieResponseDto> UpdateStatusAsync(int id, string status);
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _repo;

    public MovieService(IMovieRepository repo) => _repo = repo;

    public async Task<List<MovieResponseDto>> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating)
    {
        var movies = await _repo.SearchAsync(keyword, status, genreId, ageRating);
        return movies.Select(ToDto).ToList();
    }

    public async Task<MovieResponseDto?> GetByIdAsync(int id)
    {
        var movie = await _repo.GetByIdAsync(id);
        return movie is null ? null : ToDto(movie);
    }

    // ===== THÊM PHIM (chức năng demo) =====
    public async Task<MovieResponseDto> CreateAsync(CreateMovieDto dto)
    {
        var title = dto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new BadRequestException("Tên phim không được để trống");

        if (await _repo.TitleExistsAsync(title))
            throw new ConflictException($"Phim \"{title}\" đã tồn tại");

        var genreIds = await ValidateGenresAsync(dto.GenreIds);

        var movie = new Movie
        {
            Title       = title,
            Description = dto.Description,
            Duration    = dto.Duration,
            PosterURL   = dto.PosterURL,
            TrailerURL  = dto.TrailerURL,
            AgeRating   = dto.AgeRating,
            ReleaseDate = dto.ReleaseDate,
            Status      = NormalizeStatus(dto.Status) ?? MovieStatus.NowShowing,
            MovieGenres = genreIds.Select(id => new MovieGenre { GenreID = id }).ToList()
        };

        await _repo.AddAsync(movie);

        // Lấy lại để trả về kèm tên thể loại
        return (await GetByIdAsync(movie.MovieID))!;
    }

    public async Task<MovieResponseDto> UpdateAsync(int id, CreateMovieDto dto)
    {
        var movie = await _repo.GetForUpdateAsync(id) ?? throw new NotFoundException("Không tìm thấy phim");

        var title = dto.Title.Trim();
        if (await _repo.TitleExistsAsync(title, id))
            throw new ConflictException($"Phim \"{title}\" đã tồn tại");

        var genreIds = await ValidateGenresAsync(dto.GenreIds);

        movie.Title = title;
        movie.Description = dto.Description;
        movie.Duration = dto.Duration;
        movie.PosterURL = dto.PosterURL;
        movie.TrailerURL = dto.TrailerURL;
        movie.AgeRating = dto.AgeRating;
        movie.ReleaseDate = dto.ReleaseDate;
        if (!string.IsNullOrWhiteSpace(dto.Status)) movie.Status = NormalizeStatus(dto.Status);

        // Cập nhật thể loại theo kiểu so sánh: xóa cái không còn chọn, thêm cái mới chọn
        var wanted = genreIds.ToHashSet();
        var toRemove = movie.MovieGenres.Where(mg => !wanted.Contains(mg.GenreID)).ToList();
        _repo.RemoveMovieGenres(toRemove);
        var existing = movie.MovieGenres.Select(mg => mg.GenreID).ToHashSet();
        foreach (var gid in wanted.Where(g => !existing.Contains(g)))
            movie.MovieGenres.Add(new MovieGenre { MovieID = id, GenreID = gid });

        await _repo.SaveAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task DeleteAsync(int id)
    {
        var movie = await _repo.GetForUpdateAsync(id) ?? throw new NotFoundException("Không tìm thấy phim");

        if (await _repo.HasShowtimesAsync(id))
            throw new ConflictException(
                "Phim đã có suất chiếu nên không thể xóa. Hãy chuyển trạng thái phim sang Inactive hoặc Ended.");

        await _repo.DeleteAsync(movie);
    }

    public async Task<MovieResponseDto> UpdateStatusAsync(int id, string status)
    {
        var newStatus = NormalizeStatus(status) ?? throw new BadRequestException("Trạng thái không hợp lệ");
        var movie = await _repo.GetForUpdateAsync(id) ?? throw new NotFoundException("Không tìm thấy phim");

        if (newStatus is MovieStatus.Ended or MovieStatus.Inactive && await _repo.HasUpcomingShowtimesAsync(id))
            throw new ConflictException("Phim còn suất chiếu sắp tới. Hãy hủy hoặc chuyển các suất chiếu đó trước.");

        movie.Status = newStatus;
        await _repo.SaveAsync();
        return (await GetByIdAsync(id))!;
    }

    // ---------- helpers ----------
    private async Task<List<int>> ValidateGenresAsync(List<int> ids)
    {
        var genreIds = ids.Distinct().ToList();
        if (genreIds.Count > 0 && await _repo.CountExistingGenresAsync(genreIds) != genreIds.Count)
            throw new BadRequestException("Có thể loại không tồn tại trong hệ thống");
        return genreIds;
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        var match = MovieStatus.All.FirstOrDefault(s => string.Equals(s, status.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new BadRequestException(
            $"Trạng thái phim không hợp lệ. Chỉ nhận: {string.Join(", ", MovieStatus.All)}");
    }

    private static MovieResponseDto ToDto(Movie m) => new()
    {
        MovieID     = m.MovieID,
        Title       = m.Title,
        Description = m.Description,
        Duration    = m.Duration,
        PosterURL   = m.PosterURL,
        TrailerURL  = m.TrailerURL,
        AgeRating   = m.AgeRating,
        ReleaseDate = m.ReleaseDate,
        Status      = m.Status,
        Genres      = m.MovieGenres.Select(mg => mg.Genre?.GenreName ?? "").ToList()
    };
}
