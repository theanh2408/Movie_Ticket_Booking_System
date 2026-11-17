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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CinemasController : ControllerBase
{
    private readonly ICinemaService _service;

    public CinemasController(ICinemaService service) => _service = service;

    /// <summary>Danh sách rạp (Admin: tất cả; Quản lý/Nhân viên: rạp của mình)</summary>
    [HttpGet]
    public async Task<ActionResult<List<CinemaDto>>> GetAll() => Ok(await _service.GetAllAsync());
}

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

/// <summary>UC-4.6 - 4.9 Quản lý ghế và sơ đồ ghế của phòng</summary>
[ApiController]
[Route("api")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _service;

    public SeatsController(ISeatService service) => _service = service;

    [HttpGet("rooms/{roomId:int}/seats")]
    public async Task<ActionResult<List<SeatResponseDto>>> List(int roomId) => Ok(await _service.ListByRoomAsync(roomId));

    /// <summary>Sơ đồ ghế của phòng, gom theo hàng</summary>
    [HttpGet("rooms/{roomId:int}/seat-map")]
    public async Task<ActionResult<SeatMapDto>> SeatMap(int roomId) => Ok(await _service.GetRoomSeatMapAsync(roomId));

    [HttpPost("rooms/{roomId:int}/seats")]
    public async Task<ActionResult<SeatResponseDto>> Create(int roomId, [FromBody] SeatCreateDto dto) =>
        Ok(await _service.CreateAsync(roomId, dto));

    /// <summary>Tạo hàng loạt: RowCount hàng (A, B, ...) x SeatsPerRow ghế. Ghế đã có sẽ được bỏ qua.</summary>
    [HttpPost("rooms/{roomId:int}/seats/bulk")]
    public async Task<ActionResult> BulkCreate(int roomId, [FromBody] SeatBulkCreateDto dto)
    {
        var added = await _service.BulkCreateAsync(roomId, dto);
        return Ok(new { added, message = added == 0 ? "Không có ghế mới nào được thêm" : $"Đã thêm {added} ghế" });
    }

    [HttpPut("seats/{seatId:int}")]
    public async Task<ActionResult<SeatResponseDto>> Update(int seatId, [FromBody] SeatCreateDto dto) =>
        Ok(await _service.UpdateAsync(seatId, dto));

    [HttpDelete("seats/{seatId:int}")]
    public async Task<IActionResult> Delete(int seatId)
    {
        await _service.DeleteAsync(seatId);
        return NoContent();
    }
}

/// <summary>UC-3.x Quản lý nhân viên (Role = Staff; Admin và Quản lý). Route dùng UserID.</summary>
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
