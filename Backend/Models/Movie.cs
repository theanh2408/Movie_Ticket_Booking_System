using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Movie
{
    public int MovieId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? Duration { get; set; }

    public string? PosterUrl { get; set; }

    public string? TrailerUrl { get; set; }

    public string? AgeRating { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
}
