using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Payment")]
public class Payment
{
    [Key]
    [Column("PaymentID")]
    public int PaymentId { get; set; }

    [MaxLength(100)]
    public string? TransactionNo { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Amount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(50)]
    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    [Column("BookingID")]
    public int? BookingId { get; set; }

    public virtual Booking? Booking { get; set; }
}
