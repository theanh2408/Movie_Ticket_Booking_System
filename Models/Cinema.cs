namespace Backend.Models;

public class Cinema
{
    public int CinemaID { get; set; }
    public string? CinemaName { get; set; }
    public string? Address { get; set; }
    public string? Status { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
