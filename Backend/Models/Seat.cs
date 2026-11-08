using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Seat")]
public class Seat
{
    [Key]
    [Column("SeatID")]
    public int SeatId { get; set; }

    [Column("Row")]
    [MaxLength(20)]
    public string? Row { get; set; }

    [Column("Column")]
    [MaxLength(20)]
    public string? Column { get; set; }

    [Column("RoomID")]
    public int? RoomId { get; set; }

    public virtual Room? Room { get; set; }
    public virtual ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}
