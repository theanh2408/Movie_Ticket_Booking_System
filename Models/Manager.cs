namespace Backend.Models;

public class Manager
{
    public int ManagerID { get; set; }
    public int UserID { get; set; }
    public int? CinemaID { get; set; }

    public Account Account { get; set; } = null!;
    public Cinema Cinema { get; set; } = null!;
}
