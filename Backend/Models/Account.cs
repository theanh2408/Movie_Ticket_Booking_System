namespace Backend.Models;

public class Account
{
    public int UserID { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? DOB { get; set; }
    public string? Gender { get; set; }
    public string? PasswordHash { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }

    public Manager? Manager { get; set; }
    public Staff? Staff { get; set; }
    public Customer? Customer { get; set; }
}

public class Manager
{
    public int ManagerID { get; set; }
    public int UserID { get; set; }
    public int? CinemaID { get; set; }

    public Account Account { get; set; } = null!;
    public Cinema Cinema { get; set; } = null!;
}

public class Staff
{
    public int StaffID { get; set; }
    public int UserID { get; set; }
    public int? CinemaID { get; set; }

    public Account Account { get; set; } = null!;
    public Cinema Cinema { get; set; } = null!;
}

public class Customer
{
    public int CustomerID { get; set; }
    public int UserID { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
