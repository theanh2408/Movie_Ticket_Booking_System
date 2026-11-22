namespace Backend.Models;

public class Booking
{
    public int BookingID { get; set; }
    public int? CustomerID { get; set; }
    public int? ShowtimeID { get; set; }         // cột bổ sung: gắn đơn với suất chiếu ngay từ khi tạo (kể cả khi chưa có vé)
    public DateTime? CreatedDate { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Status { get; set; }
    public decimal? SubTotal { get; set; }
    public decimal? DiscountAmount { get; set; }

    public Customer Customer { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<BookingPromotion> BookingPromotions { get; set; } = new List<BookingPromotion>();
    public ICollection<BookingQR> QRs { get; set; } = new List<BookingQR>();
    public Invoice? Invoice { get; set; }
}

public class BookingQR
{
    public long QRID { get; set; }
    public int BookingID { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";   // Active | Revoked

    public Booking Booking { get; set; } = null!;
}
