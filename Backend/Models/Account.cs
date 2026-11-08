using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models;

[Table("Account")]
public class Account
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [MaxLength(100)]
    public string? FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [Column("DOB")]
    public DateOnly? Dob { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Manager? Manager { get; set; }
}
