using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/payments/mock")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST: api/payments/mock
    [HttpPost]
    public async Task<IActionResult> CreatePaymentAttempt(
        [FromBody] CreatePaymentAttemptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Token không hợp lệ."
            });
        }

        var key = Guid.TryParse(idempotencyKey, out Guid parsedKey) ? parsedKey : Guid.NewGuid();

        try
        {
            var result = await _paymentService.CreateAttemptAsync(
                userId,
                request,
                key);

            return StatusCode(201, new
            {
                success = true,
                data = result,
                message = "Tạo lượt giữ ghế/thanh toán thành công."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    // POST: api/payments/mock/{paymentAttemptId}/complete
    [HttpPost("{paymentAttemptId}/complete")]
    public async Task<IActionResult> CompletePayment(
        int paymentAttemptId,
        [FromBody] CompletePaymentRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                success = false,
                message = "Token không hợp lệ."
            });
        }

        try
        {
            var result = await _paymentService.CompleteAttemptAsync(
                userId,
                paymentAttemptId,
                request.Result);

            return Ok(new
            {
                success = true,
                data = result,
                message = "Xử lý thanh toán thành công."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }
}
