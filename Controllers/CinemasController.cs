using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CinemasController : ControllerBase
{
    private readonly ICinemaService _service;

    public CinemasController(ICinemaService service) => _service = service;

    /// <summary>Danh sách rạp (Admin: tất cả; Quản lý/Nhân viên: rạp của mình)</summary>
    [HttpGet]
    public async Task<ActionResult<List<CinemaDto>>> GetAll() => Ok(await _service.GetAllAsync());
}
