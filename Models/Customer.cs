namespace Backend.Models;

public class Customer
{
    public int CustomerID { get; set; }
    public int UserID { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
