using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-4.6 - 4.9 Quản lý ghế và sơ đồ ghế của phòng</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _service;

    public SeatsController(ISeatService service) => _service = service;

    [HttpGet("rooms/{roomId:int}/seats")]
    public async Task<ActionResult<List<SeatResponseDto>>> List(int roomId, CancellationToken ct = default) => Ok(await _service.ListByRoomAsync(roomId, ct: ct));

    /// <summary>Sơ đồ ghế của phòng, gom theo hàng</summary>
    [HttpGet("rooms/{roomId:int}/seat-map")]
    public async Task<ActionResult<SeatMapDto>> SeatMap(int roomId, CancellationToken ct = default) => Ok(await _service.GetRoomSeatMapAsync(roomId, ct: ct));

    [HttpPost("rooms/{roomId:int}/seats")]
    public async Task<ActionResult<SeatResponseDto>> Create(int roomId, [FromBody] SeatCreateDto dto, CancellationToken ct = default) =>
        Ok(await _service.CreateAsync(roomId, dto, ct: ct));

    /// <summary>Tạo hàng loạt: RowCount hàng (A, B, ...) x SeatsPerRow ghế. Ghế đã có sẽ được bỏ qua.</summary>
    [HttpPost("rooms/{roomId:int}/seats/bulk")]
    public async Task<ActionResult> BulkCreate(int roomId, [FromBody] SeatBulkCreateDto dto, CancellationToken ct = default)
    {
        var added = await _service.BulkCreateAsync(roomId, dto, ct: ct);
        return Ok(new { added, message = added == 0 ? "Không có ghế mới nào được thêm" : $"Đã thêm {added} ghế" });
    }

    [HttpPut("seats/{seatId:int}")]
    public async Task<ActionResult<SeatResponseDto>> Update(int seatId, [FromBody] SeatCreateDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(seatId, dto, ct: ct));

    [HttpDelete("seats/{seatId:int}")]
    public async Task<IActionResult> Delete(int seatId, CancellationToken ct = default)
    {
        await _service.DeleteAsync(seatId, ct: ct);
        return NoContent();
    }
}
