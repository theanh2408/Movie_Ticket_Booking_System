using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Movie_Genre")]
public class MovieGenre
{
    [Column("MovieID")]
    public int MovieId { get; set; }

    [Column("Genre_ID")]
    public int GenreId { get; set; }

    public virtual Movie Movie { get; set; } = null!;
    public virtual Genre Genre { get; set; } = null!;
}
