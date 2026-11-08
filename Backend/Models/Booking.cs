using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Booking")]
public class Booking
{
    [Key]
    [Column("BookingID")]
    public int BookingId { get; set; }

    [Column("CustomerID")]
    public int? CustomerId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ExpiredAt { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? TotalAmount { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? SubTotal { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? DiscountAmount { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public virtual ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual Invoice? Invoice { get; set; }
}
