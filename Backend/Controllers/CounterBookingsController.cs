using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>Đặt vé tại quầy cho nhân viên (Staff) - thanh toán ngay tại quầy.</summary>
[ApiController]
[Route("api/counter-bookings")]
[Authorize(Roles = AppRoles.AdminManagerStaff)]
public class CounterBookingsController : ControllerBase
{
    private readonly ICounterBookingService _service;

    public CounterBookingsController(ICounterBookingService service) => _service = service;

    /// <summary>Tạo đơn + vé + thanh toán + hóa đơn + mã QR trong một bước. Nhân viên chỉ bán vé của rạp mình.</summary>
    [HttpPost]
    public async Task<ActionResult<CounterBookingResultDto>> Create([FromBody] CounterBookingDto dto, CancellationToken ct = default)
    {
        var result = await _service.CreateAsync(dto, ct: ct);
        return Created($"/api/bookings/{result.Booking.BookingID}", result);
    }
}
