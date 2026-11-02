using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class CompletePaymentRequest
{
    [Required]
    public string Result { get; set; } = string.Empty;
}