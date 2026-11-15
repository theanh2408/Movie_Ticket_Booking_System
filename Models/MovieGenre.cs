namespace Backend.Models;

public class MovieGenre
{
    public int MovieID { get; set; }
    public int GenreID { get; set; }            // cột Genre_ID

    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
