using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class ApplyPromotionRequest
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
}
