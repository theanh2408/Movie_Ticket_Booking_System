using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-10.x Chương trình khuyến mãi</summary>
[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _service;

    public PromotionsController(IPromotionService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<ActionResult<PagedResult<PromotionResponseDto>>> Search(
        [FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(keyword, status, paging, ct: ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<ActionResult<PromotionResponseDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> Create([FromBody] PromotionUpsertDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = created.PromotionID }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> Update(int id, [FromBody] PromotionUpsertDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct: ct);
        return NoContent();
    }

    /// <summary>UC-10.6 Bật/tắt chương trình: Active | Inactive</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> UpdateStatus(int id, [FromBody] StatusDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateStatusAsync(id, dto.Status, ct: ct));

    /// <summary>UC-10.7 Kiểm tra mã và tính số tiền giảm (khách hàng nhập mã thủ công)</summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<ActionResult<ApplyPromotionResultDto>> Validate([FromBody] ApplyPromotionDto dto, CancellationToken ct = default) =>
        Ok(await _service.ApplyAsync(dto, ct: ct));
}
