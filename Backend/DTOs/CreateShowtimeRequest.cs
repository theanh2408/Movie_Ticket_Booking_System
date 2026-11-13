using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CreateShowtimeRequest
{
    [Required]
    public int MovieId { get; set; }

    [Required]
    public int RoomId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public TimeOnly StartHour { get; set; }

    [Required]
    [Range(10000, 10000000)]
    public decimal BasePrice { get; set; }
}
