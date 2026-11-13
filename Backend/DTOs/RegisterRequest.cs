
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class RegisterRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}
