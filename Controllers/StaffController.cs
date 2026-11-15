using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-3.x Quản lý nhân viên (Admin: toàn chuỗi; Quản lý: rạp của mình). Route dùng UserID.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class StaffController : ControllerBase
{
    private readonly IStaffService _service;

    public StaffController(IStaffService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> Search([FromQuery] string? keyword, [FromQuery] string? status) =>
        Ok(await _service.SearchAsync(keyword, status));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId) => Ok(await _service.GetAsync(userId));

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
