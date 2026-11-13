using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.Controllers;

[ApiController]
[Route("api/customers/{customerId:int}/bookings")]
[Authorize]
public class CustomerBookingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentService _paymentService;

    public CustomerBookingsController(ApplicationDbContext context, PaymentService paymentService)
    {
        _context = context;
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking(int customerId, [FromBody] CreatePaymentAttemptRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        var userId = await GetOwnedUserIdAsync(customerId);
        if (userId == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });
        var key = Guid.TryParse(idempotencyKey, out var parsedKey) ? parsedKey : Guid.NewGuid();
        try
        {
            var result = await _paymentService.CreateAttemptAsync(userId.Value, request, key);
            return StatusCode(201, new { success = true, data = result, message = "Tạo đơn đặt vé thành công." });
        }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { success = false, message = ex.Message }); }
    }

    [HttpPost("{bookingId:int}/cancel")]
    public async Task<IActionResult> CancelBooking(int customerId, int bookingId, CancellationToken cancellationToken)
    {
        var userId = await GetOwnedUserIdAsync(customerId);
        if (userId == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        var booking = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Tickets)
            .Include(b => b.Payments)
            .Include(b => b.BookingQRs)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId, cancellationToken);

        if (booking == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt vé." });

        if (booking.Status == "CANCELLED") return Conflict(new { success = false, message = "Đơn đặt vé này đã được hủy trước đó." });

        var payment = booking.Payments.Where(p => p.PaymentStatus == "SUCCESS" && p.PaymentDate.HasValue)
            .OrderByDescending(p => p.PaymentDate).FirstOrDefault();
        if (payment == null) return Conflict(new { success = false, message = "Đơn đặt vé chưa thanh toán thành công." });

        // Nghiệp vụ: Không thể hủy vé đã được in cứng tại quầy
        if (booking.Tickets.Any(t => t.Status == "PRINTED"))
            return Conflict(new { success = false, message = "Vé đã được in tại quầy, không thể hủy." });

        // Nghiệp vụ: Chỉ được hủy trong vòng 30 phút sau khi thanh toán
        if (DateTime.UtcNow > payment.PaymentDate!.Value.AddMinutes(30))
            return Conflict(new { success = false, message = "Chỉ được hủy vé trong vòng 30 phút sau khi thanh toán thành công." });

        booking.Status = "CANCELLED";
        foreach (var ticket in booking.Tickets) ticket.Status = "CANCELLED";
        foreach (var qr in booking.BookingQRs) qr.Status = "Revoked";

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { success = true, data = new { bookingId, status = booking.Status }, message = "Hủy đơn đặt vé thành công." });
    }

    [HttpGet("{bookingId:int}")]
    public async Task<IActionResult> GetBooking(int customerId, int bookingId, CancellationToken cancellationToken)
    {
        var userId = await GetOwnedUserIdAsync(customerId);
        if (userId == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        var booking = await _context.Bookings.AsNoTracking()
            .Where(b => b.BookingId == bookingId && b.CustomerId == customerId)
            .Include(b => b.Payments).Include(b => b.BookingQRs).Include(b => b.Tickets)
            .FirstOrDefaultAsync(cancellationToken);
        if (booking == null) return NotFound(new { success = false, message = "Không tìm thấy đơn đặt vé." });

        var remainingSeconds = booking.Status == "PENDING" && booking.ExpiredAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((booking.ExpiredAt.Value - DateTime.UtcNow).TotalSeconds))
            : 0;
        return Ok(new
        {
            success = true,
            data = new
            {
                bookingId = booking.BookingId,
                status = booking.Status,
                totalAmount = booking.TotalAmount,
                createdDate = booking.CreatedDate,
                expiresAt = booking.ExpiredAt,
                remainingSeconds,
                paymentStatus = booking.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.PaymentStatus).FirstOrDefault(),
                qrToken = booking.BookingQRs.Where(q => q.Status == "Active").Select(q => q.Token).FirstOrDefault(),
                ticketIds = booking.Tickets.Select(t => t.TicketId).ToList(),
                barcodes = booking.Tickets.Select(t => t.Barcode).ToList()
            },
            message = "Lấy thông tin đơn đặt vé thành công."
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(int customerId, CancellationToken cancellationToken)
    {
        var userId = await GetOwnedUserIdAsync(customerId);
        if (userId == null) return NotFound(new { success = false, message = "Không tìm thấy khách hàng." });

        var bookings = await _context.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId)
            .Where(b => b.Payments.Any(p => p.PaymentStatus == "SUCCESS"))
            .Include(b => b.Payments).Include(b => b.BookingQRs).Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(s => s!.Showtime).ThenInclude(s => s!.Movie)
            .OrderByDescending(b => b.CreatedDate)
            .Select(b => new
            {
                bookingId = b.BookingId,
                createdDate = b.CreatedDate,
                status = b.Status,
                totalAmount = b.TotalAmount,
                paymentStatus = b.Payments.Where(p => p.PaymentStatus == "SUCCESS").Select(p => p.PaymentStatus).FirstOrDefault(),
                paymentDate = b.Payments.Where(p => p.PaymentStatus == "SUCCESS").Select(p => p.PaymentDate).FirstOrDefault(),
                qrToken = b.BookingQRs.Where(q => q.Status == "Active").Select(q => q.Token).FirstOrDefault(),
                tickets = b.Tickets.Select(t => new
                {
                    ticketId = t.TicketId,
                    price = t.Price,
                    status = t.Status,
                    barcode = t.Barcode,
                    seatId = t.ShowtimeSeat!.SeatId,
                    showtimeId = t.ShowtimeSeat.ShowtimeId,
                    movieId = t.ShowtimeSeat.Showtime!.MovieId,
                    movieTitle = t.ShowtimeSeat.Showtime.Movie!.Title
                }).ToList()
            }).ToListAsync(cancellationToken);

        return Ok(new { success = true, data = bookings, message = "Lấy lịch sử vé thành công." });
    }

    private async Task<int?> GetOwnedUserIdAsync(int customerId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        return await _context.Customers.AnyAsync(c => c.CustomerId == customerId && c.UserId == userId) ? userId : null;
    }
}
