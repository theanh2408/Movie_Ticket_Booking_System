using System.Data;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services;

// The current database has no separate PaymentAttempt tables. A pending Booking and
// HELD Ticket rows are used as the temporary payment/seat reservation.
public class PaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;

    public PaymentService(ApplicationDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<PaymentAttemptResponse> CreateAttemptAsync(int userId, CreatePaymentAttemptRequest request, Guid idempotencyKey)
    {
        if (request.SeatIds.Count is < 1 or > 8 || request.SeatIds.Any(id => id <= 0) || request.SeatIds.Distinct().Count() != request.SeatIds.Count)
            throw new ArgumentException("Danh sách ghế không hợp lệ hoặc bị trùng.");

        var cacheKey = $"idempotency:{userId}:{idempotencyKey}";
        if (_cache.TryGetValue<int>(cacheKey, out var existingId))
        {
            return await GetResponseAsync(existingId);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = DateTime.UtcNow;
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId)
                ?? throw new InvalidOperationException("Tài khoản chưa có hồ sơ khách hàng.");

            var expired = await _context.Bookings.Include(b => b.Tickets)
                .Where(b => b.Status == "PENDING" && b.ExpiredAt <= now).ToListAsync();
            foreach (var expiredBooking in expired)
            {
                expiredBooking.Status = "EXPIRED";
                _context.Tickets.RemoveRange(expiredBooking.Tickets);
            }

            var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.ShowtimeId == request.ShowtimeId);
            if (showtime == null) throw new KeyNotFoundException("Không tìm thấy suất chiếu.");

            // Hỗ trợ cả trạng thái ACTIVE và OPEN để đồng bộ tuyệt đối với DB
            var activeStatuses = new[] { "ACTIVE", "OPEN" };
            if (!activeStatuses.Contains(showtime.Status?.ToUpperInvariant()))
                throw new InvalidOperationException("Suất chiếu không còn hoạt động.");

            var seats = await _context.ShowtimeSeats.Where(s => s.ShowtimeId == request.ShowtimeId && s.SeatId.HasValue && request.SeatIds.Contains(s.SeatId.Value)).ToListAsync();
            if (seats.Count != request.SeatIds.Count) throw new ArgumentException("Một hoặc nhiều ghế không thuộc suất chiếu này.");
            if (seats.Any(s => s.Price is null or <= 0)) throw new InvalidOperationException("Giá ghế không hợp lệ.");
            if (seats.Any(s => s.Status != "AVAILABLE")) throw new InvalidOperationException("Một hoặc nhiều ghế không còn trống.");

            var showtimeSeatIds = seats.Select(s => s.ShowtimeSeatId).ToList();
            var occupied = await _context.Tickets.Include(t => t.Booking)
                .Where(t => t.ShowtimeSeatId.HasValue && showtimeSeatIds.Contains(t.ShowtimeSeatId.Value))
                .Where(t => t.Status == "VALID" || t.Status == "PRINTED" || (t.Status == "HELD" && t.Booking!.Status == "PENDING" && t.Booking.ExpiredAt > now))
                .AnyAsync();

            if (occupied) throw new InvalidOperationException("Một hoặc nhiều ghế đã được đặt hoặc đang được giữ.");

            var amount = seats.Sum(s => s.Price!.Value);
            var booking = new Booking
            {
                CustomerId = customer.CustomerId,
                CreatedDate = now,
                ExpiredAt = now.AddMinutes(10),
                SubTotal = amount,
                TotalAmount = amount,
                DiscountAmount = 0,
                Status = "PENDING"
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            _context.Tickets.AddRange(seats.Select(s => new Ticket
            {
                BookingId = booking.BookingId,
                ShowtimeSeatId = s.ShowtimeSeatId,
                Price = s.Price,
                Status = "HELD"
            }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Lưu Idempotency Key vào cache với thời hạn 15 phút
            _cache.Set(cacheKey, booking.BookingId, TimeSpan.FromMinutes(15));

            return ToResponse(booking, request.ShowtimeId, request.SeatIds);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number is 1205 or 2601 or 2627)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Một hoặc nhiều ghế bạn chọn đang được người khác giữ chỗ hoặc đặt. Vui lòng chọn lại ghế.");
        }
    }

    public async Task<PaymentAttemptResponse> CompleteAttemptAsync(int userId, int paymentAttemptId, string result)
    {
        result = result?.Trim().ToUpperInvariant() ?? "";
        if (result is not ("SUCCESS" or "FAILED")) throw new ArgumentException("Kết quả thanh toán phải là SUCCESS hoặc FAILED.");

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var booking = await _context.Bookings.Include(b => b.Customer).Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).ThenInclude(s => s!.Seat).FirstOrDefaultAsync(b => b.BookingId == paymentAttemptId);
        if (booking == null) throw new KeyNotFoundException("Không tìm thấy lượt thanh toán.");
        if (booking.Customer?.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thanh toán này.");
        if (booking.Status != "PENDING") throw new InvalidOperationException("Lượt thanh toán không còn ở trạng thái PENDING.");

        var now = DateTime.UtcNow;
        var seatIds = booking.Tickets.Where(t => t.ShowtimeSeat?.SeatId != null).Select(t => t.ShowtimeSeat!.SeatId!.Value).ToList();
        var showtimeId = booking.Tickets.FirstOrDefault()?.ShowtimeSeat?.ShowtimeId ?? 0;

        if (booking.ExpiredAt <= now || result == "FAILED")
        {
            booking.Status = booking.ExpiredAt <= now ? "EXPIRED" : "FAILED";

            // Lưu lại Payment log trạng thái FAILED phục vụ đối soát
            _context.Payments.Add(new Payment
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalAmount,
                PaymentMethod = "MOCK",
                PaymentStatus = "FAILED",
                PaymentDate = now,
                TransactionNo = $"MOCK-FAIL-{Guid.NewGuid():N}"
            });

            _context.Tickets.RemoveRange(booking.Tickets);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return ToResponse(booking, showtimeId, seatIds);
        }

        var invoice = new Invoice { BookingId = booking.BookingId, CreateDate = now, Status = "PAID", TotalPrice = booking.TotalAmount };
        booking.Status = "PAID";
        _context.Invoices.Add(invoice);
        _context.Payments.Add(new Payment
        {
            BookingId = booking.BookingId,
            Amount = booking.TotalAmount,
            PaymentMethod = "MOCK",
            PaymentStatus = "SUCCESS",
            PaymentDate = now,
            TransactionNo = $"MOCK-{Guid.NewGuid():N}"
        });

        foreach (var ticket in booking.Tickets)
        {
            ticket.Status = "VALID";
            ticket.Barcode = $"TKT-{Guid.NewGuid():N}";
        }

        var qrToken = $"BOOKING-{Guid.NewGuid():N}";
        _context.BookingQRs.Add(new BookingQR
        {
            BookingId = booking.BookingId,
            Token = qrToken,
            Status = "Active"
        });

        await _context.SaveChangesAsync();
        _context.InvoiceDetails.AddRange(booking.Tickets.Select(t => new InvoiceDetail { InvoiceId = invoice.InvoiceId, TicketId = t.TicketId }));
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = ToResponse(booking, showtimeId, seatIds);
        response.QrToken = qrToken;
        response.Barcodes = booking.Tickets.Where(t => !string.IsNullOrWhiteSpace(t.Barcode)).Select(t => t.Barcode!).ToList();
        return response;
    }

    private async Task<PaymentAttemptResponse> GetResponseAsync(int bookingId)
    {
        var booking = await _context.Bookings.Include(b => b.BookingQRs).Include(b => b.Tickets).ThenInclude(t => t.ShowtimeSeat).FirstOrDefaultAsync(b => b.BookingId == bookingId) ?? throw new KeyNotFoundException("Không tìm thấy lượt thanh toán.");
        var response = ToResponse(booking, booking.Tickets.FirstOrDefault()?.ShowtimeSeat?.ShowtimeId ?? 0, booking.Tickets.Where(t => t.ShowtimeSeat?.SeatId != null).Select(t => t.ShowtimeSeat!.SeatId!.Value).ToList());
        response.QrToken = booking.BookingQRs.Where(q => q.Status == "Active").Select(q => q.Token).FirstOrDefault();
        response.Barcodes = booking.Tickets.Where(t => !string.IsNullOrWhiteSpace(t.Barcode)).Select(t => t.Barcode!).ToList();
        return response;
    }

    private static PaymentAttemptResponse ToResponse(Booking booking, int showtimeId, List<int> seatIds) => new()
    {
        PaymentAttemptId = booking.BookingId,
        ShowtimeId = showtimeId,
        SeatIds = seatIds,
        Amount = booking.TotalAmount ?? 0,
        Status = booking.Status ?? "PENDING",
        ExpiresAt = booking.ExpiredAt ?? DateTime.UtcNow,
        RemainingSeconds = booking.ExpiredAt.HasValue
            ? Math.Max(0, (int)Math.Ceiling((booking.ExpiredAt.Value - DateTime.UtcNow).TotalSeconds))
            : 0
    };
}
