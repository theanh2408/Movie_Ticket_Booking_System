using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    public async Task<ActionResult<PagedResult<ShowtimeResponseDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] int? movieId, [FromQuery] int? roomId,
        [FromQuery] DateOnly? date, [FromQuery] string? status, [FromQuery] string? keyword,
        [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(cinemaId, movieId, roomId, date, status, keyword, paging, ct: ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShowtimeResponseDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));

    [HttpPost]
    public async Task<ActionResult<ShowtimeResponseDto>> Create([FromBody] CreateShowtimeDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = created.ShowtimeID }, created);
    }

    /// <summary>Sửa các trường ngoài giờ chiếu: roomId, basePrice, status (Active | Inactive). Trường không truyền thì giữ nguyên.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ShowtimeResponseDto>> Update(int id, [FromBody] UpdateShowtimeDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    /// <summary>Sửa thời gian bắt đầu (chỉ khi còn ít nhất 2 tiếng trước giờ chiếu)</summary>
    [HttpPut("{id:int}/start-time")]
    public async Task<ActionResult<ShowtimeResponseDto>> UpdateStartTime(int id, [FromBody] UpdateShowtimeTimeDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateStartTimeAsync(id, dto, ct: ct));

    /// <summary>Hủy suất chiếu. Body là tùy chọn: suất chưa có vé thì có thể gọi không cần body.
    /// Chỉ cần body { "isIncident": true, "reason": "..." } khi hủy suất đã có vé mà còn dưới 60 phút (chỉ Admin).</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ShowtimeResponseDto>> Cancel(int id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CancelShowtimeDto? dto = null, CancellationToken ct = default) =>
        Ok(await _service.CancelAsync(id, dto, ct: ct));

    /// <summary>Sơ đồ ghế kèm trạng thái từng ghế của suất chiếu</summary>
    [HttpGet("{id:int}/seat-map")]
    public async Task<ActionResult<SeatMapDto>> SeatMap(int id, CancellationToken ct = default) => Ok(await _service.GetSeatMapAsync(id, ct: ct));

    /// <summary>UC-4.10 Đổi trạng thái ghế trong suất chiếu: Available | Disabled</summary>
    [HttpPut("{id:int}/seats/{seatId:int}/status")]
    public async Task<ActionResult<SeatMapItemDto>> SetSeatStatus(int id, int seatId, [FromBody] StatusDto dto, CancellationToken ct = default) =>
        Ok(await _service.SetSeatStatusAsync(id, seatId, dto.Status, ct: ct));
}
