using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-8.7 Quản lý danh mục (thể loại) phim</summary>
[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _service;

    public GenresController(IGenreService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<GenreDto>>> GetAll(CancellationToken ct = default) => Ok(await _service.GetAllAsync(ct: ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<GenreDto>> Get(int id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct: ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<GenreDto>> Create([FromBody] GenreUpsertDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = created.GenreID }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<GenreDto>> Update(int id, [FromBody] GenreUpsertDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct: ct);
        return NoContent();
    }
}
