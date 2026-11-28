using Backend.Common;
using Backend.DTOs;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IMovieRepository
{
    Task<(List<Movie> Items, int Total)> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating, PageQuery paging, CancellationToken ct = default);
    Task<Movie?> GetByIdAsync(int id, CancellationToken ct = default);          // không tracking, kèm thể loại
    Task<Movie?> GetForUpdateAsync(int id, CancellationToken ct = default);     // có tracking, kèm thể loại
    Task<bool> TitleExistsAsync(string title, int? exceptMovieId = null, CancellationToken ct = default);
    Task<int> CountExistingGenresAsync(List<int> genreIds, CancellationToken ct = default);
    Task<bool> HasShowtimesAsync(int movieId, CancellationToken ct = default);
    Task<bool> HasUpcomingShowtimesAsync(int movieId, CancellationToken ct = default);
    Task AddAsync(Movie movie, CancellationToken ct = default);
    void RemoveMovieGenres(IEnumerable<MovieGenre> items);
    Task DeleteAsync(Movie movie, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class MovieRepository : IMovieRepository
{
    private readonly ApplicationDbContext _db;

    public MovieRepository(ApplicationDbContext db) => _db = db;

    public async Task<(List<Movie> Items, int Total)> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating, PageQuery paging, CancellationToken ct = default)
    {
        var q = _db.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            q = q.Where(m => m.Title!.Contains(k));
        }
        if (!string.IsNullOrWhiteSpace(status))    q = q.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(ageRating)) q = q.Where(m => m.AgeRating == ageRating);
        if (genreId.HasValue)                      q = q.Where(m => m.MovieGenres.Any(mg => mg.GenreID == genreId));

        var desc = paging.IsDesc(true);
        IOrderedQueryable<Movie> ordered = paging.GetSortKey() switch
        {
            null or "id"  => q.By(m => m.MovieID, desc),
            "title"       => q.By(m => m.Title, desc),
            "releasedate" => q.By(m => m.ReleaseDate, desc),
            "duration"    => q.By(m => m.Duration, desc),
            "status"      => q.By(m => m.Status, desc),
            _ => throw PagingExtensions.UnknownSort("id, title, releaseDate, duration, status")
        };
        return await ordered.ThenBy(m => m.MovieID).ToPagedAsync(paging, ct);
    }

    public Task<Movie?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .AsNoTracking()
           .FirstOrDefaultAsync(m => m.MovieID == id, ct);

    public Task<Movie?> GetForUpdateAsync(int id, CancellationToken ct = default) =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .FirstOrDefaultAsync(m => m.MovieID == id, ct);

    public Task<bool> TitleExistsAsync(string title, int? exceptMovieId = null, CancellationToken ct = default) =>
        _db.Movies.AnyAsync(m => m.Title == title && (exceptMovieId == null || m.MovieID != exceptMovieId), ct);

    public Task<int> CountExistingGenresAsync(List<int> genreIds, CancellationToken ct = default) =>
        _db.Genres.CountAsync(g => genreIds.Contains(g.GenreID), ct);

    public Task<bool> HasShowtimesAsync(int movieId, CancellationToken ct = default) => _db.Showtimes.AnyAsync(s => s.MovieID == movieId, ct);

    /// <summary>Có suất chiếu chưa diễn ra (so cả ngày lẫn giờ bắt đầu theo giờ địa phương), chưa bị hủy.</summary>
    public Task<bool> HasUpcomingShowtimesAsync(int movieId, CancellationToken ct = default)
    {
        var now = AppClock.LocalNow;
        var today = DateOnly.FromDateTime(now);
        var time = TimeOnly.FromDateTime(now);
        return _db.Showtimes.AnyAsync(s => s.MovieID == movieId
                                           && s.Status != ShowtimeStatus.Cancelled
                                           && (s.Date > today || (s.Date == today && s.StartHour > time)), ct);
    }

    public async Task AddAsync(Movie movie, CancellationToken ct = default)
    {
        _db.Movies.Add(movie);          // kèm MovieGenres -> lưu trong 1 transaction
        await _db.SaveChangesAsync(ct);
    }

    public void RemoveMovieGenres(IEnumerable<MovieGenre> items) => _db.MovieGenres.RemoveRange(items);

    public async Task DeleteAsync(Movie movie, CancellationToken ct = default)
    {
        _db.MovieGenres.RemoveRange(movie.MovieGenres);
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
