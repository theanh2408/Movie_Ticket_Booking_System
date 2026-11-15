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

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> Search([FromQuery] string? keyword, [FromQuery] string? status) =>
        Ok(await _service.SearchAsync(keyword, status));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId) => Ok(await _service.GetAsync(userId));

    /// <summary>Thêm khách hàng. Password không bắt buộc (khách có thể tự đăng ký mật khẩu sau).</summary>
    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { userId = created.UserID }, created);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int userId, [FromBody] UpdateAccountDto dto) =>
        Ok(await _service.UpdateAsync(userId, dto));

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Delete(int userId)
    {
        await _service.DeleteAsync(userId);
        return NoContent();
    }
}
