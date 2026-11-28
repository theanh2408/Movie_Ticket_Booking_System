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

    /// <summary>keyword: mã hóa đơn, mã đơn, tên / email / SĐT khách hàng, mã vé (TicketID hoặc mã vạch). Lọc theo rạp, ngày phát hành, trạng thái.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, status, from, to, paging, ct: ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDetailDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));

    /// <summary>UC-9.5 Xuất hóa đơn ra file: format = pdf (mặc định) | xml</summary>
    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(int id, [FromQuery] string format = "pdf", CancellationToken ct = default)
    {
        var (content, contentType, fileName) = await _service.ExportAsync(id, format, ct: ct);
        return File(content, contentType, fileName);
    }

    /// <summary>UC-9.6 Hủy hóa đơn (đổi trạng thái Cancelled). Chỉ Admin, chỉ khi khách đã hủy vé.</summary>
    [HttpPut("{id:int}/cancel")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<InvoiceDetailDto>> Cancel(int id, CancellationToken ct = default) => Ok(await _service.CancelAsync(id, ct: ct));
}
