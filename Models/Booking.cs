namespace Backend.Models;

public class Booking
{
    public int BookingID { get; set; }
    public int? CustomerID { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }

    public Customer Customer { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Invoice? Invoice { get; set; }
}
