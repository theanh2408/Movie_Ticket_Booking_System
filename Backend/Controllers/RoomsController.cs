using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-4.1 - 4.5 Quản lý phòng chiếu</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _service;

    public RoomsController(IRoomService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<PagedResult<RoomResponseDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, paging, ct: ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));

    [HttpPost]
    public async Task<ActionResult<RoomResponseDto>> Create([FromBody] RoomUpsertDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = created.RoomID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Update(int id, [FromBody] RoomUpsertDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    /// <summary>Xóa vĩnh viễn (kèm ghế). Bị chặn nếu phòng đã có suất chiếu.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct: ct);
        return NoContent();
    }
}
