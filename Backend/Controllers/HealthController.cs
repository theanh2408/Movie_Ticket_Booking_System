using Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/db-health")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "error",
                    message = "Không thể kết nối cơ sở dữ liệu."
                });
            }

            return Ok(new
            {
                status = "ok",
                database = _context.Database.GetDbConnection().Database,
                provider = _context.Database.ProviderName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }
}
