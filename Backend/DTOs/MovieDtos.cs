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

    /// <summary>ComingSoon | NowShowing | Ended | Inactive (mặc định NowShowing)</summary>
    [StringLength(50)]
    public string? Status { get; set; }

    // Danh sách Genre_ID được chọn cho phim
    public List<int> GenreIds { get; set; } = new();
}

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

public class StatusDto
{
    [Required(ErrorMessage = "Trạng thái không được để trống")]
    public string Status { get; set; } = string.Empty;
}

public class GenreDto
{
    public int GenreID { get; set; }
    public string? GenreName { get; set; }
}

public class GenreUpsertDto
{
    [Required(ErrorMessage = "Tên thể loại không được để trống"), StringLength(100)]
    public string GenreName { get; set; } = string.Empty;
}

/// <summary>
/// Tham số phân trang + sắp xếp dùng chung cho các API danh sách:
/// ?page=1&amp;pageSize=20&amp;sortBy=title&amp;sortDir=desc
/// </summary>
public class PageQuery
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>Trang hiện tại, bắt đầu từ 1</summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>Số dòng mỗi trang (mặc định 20, tối đa 100)</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : Math.Min(value, 100);
    }

    /// <summary>Tên trường sắp xếp (mỗi danh sách hỗ trợ một số trường, xem API-GUIDE.md). Bỏ trống = thứ tự mặc định.</summary>
    public string? SortBy { get; set; }

    /// <summary>asc | desc</summary>
    public string? SortDir { get; set; }

    public string? GetSortKey() => string.IsNullOrWhiteSpace(SortBy) ? null : SortBy.Trim().ToLowerInvariant();

    /// <summary>Không truyền sortBy: dùng hướng mặc định của danh sách. Có sortBy mà không truyền sortDir: tăng dần.</summary>
    public bool IsDesc(bool defaultDesc)
    {
        if (string.IsNullOrWhiteSpace(SortDir))
            return GetSortKey() == null && defaultDesc;
        return string.Equals(SortDir.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public static PagedResult<T> Create(List<T> items, int total, PageQuery paging) => new()
    {
        Items = items,
        Page = paging.Page,
        PageSize = paging.PageSize,
        TotalItems = total,
        TotalPages = (int)Math.Ceiling(total / (double)paging.PageSize)
    };
}
