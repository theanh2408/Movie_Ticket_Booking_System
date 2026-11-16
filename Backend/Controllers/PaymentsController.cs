using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-6.7, 7.6 Lịch sử giao dịch (bảng Payment)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, status, from, to));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id) => Ok(await _service.GetAsync(id));
}

/// <summary>UC-9.x Hóa đơn</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service) => _service = service;

    /// <summary>Tìm theo mã hóa đơn / mã đơn (keyword); lọc theo rạp, ngày phát hành, trạng thái</summary>
    [HttpGet]
    public async Task<ActionResult<List<InvoiceListItemDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, status, from, to));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDetailDto>> Get(int id) => Ok(await _service.GetAsync(id));

    /// <summary>UC-9.6 Hủy hóa đơn (đổi trạng thái Cancelled). Chỉ Admin, chỉ khi khách đã hủy vé.</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<InvoiceDetailDto>> Cancel(int id) => Ok(await _service.CancelAsync(id));
}

/// <summary>UC-11.x Thống kê báo cáo. Mặc định 30 ngày gần nhất; from/to dạng yyyy-MM-dd.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _service;

    public StatisticsController(IStatisticsService service) => _service = service;

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueStatsDto>> Revenue(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.RevenueAsync(from, to, cinemaId));

    [HttpGet("tickets-sold")]
    public async Task<ActionResult<TicketStatsDto>> TicketsSold(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.TicketsSoldAsync(from, to, cinemaId));

    [HttpGet("bookings")]
    public async Task<ActionResult<BookingStatsDto>> Bookings(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.BookingsAsync(from, to, cinemaId));

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyStatsDto>> Occupancy(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.OccupancyAsync(from, to, cinemaId));

    [HttpGet("revenue-by-movie")]
    public async Task<ActionResult<MovieRevenueStatsDto>> RevenueByMovie(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.RevenueByMovieAsync(from, to, cinemaId));

    [HttpGet("showtime-performance")]
    public async Task<ActionResult<List<ShowtimePerformanceDto>>> ShowtimePerformance(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId) =>
        Ok(await _service.ShowtimePerformanceAsync(from, to, cinemaId));
}
