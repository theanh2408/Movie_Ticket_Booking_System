using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IMovieRepository
{
    Task<List<Movie>> GetAllAsync();
    Task<Movie?> GetByIdAsync(int id);
    Task<bool> TitleExistsAsync(string title);
    Task<int> CountExistingGenresAsync(List<int> genreIds);
    Task AddAsync(Movie movie);
}

public class MovieRepository : IMovieRepository
{
    private readonly ApplicationDbContext _db;

    public MovieRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Movie>> GetAllAsync() =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .OrderByDescending(m => m.MovieID)
           .AsNoTracking()
           .ToListAsync();

    public Task<Movie?> GetByIdAsync(int id) =>
        _db.Movies
           .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
           .AsNoTracking()
           .FirstOrDefaultAsync(m => m.MovieID == id);

    public Task<bool> TitleExistsAsync(string title) =>
        _db.Movies.AnyAsync(m => m.Title == title);

    public Task<int> CountExistingGenresAsync(List<int> genreIds) =>
        _db.Genres.CountAsync(g => genreIds.Contains(g.GenreID));

    public async Task AddAsync(Movie movie)
    {
        _db.Movies.Add(movie);          // kèm MovieGenres -> lưu trong 1 transaction
        await _db.SaveChangesAsync();
    }
}
