using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public string? RoomName { get; set; }

    public string? Status { get; set; }

    public int? CinemaId { get; set; }

    public virtual Cinema? Cinema { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
