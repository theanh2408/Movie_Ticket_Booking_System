namespace Backend.Models;

public class Ticket
{
    public int TicketID { get; set; }
    public decimal? Price { get; set; }
    public string? Status { get; set; }
    public int? ShowtimeSeatID { get; set; }     // cột Showtime_SeatID
    public int? BookingID { get; set; }          // cột Booking_ID
    public string? Barcode { get; set; }

    public ShowtimeSeat ShowtimeSeat { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
}
