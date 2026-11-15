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
    public async Task<ActionResult<List<RoomResponseDto>>> Search([FromQuery] int? cinemaId, [FromQuery] string? keyword) =>
        Ok(await _service.SearchAsync(cinemaId, keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Get(int id) => Ok(await _service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<RoomResponseDto>> Create([FromBody] RoomUpsertDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.RoomID }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomResponseDto>> Update(int id, [FromBody] RoomUpsertDto dto) =>
        Ok(await _service.UpdateAsync(id, dto));

    /// <summary>Xóa vĩnh viễn (kèm ghế). Bị chặn nếu phòng đã có suất chiếu.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
