using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Invoice
{
    public int InvoiceId { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Status { get; set; }

    public decimal? TotalPrice { get; set; }
    public int? BookingId { get; set; }

    public virtual Booking? Booking { get; set; }
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
}
