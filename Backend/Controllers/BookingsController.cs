using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-6.2, 6.6 Xem / tìm kiếm đơn đặt vé (Admin: toàn chuỗi; Quản lý: rạp của mình)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _service;

    public BookingsController(IBookingService service) => _service = service;

    /// <summary>keyword: mã đơn, tên/SĐT/email khách. from/to: ngày tạo đơn (yyyy-MM-dd). Phân trang: page, pageSize; sắp xếp: sortBy (id, createdDate, totalAmount, status), sortDir.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingListItemDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, status, from, to, paging, ct: ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDetailDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));
}
