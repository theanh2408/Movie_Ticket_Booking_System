using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Backend.DTOs;

public class UpdateMovieRequest
{
    [StringLength(255, ErrorMessage = "Tên phim tối đa 255 ký tự.")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Thời lượng phải là số nguyên dương.")]
    public int? Duration { get; set; }

    /// <summary>
    /// File ảnh poster mới thay thế (tối đa 5MB)
    /// </summary>
    public IFormFile? PosterFile { get; set; }

    /// <summary>
    /// Link URL poster mới nếu không upload file
    /// </summary>
    [StringLength(500, ErrorMessage = "PosterURL tối đa 500 ký tự.")]
    public string? PosterUrl { get; set; }

    /// <summary>
    /// File video trailer mới thay thế (tối đa 100MB)
    /// </summary>
    public IFormFile? TrailerFile { get; set; }

    /// <summary>
    /// Link URL trailer mới nếu không upload file
    /// </summary>
    [StringLength(500, ErrorMessage = "TrailerURL tối đa 500 ký tự.")]
    public string? TrailerUrl { get; set; }

    [RegularExpression(
        @"^(P|K|T13|T16|T18|C13|C16|C18)$",
        ErrorMessage = "AgeRating chỉ được là P, K, T13, T16, T18, C13, C16 hoặc C18.")]
    public string? AgeRating { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    [RegularExpression(
        @"^(UPCOMING|NOW_SHOWING|ENDED|COMING_SOON)$",
        ErrorMessage = "Status phải là UPCOMING, NOW_SHOWING hoặc ENDED.")]
    public string? Status { get; set; }

    /// <summary>
    /// Danh sách thể loại mới (nếu truyền vào thì cập nhật, không truyền thì giữ nguyên)
    /// </summary>
    public List<int>? GenreIds { get; set; }
}
