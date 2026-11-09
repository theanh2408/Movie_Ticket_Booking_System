namespace Backend.Models;

public class Movie
{
    public int MovieID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Duration { get; set; }          // phút
    public string? PosterURL { get; set; }
    public string? TrailerURL { get; set; }
    public string? AgeRating { get; set; }      // P, K, T13, T16, T18
    public DateOnly? ReleaseDate { get; set; }
    public string? Status { get; set; }

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
