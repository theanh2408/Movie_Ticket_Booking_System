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
