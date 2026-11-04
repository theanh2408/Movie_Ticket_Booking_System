namespace Backend.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public int UserId { get; set; }
    public Account? User { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
