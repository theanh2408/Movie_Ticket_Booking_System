using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public string? Row { get; set; }

    public string? Column { get; set; }

    public int? RoomId { get; set; }

    public virtual Room? Room { get; set; }

    public virtual ICollection<ShowtimeSeat> ShowtimeSeats { get; set; } = new List<ShowtimeSeat>();
}
