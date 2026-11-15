namespace Backend.Models;

public class Genre
{
    public int GenreID { get; set; }            // cột Genre_ID
    public string? GenreName { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
