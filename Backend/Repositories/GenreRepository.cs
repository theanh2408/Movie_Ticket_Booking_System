using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface IGenreRepository
{
    Task<List<Genre>> GetAllAsync(CancellationToken ct = default);
    Task<Genre?> GetByIdAsync(int id, CancellationToken ct = default);   // có tracking
    Task<bool> NameExistsAsync(string name, int? exceptId = null, CancellationToken ct = default);
    Task<bool> HasMoviesAsync(int genreId, CancellationToken ct = default);
    Task AddAsync(Genre genre, CancellationToken ct = default);
    Task RemoveAsync(Genre genre, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public class GenreRepository : IGenreRepository
{
    private readonly ApplicationDbContext _db;

    public GenreRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Genre>> GetAllAsync(CancellationToken ct = default) =>
        _db.Genres.AsNoTracking().OrderBy(g => g.GenreName).ToListAsync(ct);

    public Task<Genre?> GetByIdAsync(int id, CancellationToken ct = default) => _db.Genres.FirstOrDefaultAsync(g => g.GenreID == id, ct);

    public Task<bool> NameExistsAsync(string name, int? exceptId = null, CancellationToken ct = default) =>
        _db.Genres.AnyAsync(g => g.GenreName == name && (exceptId == null || g.GenreID != exceptId), ct);

    public Task<bool> HasMoviesAsync(int genreId, CancellationToken ct = default) => _db.MovieGenres.AnyAsync(mg => mg.GenreID == genreId, ct);

    public async Task AddAsync(Genre genre, CancellationToken ct = default)
    {
        _db.Genres.Add(genre);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Genre genre, CancellationToken ct = default)
    {
        _db.Genres.Remove(genre);
        await _db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
