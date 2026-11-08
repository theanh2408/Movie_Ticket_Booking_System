using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Promotion")]
public class Promotion
{
    [Key]
    [Column("PromotionID")]
    public int PromotionId { get; set; }

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(255)]
    public string? PromotionName { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? DiscountType { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DiscountValue { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MinOrderAmount { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? MaxDiscountAmount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int? UsagePerCustomer { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
}
