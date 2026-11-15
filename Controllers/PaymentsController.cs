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
