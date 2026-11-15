namespace Backend.Models;

public class Showtime
{
    public int ShowtimeID { get; set; }
    public TimeOnly? StartHour { get; set; }     // cột Start_hour
    public DateOnly? Date { get; set; }
    public decimal? BasePrice { get; set; }
    public string? Status { get; set; }
    public int? MovieID { get; set; }
    public int? RoomID { get; set; }

    public Movie Movie { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}
