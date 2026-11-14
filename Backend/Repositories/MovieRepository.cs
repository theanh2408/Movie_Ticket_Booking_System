using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

public class MovieRepository
{
    private readonly ApplicationDbContext _context;

    public MovieRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Movie> AddAsync(
        Movie movie,
        IReadOnlyCollection<int> genreIds,
        CancellationToken cancellationToken = default)
    {
        if (genreIds is null)
        {
            throw new ArgumentException("Danh sách thể loại không hợp lệ.");
        }

        // Loại bỏ GenreId trùng lặp.
        var uniqueGenreIds = genreIds.Distinct().ToList();

        // Kiểm tra các thể loại có tồn tại trong database không.
        var genres = await _context.Genres
            .Where(g => uniqueGenreIds.Contains(g.GenreId))
            .ToListAsync(cancellationToken);

        if (genres.Count != uniqueGenreIds.Count)
        {
            throw new ArgumentException(
                "Một hoặc nhiều thể loại không tồn tại.");
        }

        // Liên kết phim với các thể loại đã chọn.
        movie.Genres = genres;

        _context.Movies.Add(movie);

        await _context.SaveChangesAsync(cancellationToken);

        return movie;
    }

    public async Task<Movie?> GetByIdAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Movies
            .Include(m => m.Genres)
            .Include(m => m.Showtimes)
            .FirstOrDefaultAsync(m => m.MovieId == movieId, cancellationToken);
    }

    public async Task<Movie> UpdateAsync(
        Movie movie,
        IReadOnlyCollection<int>? genreIds,
        CancellationToken cancellationToken = default)
    {
        if (genreIds != null)
        {
            var uniqueGenreIds = genreIds.Distinct().ToList();
            var genres = await _context.Genres
                .Where(g => uniqueGenreIds.Contains(g.GenreId))
                .ToListAsync(cancellationToken);

            if (genres.Count != uniqueGenreIds.Count)
            {
                throw new ArgumentException("Một hoặc nhiều thể loại không tồn tại.");
            }

            movie.Genres.Clear();
            foreach (var g in genres)
            {
                movie.Genres.Add(g);
            }
        }

        _context.Movies.Update(movie);
        await _context.SaveChangesAsync(cancellationToken);
        return movie;
    }

    public async Task DeleteAsync(
        Movie movie,
        CancellationToken cancellationToken = default)
    {
        movie.Genres.Clear();
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Movies
            .AnyAsync(m => m.MovieId == movieId, cancellationToken);
    }
}
