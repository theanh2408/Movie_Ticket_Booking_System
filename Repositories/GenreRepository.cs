using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IGenreRepository
{
    Task<List<Genre>> GetAllAsync();
    Task<Genre?> GetByIdAsync(int id);   // có tracking
    Task<bool> NameExistsAsync(string name, int? exceptId = null);
    Task<bool> HasMoviesAsync(int genreId);
    Task AddAsync(Genre genre);
    Task RemoveAsync(Genre genre);
    Task SaveAsync();
}

public class GenreRepository : IGenreRepository
{
    private readonly ApplicationDbContext _db;

    public GenreRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Genre>> GetAllAsync() =>
        _db.Genres.AsNoTracking().OrderBy(g => g.GenreName).ToListAsync();

    public Task<Genre?> GetByIdAsync(int id) => _db.Genres.FirstOrDefaultAsync(g => g.GenreID == id);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null) =>
        _db.Genres.AnyAsync(g => g.GenreName == name && (exceptId == null || g.GenreID != exceptId));

    public Task<bool> HasMoviesAsync(int genreId) => _db.MovieGenres.AnyAsync(mg => mg.GenreID == genreId);

    public async Task AddAsync(Genre genre)
    {
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Genre genre)
    {
        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync();
    }

    public Task SaveAsync() => _db.SaveChangesAsync();
}
