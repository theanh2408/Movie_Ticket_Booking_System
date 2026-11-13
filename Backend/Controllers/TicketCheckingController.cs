using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

// Staff scans the QR at the counter. Barcode scanning at the entrance is
// intentionally not exposed yet, per the current customer-backend scope.
[ApiController]
[Route("api/ticket-checking")]
[Authorize(Roles = "ADMIN,MANAGER,STAFF")]
public class TicketCheckingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TicketCheckingController(ApplicationDbContext context) => _context = context;

    [HttpGet("qr/{token}")]
    public async Task<IActionResult> ScanQr(string token, CancellationToken cancellationToken)
    {
        var qr = await LoadQrAsync(token, cancellationToken);
        if (qr == null) return NotFound(new { success = false, message = "Không tìm thấy QR Token." });
        if (qr.Status != "Active") return Conflict(new { success = false, message = "QR Token đã bị vô hiệu hóa hoặc đã được in vé." });

        var validationError = ValidateBooking(qr.Booking);
        if (validationError != null) return Conflict(new { success = false, message = validationError });

        return Ok(new { success = true, data = ToScanResponse(qr), message = "Xác thực QR thành công." });
    }

    [HttpPost("qr/{token}/print")]
    public async Task<IActionResult> ConfirmPrint(string token, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var qr = await LoadQrAsync(token, cancellationToken);
        if (qr == null) return NotFound(new { success = false, message = "Không tìm thấy QR Token." });

        var validationError = ValidateBooking(qr.Booking);
        if (validationError != null) return Conflict(new { success = false, message = validationError });
        if (qr.Status != "Active" && !qr.Booking!.Tickets.All(t => t.Status == "PRINTED"))
            return Conflict(new { success = false, message = "QR Token không còn hiệu lực để in vé." });

        foreach (var ticket in qr.Booking!.Tickets)
        {
            if (ticket.Status != "PRINTED") ticket.Status = "PRINTED";
        }
        qr.Status = "Revoked";
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            success = true,
            data = new
            {
                bookingId = qr.Booking.BookingId,
                qrStatus = qr.Status,
                printStatus = "PRINTED",
                printedAt = DateTime.UtcNow,
                tickets = qr.Booking.Tickets.Select(ToTicketResponse).ToList()
            },
            message = "Đã xác nhận in vé thành công."
        });
    }

    private async Task<BookingQR?> LoadQrAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        return await _context.BookingQRs
            .Include(q => q.Booking!).ThenInclude(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat!).ThenInclude(s => s.Showtime!).ThenInclude(s => s.Movie)
            .Include(q => q.Booking!).ThenInclude(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat!).ThenInclude(s => s.Showtime!).ThenInclude(s => s.Room)
            .Include(q => q.Booking!).ThenInclude(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat!).ThenInclude(s => s.Seat)
            .Include(q => q.Booking!).ThenInclude(b => b.Payments)
            .Include(q => q.Booking!).ThenInclude(b => b.Customer!).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(q => q.Token == token.Trim(), cancellationToken);
    }

    private static string? ValidateBooking(Booking? booking)
    {
        if (booking == null) return "Booking không tồn tại.";
        if (booking.Status != "PAID") return "Booking chưa thanh toán thành công hoặc đã bị hủy.";
        if (!booking.Payments.Any(p => p.PaymentStatus == "SUCCESS")) return "Booking chưa có thanh toán thành công.";
        if (booking.Tickets.Count == 0) return "Booking không có ticket.";
        return null;
    }

    private static object ToScanResponse(BookingQR qr) => new
    {
        bookingId = qr.Booking!.BookingId,
        qrStatus = qr.Status,
        customerName = qr.Booking.Customer?.User?.FullName,
        phone = qr.Booking.Customer?.User?.Phone,
        customer = new
        {
            customerId = qr.Booking.CustomerId,
            fullName = qr.Booking.Customer?.User?.FullName,
            phone = qr.Booking.Customer?.User?.Phone,
            email = qr.Booking.Customer?.User?.Email
        },
        movieTitle = qr.Booking.Tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime?.Movie?.Title,
        showtime = qr.Booking.Tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime?.StartHour?.ToString(),
        room = qr.Booking.Tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime?.Room?.RoomName,
        seats = qr.Booking.Tickets.Select(t => $"{t.ShowtimeSeat?.Seat?.Row}{t.ShowtimeSeat?.Seat?.Column}").ToList(),
        totalAmount = qr.Booking.TotalAmount,
        printStatus = qr.Booking.Tickets.All(t => t.Status == "PRINTED") ? "PRINTED" : "NOT_PRINTED",
        tickets = qr.Booking.Tickets.Select(ToTicketResponse).ToList()
    };

    private static object ToTicketResponse(Ticket ticket) => new
    {
        ticketId = ticket.TicketId,
        barcode = ticket.Barcode,
        status = ticket.Status,
        price = ticket.Price,
        seatId = ticket.ShowtimeSeat?.SeatId,
        showtimeId = ticket.ShowtimeSeat?.ShowtimeId,
        movieId = ticket.ShowtimeSeat?.Showtime?.MovieId,
        movieTitle = ticket.ShowtimeSeat?.Showtime?.Movie?.Title
    };
}
