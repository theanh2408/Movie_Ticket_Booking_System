using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>Thông tin tài khoản của chính người đang đăng nhập (UC-3.2, 3.3 phía nhân viên).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IAccountService _service;

    public ProfileController(IAccountService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AccountResponseDto>> Get(CancellationToken ct = default) => Ok(await _service.GetMeAsync(ct: ct));

    [HttpPut]
    public async Task<ActionResult<AccountResponseDto>> Update([FromBody] UpdateAccountDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateMeAsync(dto, ct: ct));
}
