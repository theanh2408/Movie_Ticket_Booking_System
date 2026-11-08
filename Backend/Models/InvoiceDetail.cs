using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Invoice_Detail")]
public class InvoiceDetail
{
    [Key]
    [Column("DetailID")]
    public int DetailId { get; set; }

    [Column("InvoiceID")]
    public int InvoiceId { get; set; }

    [Column("TicketID")]
    public int TicketId { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
    public virtual Ticket Ticket { get; set; } = null!;
}
