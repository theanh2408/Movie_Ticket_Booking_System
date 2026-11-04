namespace Backend.Models;

public class Booking
{
    public int BookingId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
    public Customer? Customer { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
}
