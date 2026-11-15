using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-8.x Quản lý phim. Xem/tìm kiếm/lọc: ai cũng gọi được. Thêm/sửa/xóa: Admin.</summary>
[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _service;

    public MoviesController(IMovieService service) => _service = service;

    /// <summary>Danh sách phim; tìm theo tên (keyword), lọc theo trạng thái, thể loại, độ tuổi</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<MovieResponseDto>>> GetAll(
        [FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] int? genreId, [FromQuery] string? ageRating) =>
        Ok(await _service.SearchAsync(keyword, status, genreId, ageRating));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<MovieResponseDto>> GetById(int id)
    {
        var movie = await _service.GetByIdAsync(id);
        return movie is null ? NotFound(new { message = "Không tìm thấy phim" }) : Ok(movie);
    }

    // POST api/movies  <-- chức năng THÊM PHIM (demo)
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MovieResponseDto>> Create([FromBody] CreateMovieDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.MovieID }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MovieResponseDto>> Update(int id, [FromBody] CreateMovieDto dto) =>
        Ok(await _service.UpdateAsync(id, dto));

    /// <summary>Xóa phim. Bị chặn nếu phim đã có suất chiếu (hãy đổi trạng thái thay vì xóa).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>UC-8.8 Cập nhật trạng thái: ComingSoon | NowShowing | Ended | Inactive</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<ActionResult<MovieResponseDto>> UpdateStatus(int id, [FromBody] StatusDto dto) =>
        Ok(await _service.UpdateStatusAsync(id, dto.Status));
}
