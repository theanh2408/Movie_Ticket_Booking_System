using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Movie")]
public class Movie
{
    [Key]
    [Column("MovieID")]
    public int MovieId { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? Duration { get; set; }

    [Column("PosterURL")]
    [MaxLength(500)]
    public string? PosterUrl { get; set; }

    [Column("TrailerURL")]
    [MaxLength(500)]
    public string? TrailerUrl { get; set; }

    [MaxLength(20)]
    public string? AgeRating { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
