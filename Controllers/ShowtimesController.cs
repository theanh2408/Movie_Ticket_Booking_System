using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-5.x Quản lý suất chiếu (Admin: toàn chuỗi; Quản lý: rạp của mình)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class ShowtimesController : ControllerBase
{
    private readonly IShowtimeService _service;

    public ShowtimesController(IShowtimeService service) => _service = service;

    /// <summary>Danh sách + tìm theo tên phim (keyword) + lọc theo rạp, phim, phòng, ngày, trạng thái</summary>
    [HttpGet]
    public async Task<ActionResult<List<ShowtimeResponseDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] int? movieId, [FromQuery] int? roomId,
        [FromQuery] DateOnly? date, [FromQuery] string? status, [FromQuery] string? keyword) =>
        Ok(await _service.SearchAsync(cinemaId, movieId, roomId, date, status, keyword));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShowtimeResponseDto>> Get(int id) => Ok(await _service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<ShowtimeResponseDto>> Create([FromBody] CreateShowtimeDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.ShowtimeID }, created);
    }

    /// <summary>Sửa thời gian bắt đầu (chỉ khi còn ít nhất 2 tiếng trước giờ chiếu)</summary>
    [HttpPut("{id:int}/start-time")]
    public async Task<ActionResult<ShowtimeResponseDto>> UpdateStartTime(int id, [FromBody] UpdateShowtimeTimeDto dto) =>
        Ok(await _service.UpdateStartTimeAsync(id, dto));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ShowtimeResponseDto>> Cancel(int id, [FromBody] CancelShowtimeDto dto) =>
        Ok(await _service.CancelAsync(id, dto));

    /// <summary>Sơ đồ ghế kèm trạng thái từng ghế của suất chiếu</summary>
    [HttpGet("{id:int}/seat-map")]
    public async Task<ActionResult<SeatMapDto>> SeatMap(int id) => Ok(await _service.GetSeatMapAsync(id));

    /// <summary>UC-4.10 Đổi trạng thái ghế trong suất chiếu: Available | Disabled</summary>
    [HttpPut("{id:int}/seats/{seatId:int}/status")]
    public async Task<ActionResult<SeatMapItemDto>> SetSeatStatus(int id, int seatId, [FromBody] StatusDto dto) =>
        Ok(await _service.SetSeatStatusAsync(id, seatId, dto.Status));
}
