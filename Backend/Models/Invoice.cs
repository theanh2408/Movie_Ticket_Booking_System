namespace Backend.Models;

public class Invoice
{
    public int InvoiceID { get; set; }
    public DateTime? CreateDate { get; set; }
    public string? Status { get; set; }
    public decimal? TotalPrice { get; set; }
    public int? BookingID { get; set; }

    public Booking Booking { get; set; } = null!;
}
