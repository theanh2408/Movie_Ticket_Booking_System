namespace Backend.Models;

public class Promotion
{
    public int PromotionId { get; set; }
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
    public ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
}
