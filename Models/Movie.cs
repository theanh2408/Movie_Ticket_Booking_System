namespace Backend.Models;

public class Movie
{
    public int MovieID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Duration { get; set; }          // phút
    public string? PosterURL { get; set; }
    public string? TrailerURL { get; set; }
    public string? AgeRating { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Status { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

public class Genre
{
    public int GenreID { get; set; }            // cột Genre_ID
    public string? GenreName { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}

public class MovieGenre
{
    public int MovieID { get; set; }
    public int GenreID { get; set; }            // cột Genre_ID

    public Movie Movie { get; set; } = null!;
    public Genre Genre { get; set; } = null!;
}
