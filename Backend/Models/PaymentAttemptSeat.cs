
namespace Backend.Models;

public partial class PaymentAttemptSeat
{
    public int PaymentAttemptId { get; set; }

    public int ShowtimeSeatId { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual PaymentAttempt? PaymentAttempt { get; set; }

    public virtual ShowtimeSeat? ShowtimeSeat { get; set; }
}
