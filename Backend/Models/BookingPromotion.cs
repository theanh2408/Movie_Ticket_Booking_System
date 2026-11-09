namespace Backend.Models;

public class BookingPromotion
{
    public int BookingPromotionId { get; set; }
    public int BookingId { get; set; }
    public int PromotionId { get; set; }
    public decimal? DiscountAmount { get; set; }
    public DateTime? AppliedAt { get; set; }
    public Booking? Booking { get; set; }
    public Promotion? Promotion { get; set; }
}
