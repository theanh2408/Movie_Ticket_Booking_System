
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreatePaymentAttemptRequest
{
    [Required]
    public int ShowtimeId { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(8)]
    public List<int> SeatIds { get; set; } = new();
}
