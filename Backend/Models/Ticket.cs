using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Ticket
{
    public int TicketId { get; set; }

    public decimal? Price { get; set; }

    public string? Status { get; set; }

    public int? InvoiceId { get; set; }

    public int? ShowtimeSeatId { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual ShowtimeSeat? ShowtimeSeat { get; set; }
}
