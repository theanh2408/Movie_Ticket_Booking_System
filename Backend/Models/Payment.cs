namespace Backend.Models;

public class Payment
{
    public int PaymentID { get; set; }
    public string? TransactionNo { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int? BookingID { get; set; }

    public Booking Booking { get; set; } = null!;
}
