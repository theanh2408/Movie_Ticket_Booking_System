using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class PromotionUpsertDto
{
    [Required(ErrorMessage = "Mã khuyến mãi không được để trống"), StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên chương trình không được để trống"), StringLength(255)]
    public string PromotionName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Percent (giảm theo %) | Fixed (giảm số tiền cố định)</summary>
    [Required(ErrorMessage = "Loại giảm giá không được để trống")]
    public string DiscountType { get; set; } = string.Empty;

    [Range(0.01, 1000000000, ErrorMessage = "Mức giảm phải lớn hơn 0")]
    public decimal DiscountValue { get; set; }

    [Range(0, 1000000000)]
    public decimal? MinOrderAmount { get; set; }

    [Range(0, 1000000000)]
    public decimal? MaxDiscountAmount { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime? StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
    public DateTime? EndDate { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }

    [Range(1, int.MaxValue)]
    public int? UsagePerCustomer { get; set; }

    /// <summary>Active | Inactive (mặc định Active)</summary>
    public string? Status { get; set; }
}

public class PromotionResponseDto
{
    public int PromotionID { get; set; }
    public string? Code { get; set; }
    public string? PromotionName { get; set; }
    public string? Description { get; set; }
    public string? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int? UsagePerCustomer { get; set; }
    public string? Status { get; set; }
}

public class ApplyPromotionDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
    public string Code { get; set; } = string.Empty;

    [Range(0.01, 1000000000, ErrorMessage = "Giá trị đơn phải lớn hơn 0")]
    public decimal OrderAmount { get; set; }
}

public class ApplyPromotionResultDto
{
    public string? Code { get; set; }
    public string? PromotionName { get; set; }
    public decimal OrderAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
}
