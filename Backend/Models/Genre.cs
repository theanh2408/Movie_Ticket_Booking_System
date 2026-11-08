using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Genre")]
public class Genre
{
    [Key]
    [Column("Genre_ID")]
    public int GenreId { get; set; }

    [MaxLength(100)]
    public string? GenreName { get; set; }

    public virtual ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
