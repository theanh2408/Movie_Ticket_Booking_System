using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Ticket
{
    public int TicketId { get; set; }

    public decimal? Price { get; set; }

    public string? Status { get; set; }

    public int? BookingId { get; set; }

    public int? ShowtimeSeatId { get; set; }

    public virtual Booking? Booking { get; set; }
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual ShowtimeSeat? ShowtimeSeat { get; set; }
}
