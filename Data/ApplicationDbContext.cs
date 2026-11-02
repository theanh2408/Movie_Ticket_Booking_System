using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>(e =>
        {
            e.ToTable("Movie");
            e.HasKey(x => x.MovieID);
        });

        modelBuilder.Entity<Genre>(e =>
        {
            e.ToTable("Genre");
            e.HasKey(x => x.GenreID);
            e.Property(x => x.GenreID).HasColumnName("Genre_ID");
        });

        modelBuilder.Entity<MovieGenre>(e =>
        {
            e.ToTable("Movie_Genre");
            e.HasKey(x => new { x.MovieID, x.GenreID });
            e.Property(x => x.GenreID).HasColumnName("Genre_ID");

            e.HasOne(x => x.Movie).WithMany(m => m.MovieGenres).HasForeignKey(x => x.MovieID);
            e.HasOne(x => x.Genre).WithMany(g => g.MovieGenres).HasForeignKey(x => x.GenreID);
        });
    }
}
