using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public string? TransactionNo { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime? PaymentDate { get; set; }

    public int? InvoiceId { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
