namespace Backend.Models;

public class ShowtimeSeat
{
    public int ShowtimeSeatID { get; set; }      // cột Showtime_SeatID
    public string? Status { get; set; }
    public decimal? Price { get; set; }
    public int? ShowtimeID { get; set; }
    public int? SeatID { get; set; }

    public Showtime Showtime { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
}
