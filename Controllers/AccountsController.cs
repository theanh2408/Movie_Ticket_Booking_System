using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-1.x Quản lý tài khoản (chỉ Admin)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service) => _service = service;

    /// <summary>Danh sách + tìm kiếm theo tên/email/SĐT, lọc theo vai trò, trạng thái, rạp</summary>
    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> Search(
        [FromQuery] string? keyword, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int? cinemaId) =>
        Ok(await _service.SearchAsync(keyword, role, status, cinemaId));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId) => Ok(await _service.GetAsync(userId));

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountDto dto)
    {
        var created = await _service.CreateAsync(dto, requirePassword: true);
        return CreatedAtAction(nameof(Get), new { userId = created.UserID }, created);
    }

    /// <summary>Sửa thông tin (không cho sửa email)</summary>
    [HttpPut("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int userId, [FromBody] UpdateAccountDto dto) =>
        Ok(await _service.UpdateAsync(userId, dto));

    /// <summary>Xóa mềm: Status = Inactive, xóa thông tin đăng nhập</summary>
    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Delete(int userId)
    {
        await _service.SoftDeleteAsync(userId);
        return NoContent();
    }

    /// <summary>UC-1.6 Phân quyền (không được gán/đổi vai trò Admin)</summary>
    [HttpPut("{userId:int}/role")]
    public async Task<ActionResult<AccountResponseDto>> ChangeRole(int userId, [FromBody] ChangeRoleDto dto) =>
        Ok(await _service.ChangeRoleAsync(userId, dto));
}
