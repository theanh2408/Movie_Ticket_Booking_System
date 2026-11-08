using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Customer")]
public class Customer
{
    [Key]
    [Column("CustomerID")]
    public int CustomerId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    public virtual Account? User { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
