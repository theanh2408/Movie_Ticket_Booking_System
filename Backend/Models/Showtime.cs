using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Showtime
{
    public int ShowtimeId { get; set; }

    public TimeOnly? StartHour { get; set; }

    public DateOnly? Date { get; set; }

    public decimal? BasePrice { get; set; }

    public string? Status { get; set; }

    public int? MovieId { get; set; }

    public int? RoomId { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual Room? Room { get; set; }

    public virtual ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}
