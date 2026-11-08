using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Booking_Promotion")]
public class BookingPromotion
{
    [Key]
    [Column("BookingPromotionID")]
    public int BookingPromotionId { get; set; }

    [Column("BookingID")]
    public int BookingId { get; set; }

    [Column("PromotionID")]
    public int PromotionId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DiscountAmount { get; set; }

    public DateTime? AppliedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;
    public virtual Promotion Promotion { get; set; } = null!;
}
