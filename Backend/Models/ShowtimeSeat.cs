using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Showtime_Seat")]
public class ShowtimeSeat
{
    [Key]
    [Column("Showtime_SeatID")]
    public int ShowtimeSeatId { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Price { get; set; }

    [Column("ShowtimeID")]
    public int? ShowtimeId { get; set; }

    [Column("SeatID")]
    public int? SeatId { get; set; }

    public virtual Showtime? Showtime { get; set; }
    public virtual Seat? Seat { get; set; }
    public virtual Ticket? Ticket { get; set; }
}
