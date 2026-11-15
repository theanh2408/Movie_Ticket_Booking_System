namespace Backend.Models;

public class Staff                               // bảng mới (file SQL cập nhật lần 2)
{
    public int StaffID { get; set; }
    public int UserID { get; set; }
    public int? CinemaID { get; set; }

    public Account Account { get; set; } = null!;
    public Cinema Cinema { get; set; } = null!;
}
