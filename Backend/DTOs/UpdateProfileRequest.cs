using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }

    [StringLength(20)]
    public string? Gender { get; set; }
}