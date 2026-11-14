using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Backend.DTOs;

public class CreateMovieRequest : IValidatableObject
{
    [Required(ErrorMessage = "Tên phim không được để trống.")]
    [StringLength(255, ErrorMessage = "Tên phim tối đa 255 ký tự.")]
    [RegularExpression(@".*\S.*", ErrorMessage = "Tên phim không được chỉ chứa khoảng trắng.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Thời lượng phim là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải là số nguyên dương.")]
    public int? Duration { get; set; }

    /// <summary>
    /// File ảnh poster tải lên từ máy client (tối đa 5MB, .jpg, .png, .webp)
    /// </summary>
    public IFormFile? PosterFile { get; set; }

    /// <summary>
    /// Link URL poster ngoài (hoặc dùng khi không upload file)
    /// </summary>
    [StringLength(500, ErrorMessage = "PosterURL tối đa 500 ký tự.")]
    public string? PosterUrl { get; set; }

    /// <summary>
    /// File video trailer tải lên từ máy client (tối đa 100MB, .mp4, .webm, .mov)
    /// </summary>
    public IFormFile? TrailerFile { get; set; }

    /// <summary>
    /// Link URL trailer ngoài (ví dụ YouTube link, hoặc dùng khi không upload file)
    /// </summary>
    [StringLength(500, ErrorMessage = "TrailerURL tối đa 500 ký tự.")]
    public string? TrailerUrl { get; set; }

    [Required(ErrorMessage = "Phân loại độ tuổi là bắt buộc.")]
    [RegularExpression(
        @"^(P|K|T13|T16|T18|C13|C16|C18)$",
        ErrorMessage = "AgeRating chỉ được là P, K, T13, T16, T18, C13, C16 hoặc C18.")]
    public string AgeRating { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    [Required(ErrorMessage = "Trạng thái phim là bắt buộc.")]
    [RegularExpression(
        @"^(UPCOMING|NOW_SHOWING|ENDED|COMING_SOON)$",
        ErrorMessage = "Status phải là UPCOMING, NOW_SHOWING hoặc ENDED.")]
    public string Status { get; set; } = string.Empty;

    public List<int> GenreIds { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PosterFile == null && string.IsNullOrWhiteSpace(PosterUrl))
        {
            yield return new ValidationResult(
                "Vui lòng cung cấp file ảnh Poster (PosterFile) hoặc đường dẫn PosterURL.",
                new[] { nameof(PosterFile), nameof(PosterUrl) });
        }

        if (TrailerFile == null && string.IsNullOrWhiteSpace(TrailerUrl))
        {
            yield return new ValidationResult(
                "Vui lòng cung cấp file video Trailer (TrailerFile) hoặc đường dẫn TrailerURL.",
                new[] { nameof(TrailerFile), nameof(TrailerUrl) });
        }
    }
}
