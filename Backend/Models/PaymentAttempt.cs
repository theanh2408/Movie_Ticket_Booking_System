
using System;

namespace Backend.Models;

public partial class PaymentAttempt
{
    public int PaymentAttemptId { get; set; }

    public int UserId { get; set; }

    public int ShowtimeId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = "PENDING";

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public Guid IdempotencyKey { get; set; }

    public virtual ICollection<PaymentAttemptSeat> PaymentAttemptSeats
    { get; set; } = new List<PaymentAttemptSeat>();
}
