namespace Backend.Models;

public class InvoiceDetail
{
    public int DetailId { get; set; }
    public int InvoiceId { get; set; }
    public int TicketId { get; set; }
    public Invoice? Invoice { get; set; }
    public Ticket? Ticket { get; set; }
}
