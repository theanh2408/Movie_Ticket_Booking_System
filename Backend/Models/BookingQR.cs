namespace Backend.Models;

public class BookingQR
{
    public long QrId { get; set; }
    public int BookingId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public Booking? Booking { get; set; }
}
