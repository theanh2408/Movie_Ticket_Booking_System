using Microsoft.EntityFrameworkCore;

namespace Backend.Common;

/// <summary>
/// Bắt mọi lỗi và trả JSON { "message": "..." } với mã HTTP phù hợp,
/// nên Controller không cần try/catch.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly bool _showDetails;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _showDetails = config.GetValue<bool>("ShowErrorDetails");
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            if (ctx.Response.HasStarted) throw;

            int status;
            string message;

            switch (ex)
            {
                case AppException app:
                    status = app.StatusCode;
                    message = app.Message;
                    break;

                case DbUpdateException:
                    _logger.LogError(ex, "Lỗi lưu database");
                    status = 409;
                    message = "Không thể lưu: dữ liệu bị trùng hoặc đang được sử dụng ở nơi khác."
                              + (_showDetails ? " Chi tiết: " + ex.GetBaseException().Message : "");
                    break;

                default:
                    _logger.LogError(ex, "Lỗi không xử lý được");
                    status = 500;
                    message = _showDetails
                        ? ex.GetBaseException().Message
                        : "Lỗi hệ thống, vui lòng thử lại sau.";
                    break;
            }

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { message });
        }
    }
}
