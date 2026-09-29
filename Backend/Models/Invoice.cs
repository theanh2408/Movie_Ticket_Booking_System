using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Status { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Account? User { get; set; }
}
