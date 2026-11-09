
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MovieTicketBooking.Api.Data;

namespace MovieTicketBooking.Api.Controllers;

[ApiController]
[Route("api/db-health")]
public class DbHealthController : ControllerBase
{
    private readonly DbConnectionFactory _connectionFactory;

    public DbHealthController(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            await using var connection =
                _connectionFactory.CreateConnection();

            await connection.OpenAsync();

            await using var command =
                new SqlCommand("SELECT DB_NAME()", connection);

            var databaseName = (string?)await command.ExecuteScalarAsync();

            return Ok(new
            {
                status = "ok",
                database = databaseName
            });
        }
        catch (Exception)
        {
            return StatusCode(503, new
            {
                status = "error",
                message = "Database connection failed"
            });
        }
    }
}
