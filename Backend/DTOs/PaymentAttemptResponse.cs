
namespace Backend.DTOs;

public class PaymentAttemptResponse
{
    public int PaymentAttemptId { get; set; }

    public int ShowtimeId { get; set; }

    public List<int> SeatIds { get; set; } = new();

    public decimal Amount { get; set; }

    public string Status { get; set; } = "PENDING";

    public DateTime ExpiresAt { get; set; }

    public int RemainingSeconds { get; set; }

    public string? QrToken { get; set; }

    public List<string> Barcodes { get; set; } = new();
}
