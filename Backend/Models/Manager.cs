using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Manager")]
public class Manager
{
    [Key]
    [Column("ManagerID")]
    public int ManagerId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    [Column("CinemaID")]
    public int? CinemaId { get; set; }

    public virtual Account? User { get; set; }
    public virtual Cinema? Cinema { get; set; }
}
