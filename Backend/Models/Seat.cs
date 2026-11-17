namespace Backend.Models;

public class Seat
{
    public int SeatID { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    public int? RoomID { get; set; }

    public Room Room { get; set; } = null!;
}
