namespace Backend.Models;

public class BookingPromotion
{
    public int BookingPromotionID { get; set; }
    public int BookingID { get; set; }
    public int PromotionID { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime? AppliedAt { get; set; }
}
