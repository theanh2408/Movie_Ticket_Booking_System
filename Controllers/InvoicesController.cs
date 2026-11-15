using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

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
