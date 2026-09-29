using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class ShowtimeSeat
{
    public int ShowtimeSeatId { get; set; }

    public string? Status { get; set; }

    public decimal? Price { get; set; }

    public int? ShowtimeId { get; set; }

    public int? SeatId { get; set; }

    public virtual Seat? Seat { get; set; }

    public virtual Showtime? Showtime { get; set; }

    public virtual Ticket? Ticket { get; set; }
}
