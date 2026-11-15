using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public interface ICinemaRepository
{
    Task<List<Cinema>> GetAllAsync(int? cinemaId);
    Task<Cinema?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
}

public class CinemaRepository : ICinemaRepository
{
    private readonly ApplicationDbContext _db;

    public CinemaRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Cinema>> GetAllAsync(int? cinemaId) =>
        _db.Cinemas.AsNoTracking()
            .Where(c => cinemaId == null || c.CinemaID == cinemaId)
            .OrderBy(c => c.CinemaName)
            .ToListAsync();

    public Task<Cinema?> GetByIdAsync(int id) =>
        _db.Cinemas.AsNoTracking().FirstOrDefaultAsync(c => c.CinemaID == id);

    public Task<bool> ExistsAsync(int id) => _db.Cinemas.AnyAsync(c => c.CinemaID == id);
}
