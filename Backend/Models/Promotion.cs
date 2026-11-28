namespace Backend.Models;

public class Promotion
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

public class BookingPromotion
{
    public int BookingPromotionID { get; set; }
    public int BookingID { get; set; }
    public int PromotionID { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime? AppliedAt { get; set; }

    public Booking Booking { get; set; } = null!;
    public Promotion Promotion { get; set; } = null!;
}
