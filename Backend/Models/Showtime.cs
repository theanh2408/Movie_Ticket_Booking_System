using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Showtime")]
public class Showtime
{
    [Key]
    [Column("ShowtimeID")]
    public int ShowtimeId { get; set; }

    [Column("Start_hour")]
    public TimeOnly? StartHour { get; set; }

    [Column("Date")]
    public DateOnly? Date { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? BasePrice { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column("MovieID")]
    public int? MovieId { get; set; }

    [Column("RoomID")]
    public int? RoomId { get; set; }

    public virtual Movie? Movie { get; set; }
    public virtual Room? Room { get; set; }
    public virtual ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}
