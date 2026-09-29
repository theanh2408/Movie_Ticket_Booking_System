using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Cinema
{
    public int CinemaId { get; set; }

    public string? CinemaName { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
