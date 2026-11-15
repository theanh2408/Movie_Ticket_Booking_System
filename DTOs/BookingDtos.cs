namespace Backend.DTOs;

public class BookingListItemDto
{
    public int BookingID { get; set; }
    public int? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime? CreatedDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
}

public class TicketDto
{
    public int TicketID { get; set; }
    public string? SeatCode { get; set; }
    public decimal? Price { get; set; }
    public string? Status { get; set; }
}

public class PaymentDto
{
    public int PaymentID { get; set; }
    public string? TransactionNo { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int? BookingID { get; set; }
    public int? InvoiceID { get; set; }
}

public class BookingDetailDto
{
    public int BookingID { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public int? CustomerID { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    public string? MovieTitle { get; set; }
    public string? CinemaName { get; set; }
    public string? RoomName { get; set; }
    public DateOnly? ShowDate { get; set; }
    public TimeOnly? StartTime { get; set; }

    public List<TicketDto> Tickets { get; set; } = new();
    public List<PaymentDto> Payments { get; set; } = new();
    public int? InvoiceID { get; set; }
}

public class InvoiceListItemDto
{
    public int InvoiceID { get; set; }
    public DateTime? CreateDate { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Status { get; set; }
    public int? BookingID { get; set; }
    public string? CustomerName { get; set; }
}

public class InvoiceDetailDto
{
    public int InvoiceID { get; set; }
    public DateTime? CreateDate { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Status { get; set; }
    public BookingDetailDto? Booking { get; set; }
}
