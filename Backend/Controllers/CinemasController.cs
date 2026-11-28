using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CinemasController : ControllerBase
{
    private readonly ICinemaService _service;

    public CinemasController(ICinemaService service) => _service = service;

    /// <summary>Công khai cho Customer: chỉ các rạp đang mở cửa (Status = Opened)</summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CinemaDto>>> GetPublic(CancellationToken ct = default) => Ok(await _service.GetPublicAsync(ct: ct));

    /// <summary>Quản trị (Admin/Quản lý/Nhân viên): Admin thấy tất cả rạp; Quản lý/Nhân viên chỉ thấy rạp của mình</summary>
    [HttpGet]
    [Authorize(Roles = AppRoles.AdminManagerStaff)]
    public async Task<ActionResult<List<CinemaDto>>> GetAll(CancellationToken ct = default) => Ok(await _service.GetAllAsync(ct: ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.AdminManagerStaff)]
    public async Task<ActionResult<CinemaDto>> GetById(int id, CancellationToken ct = default) => Ok(await _service.GetByIdAsync(id, ct: ct));

    /// <summary>Thêm rạp (chỉ Admin)</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CinemaDto>> Create([FromBody] CinemaUpsertDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = created.CinemaID }, created);
    }

    /// <summary>Sửa thông tin rạp (chỉ Admin)</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CinemaDto>> Update(int id, [FromBody] CinemaUpsertDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    /// <summary>Đổi trạng thái rạp: Opened | Closed (chỉ Admin; không đóng được khi còn suất chiếu sắp tới)</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<CinemaDto>> UpdateStatus(int id, [FromBody] StatusDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateStatusAsync(id, dto.Status, ct: ct));
}
