using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MovieTicketBooking.Api.Controllers;

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
        // Lấy UserID từ JWT
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Token không hợp lệ."
            });
        }

        // Kiểm tra Idempotency Key
        if (!Guid.TryParse(idempotencyKey, out Guid key))
        {
            return BadRequest(new
            {
                message = "Idempotency-Key không hợp lệ hoặc bị thiếu."
            });
        }

        try
        {
            var result = await _paymentService.CreateAttemptAsync(
                userId,
                request,
                key);

            return StatusCode(201, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // POST: api/payments/mock/{paymentAttemptId}/complete
    [HttpPost("{paymentAttemptId}/complete")]
    public async Task<IActionResult> CompletePayment(
        int paymentAttemptId,
        [FromBody] CompletePaymentRequest request)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new
            {
                message = "Token không hợp lệ."
            });
        }

        try
        {
            var result = await _paymentService.CompleteAttemptAsync(
                userId,
                paymentAttemptId,
                request.Result);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}