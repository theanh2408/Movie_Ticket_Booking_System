using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-11.x Thống kê báo cáo. Mặc định 30 ngày gần nhất; from/to dạng yyyy-MM-dd.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _service;

    public StatisticsController(IStatisticsService service) => _service = service;

    /// <summary>UC-11.9 Xuất báo cáo ra file CSV. type: revenue | tickets-sold | bookings | occupancy | revenue-by-movie | showtime-performance</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string type, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default)
    {
        var (content, fileName) = await _service.ExportCsvAsync(type, from, to, cinemaId, ct: ct);
        return File(content, "text/csv", fileName);
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueStatsDto>> Revenue(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.RevenueAsync(from, to, cinemaId, ct: ct));

    [HttpGet("tickets-sold")]
    public async Task<ActionResult<TicketStatsDto>> TicketsSold(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.TicketsSoldAsync(from, to, cinemaId, ct: ct));

    [HttpGet("bookings")]
    public async Task<ActionResult<BookingStatsDto>> Bookings(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.BookingsAsync(from, to, cinemaId, ct: ct));

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyStatsDto>> Occupancy(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.OccupancyAsync(from, to, cinemaId, ct: ct));

    [HttpGet("revenue-by-movie")]
    public async Task<ActionResult<MovieRevenueStatsDto>> RevenueByMovie(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.RevenueByMovieAsync(from, to, cinemaId, ct: ct));

    [HttpGet("showtime-performance")]
    public async Task<ActionResult<List<ShowtimePerformanceDto>>> ShowtimePerformance(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int? cinemaId, CancellationToken ct = default) =>
        Ok(await _service.ShowtimePerformanceAsync(from, to, cinemaId, ct: ct));
}
