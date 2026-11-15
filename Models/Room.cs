namespace Backend.Models;

public class Room
{
    public int RoomID { get; set; }
    public string? RoomName { get; set; }
    public string? Status { get; set; }
    public int? CinemaID { get; set; }
    public int? Capacity { get; set; }           // cột mới
    public string? Description { get; set; }     // cột mới

    public Cinema Cinema { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
