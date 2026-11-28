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

    /// <summary>Danh sách phim; tìm theo tên (keyword), lọc theo trạng thái, thể loại, độ tuổi. Phân trang: page, pageSize; sắp xếp: sortBy (id, title, releaseDate, duration, status), sortDir.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<MovieResponseDto>>> GetAll(
        [FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] int? genreId, [FromQuery] string? ageRating,
        [FromQuery] PageQuery paging, CancellationToken ct = default) =>
        Ok(await _service.SearchAsync(keyword, status, genreId, ageRating, paging, ct: ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<MovieResponseDto>> GetById(int id, CancellationToken ct = default)
    {
        var movie = await _service.GetByIdAsync(id, ct: ct);
        return movie is null ? NotFound(new { message = "Không tìm thấy phim" }) : Ok(movie);
    }

    // POST api/movies  <-- chức năng THÊM PHIM (demo)
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MovieResponseDto>> Create([FromBody] CreateMovieDto dto, CancellationToken ct = default)
    {
        var created = await _service.CreateAsync(dto, ct: ct);
        return CreatedAtAction(nameof(GetById), new { id = created.MovieID }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MovieResponseDto>> Update(int id, [FromBody] CreateMovieDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateAsync(id, dto, ct: ct));

    /// <summary>Xóa phim. Bị chặn nếu phim đã có suất chiếu (hãy đổi trạng thái thay vì xóa).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        await _service.DeleteAsync(id, ct: ct);
        return NoContent();
    }

    /// <summary>UC-8.8 Cập nhật trạng thái (chỉ Admin): ComingSoon | NowShowing | Ended | Inactive</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<MovieResponseDto>> UpdateStatus(int id, [FromBody] StatusDto dto, CancellationToken ct = default) =>
        Ok(await _service.UpdateStatusAsync(id, dto.Status, ct: ct));
}
