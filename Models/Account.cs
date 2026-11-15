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
    public string? Status { get; set; }         // cột mới (file SQL cập nhật lần 2)

    public Manager? Manager { get; set; }
    public Staff? Staff { get; set; }
    public Customer? Customer { get; set; }
}
