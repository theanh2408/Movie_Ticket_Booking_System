using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IMovieRepository
{
    Task<List<Movie>> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating);
    Task<Movie?> GetByIdAsync(int id);          // không tracking, kèm thể loại
    Task<Movie?> GetForUpdateAsync(int id);     // có tracking, kèm thể loại
    Task<bool> TitleExistsAsync(string title, int? exceptMovieId = null);
    Task<int> CountExistingGenresAsync(List<int> genreIds);
    Task<bool> HasShowtimesAsync(int movieId);
    Task<bool> HasUpcomingShowtimesAsync(int movieId);
    Task AddAsync(Movie movie);
    void RemoveMovieGenres(IEnumerable<MovieGenre> items);
    Task DeleteAsync(Movie movie);
    Task SaveAsync();
}

public class MovieRepository : IMovieRepository
{
    private readonly ApplicationDbContext _db;

    public MovieRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Movie>> SearchAsync(string? keyword, string? status, int? genreId, string? ageRating)
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

        return q.OrderByDescending(m => m.MovieID).ToListAsync();
    }

    public Task<Movie?> GetByIdAsync(int id) =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .AsNoTracking()
           .FirstOrDefaultAsync(m => m.MovieID == id);

    public Task<Movie?> GetForUpdateAsync(int id) =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .FirstOrDefaultAsync(m => m.MovieID == id);

    public Task<bool> TitleExistsAsync(string title, int? exceptMovieId = null) =>
        _db.Movies.AnyAsync(m => m.Title == title && (exceptMovieId == null || m.MovieID != exceptMovieId));

    public Task<int> CountExistingGenresAsync(List<int> genreIds) =>
        _db.Genres.CountAsync(g => genreIds.Contains(g.GenreID));

    public Task<bool> HasShowtimesAsync(int movieId) => _db.Showtimes.AnyAsync(s => s.MovieID == movieId);

    public Task<bool> HasUpcomingShowtimesAsync(int movieId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return _db.Showtimes.AnyAsync(s => s.MovieID == movieId
                                           && s.Status != ShowtimeStatus.Cancelled
                                           && s.Date >= today);
    }

    public async Task AddAsync(Movie movie)
    {
        _db.Movies.Add(movie);          // kèm MovieGenres -> lưu trong 1 transaction
        await _db.SaveChangesAsync();
    }

    public void RemoveMovieGenres(IEnumerable<MovieGenre> items) => _db.MovieGenres.RemoveRange(items);

    public async Task DeleteAsync(Movie movie)
    {
        _db.MovieGenres.RemoveRange(movie.MovieGenres);
        _db.Movies.Remove(movie);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
