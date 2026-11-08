using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Room")]
public class Room
{
    [Key]
    [Column("RoomID")]
    public int RoomId { get; set; }

    [MaxLength(100)]
    public string? RoomName { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Column("CinemaID")]
    public int? CinemaId { get; set; }

    public virtual Cinema? Cinema { get; set; }
    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
