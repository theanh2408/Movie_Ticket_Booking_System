using System;
using System.Collections.Generic;

namespace Backend.Models;

public partial class Account
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public DateOnly? Dob { get; set; }

    public string? Gender { get; set; }

    public string? PasswordHash { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
