using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-6.9 Kiểm tra vé bằng mã QR / mã vạch trước khi khách vào phòng chiếu.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminManagerStaff)]
public class TicketsController : ControllerBase
{
    private readonly ITicketCheckService _service;

    public TicketsController(ITicketCheckService service) => _service = service;

    /// <summary>Chỉ kiểm tra vé có hợp lệ không (không đổi trạng thái). Luôn trả 200, xem isValid và reason.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<TicketCheckResultDto>> Verify([FromBody] TokenDto dto, CancellationToken ct = default) =>
        Ok(await _service.VerifyAsync(dto.Token, ct: ct));

    /// <summary>Xác nhận cho khách vào phòng: các vé hợp lệ chuyển Unused -> Used. Vé không hợp lệ trả 409 kèm lý do.</summary>
    [HttpPost("use")]
    public async Task<ActionResult<TicketCheckResultDto>> Use([FromBody] TokenDto dto, CancellationToken ct = default) =>
        Ok(await _service.UseAsync(dto.Token, ct: ct));
}
