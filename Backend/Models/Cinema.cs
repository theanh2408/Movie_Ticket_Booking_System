using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Cinema")]
public class Cinema
{
    [Key]
    [Column("CinemaID")]
    public int CinemaId { get; set; }

    [MaxLength(150)]
    public string? CinemaName { get; set; }

    [MaxLength(255)]
    public string? Address { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
    public virtual ICollection<Manager> Managers { get; set; } = new List<Manager>();
}
