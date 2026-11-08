using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Ticket")]
public class Ticket
{
    [Key]
    [Column("TicketID")]
    public int TicketId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Price { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column("Showtime_SeatID")]
    public int? ShowtimeSeatId { get; set; }

    [Column("Booking_ID")]
    public int? BookingId { get; set; }

    public virtual ShowtimeSeat? ShowtimeSeat { get; set; }
    public virtual Booking? Booking { get; set; }
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
}
