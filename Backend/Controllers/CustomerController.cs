using Backend.Data;
using Backend.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CustomerController(ApplicationDbContext context) => _context = context;

    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> GetCustomer(int customerId, CancellationToken cancellationToken)
    {
        var customer = await GetOwnedCustomerAsync(customerId, cancellationToken);
        if (customer == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        return Ok(new
        {
            success = true,
            data = new
            {
                customerId = customer.CustomerId,
                userId = customer.UserId,
                fullName = customer.User!.FullName,
                phone = customer.User.Phone,
                email = customer.User.Email,
                dob = customer.User.Dob,
                gender = customer.User.Gender
            },
            message = "Lấy thông tin khách hàng thành công."
        });
    }

    [HttpPatch("{customerId:int}")]
    public async Task<IActionResult> UpdateCustomer(int customerId, [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var customer = await GetOwnedCustomerAsync(customerId, cancellationToken);
        if (customer == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { success = false, message = "FullName và Phone không được để trống." });

        var account = customer.User!;
        account.FullName = request.FullName.Trim();
        account.Phone = request.Phone.Trim();
        account.Dob = request.Dob.HasValue ? DateOnly.FromDateTime(request.Dob.Value) : null;
        account.Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim();
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new { customerId, fullName = account.FullName, phone = account.Phone, email = account.Email, dob = account.Dob, gender = account.Gender },
            message = "Cập nhật thông tin khách hàng thành công."
        });
    }

    private async Task<Models.Customer?> GetOwnedCustomerAsync(int customerId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _context.Customers.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.UserId == userId, cancellationToken);
    }
}
