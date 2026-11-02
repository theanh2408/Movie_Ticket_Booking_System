namespace Backend.Models;

// Bảng trung gian Movie_Genre (n-n)
public class MovieGenre
{
    public int MovieID { get; set; }
    public int GenreID { get; set; }            // cột Genre_ID trong DB

    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
