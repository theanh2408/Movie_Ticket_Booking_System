namespace Backend.Models;

public class Seat
{
    public int SeatID { get; set; }
    public string? Row { get; set; }
    public string? Column { get; set; }
    public string? SeatType { get; set; }        // cột mới (Standard, VIP, Couple...)
    public int? RoomID { get; set; }

    public Room Room { get; set; } = null!;
}
