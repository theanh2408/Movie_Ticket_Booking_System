using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-2.x Quản lý khách hàng (Admin, Quản lý). Route dùng UserID.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    /// <summary>Phân trang: page, pageSize; sắp xếp: sortBy (id, fullName, email, role, status), sortDir.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccountResponseDto>>> Search(
        [FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(keyword, status, paging, ct: ct));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId, CancellationToken ct = default) => Ok(await _service.GetAsync(userId, ct: ct));

    /// <summary>Thêm khách hàng. Password không bắt buộc (khách có thể tự đăng ký mật khẩu sau).</summary>
    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(Get), new { userId = created.UserID }, created);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int userId, [FromBody] UpdateAccountDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(userId, dto, ct: ct));

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Delete(int userId, CancellationToken ct = default)
    {
        await _service.DeleteAsync(userId, ct: ct);
        return NoContent();
    }
}
