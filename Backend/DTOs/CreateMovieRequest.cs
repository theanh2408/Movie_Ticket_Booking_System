
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateMovieRequest
{
    [Required(ErrorMessage = "Tên phim không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên phim tối đa 255 ký tự.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Tên phim không được chỉ chứa khoảng trắng.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Thời lượng phim là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải là số nguyên dương.")]
    public int? Duration { get; set; }

    [Required(ErrorMessage = "PosterURL là bắt buộc.")]
    [StringLength(500, ErrorMessage = "PosterURL tối đa 500 ký tự.")]
    [Url(ErrorMessage = "PosterURL không đúng định dạng URL.")]
    public string? PosterUrl { get; set; }

    [Required(ErrorMessage = "TrailerURL là bắt buộc.")]
    [StringLength(500, ErrorMessage = "TrailerURL tối đa 500 ký tự.")]
    [Url(ErrorMessage = "TrailerURL không đúng định dạng URL.")]
    public string? TrailerUrl { get; set; }

    [Required(ErrorMessage = "Phân loại độ tuổi là bắt buộc.")]
    [RegularExpression(
        @"^(P|K|T13|T16|T18)$",
        ErrorMessage = "AgeRating chỉ được là P, K, T13, T16 hoặc T18.")]
    public string AgeRating { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    [Required(ErrorMessage = "Trạng thái phim là bắt buộc.")]
    [RegularExpression(
        @"^(UPCOMING|NOW_SHOWING|ENDED)$",
        ErrorMessage = "Status phải là UPCOMING, NOW_SHOWING hoặc ENDED.")]
    public string Status { get; set; } = string.Empty;

    public List<int> GenreIds { get; set; } = new();
}
