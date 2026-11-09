using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateMovieDto
{
    [Required(ErrorMessage = "Tên phim không được để trống")]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Thời lượng không được để trống")]
    [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút")]
    public int Duration { get; set; }

    [StringLength(500)]
    public string? PosterURL { get; set; }

    [StringLength(500)]
    public string? TrailerURL { get; set; }

    [StringLength(20)]
    public string? AgeRating { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    // Danh sách Genre_ID được chọn cho phim
    public List<int> GenreIds { get; set; } = new();
}
