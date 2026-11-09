using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IMovieService
{
    Task<List<MovieResponseDto>> GetAllAsync();
    Task<MovieResponseDto?> GetByIdAsync(int id);
    Task<MovieResponseDto> CreateAsync(CreateMovieDto dto);
}

public class MovieService : IMovieService
{
    private readonly IMovieRepository _repo;

    public MovieService(IMovieRepository repo) => _repo = repo;

    public async Task<List<MovieResponseDto>> GetAllAsync()
    {
        var movies = await _repo.GetAllAsync();
        return movies.Select(ToDto).ToList();
    }

    public async Task<MovieResponseDto?> GetByIdAsync(int id)
    {
        var movie = await _repo.GetByIdAsync(id);
        return movie is null ? null : ToDto(movie);
    }

    public async Task<MovieResponseDto> CreateAsync(CreateMovieDto dto)
    {
        // Nghiệp vụ: kiểm tra trước khi lưu
        var title = dto.Title.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tên phim không được để trống");

        if (await _repo.TitleExistsAsync(title))
            throw new InvalidOperationException($"Phim \"{title}\" đã tồn tại");

        var genreIds = dto.GenreIds.Distinct().ToList();
        if (genreIds.Count > 0 && await _repo.CountExistingGenresAsync(genreIds) != genreIds.Count)
            throw new ArgumentException("Có thể loại không tồn tại trong hệ thống");

        var movie = new Movie
        {
            Title       = title,
            Description = dto.Description,
            Duration    = dto.Duration,
            PosterURL   = dto.PosterURL,
            TrailerURL  = dto.TrailerURL,
            AgeRating   = dto.AgeRating,
            ReleaseDate = dto.ReleaseDate,
            Status      = string.IsNullOrWhiteSpace(dto.Status) ? "NowShowing" : dto.Status,
            MovieGenres = genreIds.Select(id => new MovieGenre { GenreID = id }).ToList()
        };

        await _repo.AddAsync(movie);

        // Lấy lại để trả về kèm tên thể loại
        return (await GetByIdAsync(movie.MovieID))!;
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
        Genres      = m.MovieGenres.Select(mg => mg.Genre.GenreName ?? "").ToList()
    };
}
