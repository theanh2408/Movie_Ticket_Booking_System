using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _service;

    public MoviesController(IMovieService service) => _service = service;

    // GET api/movies
    [HttpGet]
    public async Task<ActionResult<List<MovieResponseDto>>> GetAll() =>
        Ok(await _service.GetAllAsync());

    // GET api/movies/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieResponseDto>> GetById(int id)
    {
        var movie = await _service.GetByIdAsync(id);
        return movie is null ? NotFound(new { message = "Không tìm thấy phim" }) : Ok(movie);
    }

    // POST api/movies  <-- chức năng THÊM PHIM
    [HttpPost]
    public async Task<ActionResult<MovieResponseDto>> Create([FromBody] CreateMovieDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.MovieID }, created);
        }
        catch (ArgumentException ex)          { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex)  { return Conflict(new { message = ex.Message }); }
    }
}
