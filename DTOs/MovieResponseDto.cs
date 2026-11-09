namespace Backend.DTOs;

public class MovieResponseDto
{
    public int MovieID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Duration { get; set; }
    public string? PosterURL { get; set; }
    public string? TrailerURL { get; set; }
    public string? AgeRating { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public string? Status { get; set; }
    public List<string> Genres { get; set; } = new();
}
