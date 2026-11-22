namespace Backend.Models;

public class Invoice
{
    public int InvoiceID { get; set; }
    public DateTime? CreateDate { get; set; }
    public string? Status { get; set; }
    public decimal? TotalPrice { get; set; }
    public int? BookingID { get; set; }

    public Booking Booking { get; set; } = null!;
    public ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();
}

public class InvoiceDetail
{
    public int DetailID { get; set; }
    public int InvoiceID { get; set; }
    public int TicketID { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public Ticket Ticket { get; set; } = null!;
}
