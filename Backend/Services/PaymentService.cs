
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Backend.Services;

public class PaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentAttemptResponse> CreateAttemptAsync(
        int userId,
        CreatePaymentAttemptRequest request,
        Guid idempotencyKey)
    {
        if (request.SeatIds == null ||
            request.SeatIds.Count == 0 ||
            request.SeatIds.Count > 8)
        {
            throw new ArgumentException(
                "Số lượng ghế phải từ 1 đến 8.");
        }

        if (request.SeatIds.Any(id => id <= 0) ||
            request.SeatIds.Distinct().Count() != request.SeatIds.Count)
        {
            throw new ArgumentException(
                "Danh sách ghế không hợp lệ hoặc bị trùng.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            // Kiểm tra Idempotency
            var existing = await _context.PaymentAttempts
                 .Include(a => a.PaymentAttemptSeats)
                     .ThenInclude(s => s.ShowtimeSeat)
                 .FirstOrDefaultAsync(a =>
                     a.UserId == userId &&
                     a.IdempotencyKey == idempotencyKey);

            if (existing != null)
            {
                var existingSeatIds = existing.PaymentAttemptSeats
                    .Where(s => s.ShowtimeSeat?.SeatId != null)
                    .Select(s => s.ShowtimeSeat!.SeatId!.Value)
                    .OrderBy(id => id)
                    .ToList();

                var requestedSeatIds = request.SeatIds
                    .OrderBy(id => id)
                    .ToList();

                if (existing.ShowtimeId != request.ShowtimeId ||
                    !existingSeatIds.SequenceEqual(requestedSeatIds))
                {
                    throw new InvalidOperationException(
                        "Idempotency Key đã được sử dụng cho yêu cầu khác.");
                }

                await transaction.CommitAsync();

                return new PaymentAttemptResponse
                {
                    PaymentAttemptId = existing.PaymentAttemptId,
                    ShowtimeId = existing.ShowtimeId,
                    SeatIds = existingSeatIds,
                    Amount = existing.Amount,
                    Status = existing.Status,
                    ExpiresAt = existing.ExpiresAt
                };
            }

            var now = DateTime.UtcNow;

            // Giải phóng các lượt giữ đã hết hạn
            var expiredAttempts = await _context.PaymentAttempts
                .Where(a =>
                    a.Status == "PENDING" &&
                    a.ExpiresAt <= now)
                .ToListAsync();

            foreach (var expiredAttempt in expiredAttempts)
            {
                expiredAttempt.Status = "EXPIRED";
            }

            var expiredIds = expiredAttempts
                .Select(a => a.PaymentAttemptId)
                .ToList();

            if (expiredIds.Count > 0)
            {
                var expiredSeats = await _context.PaymentAttemptSeats
                    .Where(s =>
                        expiredIds.Contains(s.PaymentAttemptId) &&
                        s.IsActive)
                    .ToListAsync();

                foreach (var seat in expiredSeats)
                {
                    seat.IsActive = false;
                }
            }

            // Kiểm tra suất chiếu
            var showtime = await _context.Showtimes
                .FirstOrDefaultAsync(s =>
                    s.ShowtimeId == request.ShowtimeId);

            if (showtime == null)
                throw new KeyNotFoundException(
                    "Không tìm thấy suất chiếu.");

            if (showtime.Status != "ACTIVE")
                throw new InvalidOperationException(
                    "Suất chiếu không còn hoạt động.");

            // Lấy ghế thuộc đúng suất chiếu
            var showtimeSeats = await _context.ShowtimeSeats
                .Where(s =>
                    s.ShowtimeId == request.ShowtimeId &&
                    s.SeatId.HasValue &&
                    request.SeatIds.Contains(s.SeatId.Value))
                .ToListAsync();

            if (showtimeSeats.Count != request.SeatIds.Count)
                throw new ArgumentException(
                    "Một hoặc nhiều ghế không thuộc suất chiếu này.");

            if (showtimeSeats.Any(s => !s.Price.HasValue || s.Price.Value <= 0))
            {
                throw new InvalidOperationException(
                    "Giá ghế không hợp lệ.");
            }

            // Kiểm tra trạng thái ghế
            if (showtimeSeats.Any(s => s.Status != "AVAILABLE"))
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế không còn trống.");

            // Kiểm tra vé đã tồn tại
            var showtimeSeatIds = showtimeSeats
                .Select(s => s.ShowtimeSeatId)
                .ToList();

            var bookedSeatIds = await _context.Tickets
                .Where(t =>
                    t.ShowtimeSeatId.HasValue &&
                    showtimeSeatIds.Contains(t.ShowtimeSeatId.Value))
                .Select(t => t.ShowtimeSeatId!.Value)
                .ToListAsync();

            if (bookedSeatIds.Count > 0)
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế đã được đặt.");

            // Kiểm tra ghế đang được lượt thanh toán khác giữ
            var heldSeatIds = await _context.PaymentAttemptSeats
                .Where(s =>
                    showtimeSeatIds.Contains(s.ShowtimeSeatId) &&
                    s.IsActive &&
                    s.PaymentAttempt!.Status == "PENDING" &&
                    s.PaymentAttempt.ExpiresAt > now)
                .Select(s => s.ShowtimeSeatId)
                .ToListAsync();

            if (heldSeatIds.Count > 0)
            {
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế đang được khách hàng khác giữ.");
            }

            // Tạo lượt giữ ghế
            var attempt = new PaymentAttempt
            {
                UserId = userId,
                ShowtimeId = request.ShowtimeId,
                Amount = showtimeSeats.Sum(s => s.Price!.Value),
                Status = "PENDING",
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10),
                IdempotencyKey = idempotencyKey
            };

            _context.PaymentAttempts.Add(attempt);

            foreach (var seat in showtimeSeats)
            {
                _context.PaymentAttemptSeats.Add(
                    new PaymentAttemptSeat
                    {
                        PaymentAttempt = attempt,
                        ShowtimeSeatId = seat.ShowtimeSeatId,
                        Price = seat.Price!.Value,
                        IsActive = true
                    });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PaymentAttemptResponse
            {
                PaymentAttemptId = attempt.PaymentAttemptId,
                ShowtimeId = attempt.ShowtimeId,
                SeatIds = request.SeatIds,
                Amount = attempt.Amount,
                Status = attempt.Status,
                ExpiresAt = attempt.ExpiresAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PaymentAttemptResponse> CompleteAttemptAsync(
    int userId,
    int paymentAttemptId,
    string result)
    {
        result = result?.Trim().ToUpperInvariant() ?? "";

        if (result != "SUCCESS" && result != "FAILED")
        {
            throw new ArgumentException(
                "Kết quả thanh toán phải là SUCCESS hoặc FAILED.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var attempt = await _context.PaymentAttempts
                .Include(a => a.PaymentAttemptSeats)
                    .ThenInclude(s => s.ShowtimeSeat)
                .FirstOrDefaultAsync(a =>
                    a.PaymentAttemptId == paymentAttemptId);

            if (attempt == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy lượt thanh toán.");
            }

            if (attempt.UserId != userId)
            {
                throw new UnauthorizedAccessException(
                    "Bạn không có quyền thực hiện thanh toán này.");
            }

            // Chống hoàn tất thanh toán nhiều lần
            if (attempt.Status != "PENDING")
            {
                throw new InvalidOperationException(
                    "Lượt thanh toán không còn ở trạng thái PENDING.");
            }

            var now = DateTime.UtcNow;

            // Kiểm tra thời hạn giữ ghế
            if (attempt.ExpiresAt <= now)
            {
                attempt.Status = "EXPIRED";

                foreach (var seat in attempt.PaymentAttemptSeats)
                {
                    seat.IsActive = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PaymentAttemptResponse
                {
                    PaymentAttemptId = attempt.PaymentAttemptId,
                    ShowtimeId = attempt.ShowtimeId,
                    SeatIds = attempt.PaymentAttemptSeats
                        .Where(s => s.ShowtimeSeat?.SeatId != null)
                        .Select(s => s.ShowtimeSeat!.SeatId!.Value)
                        .ToList(),
                    Amount = attempt.Amount,
                    Status = attempt.Status,
                    ExpiresAt = attempt.ExpiresAt
                };
            }

            // Thanh toán thất bại
            if (result == "FAILED")
            {
                attempt.Status = "FAILED";
                attempt.CompletedAt = now;

                foreach (var seat in attempt.PaymentAttemptSeats)
                {
                    seat.IsActive = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PaymentAttemptResponse
                {
                    PaymentAttemptId = attempt.PaymentAttemptId,
                    ShowtimeId = attempt.ShowtimeId,
                    SeatIds = attempt.PaymentAttemptSeats
                        .Where(s => s.ShowtimeSeat?.SeatId != null)
                        .Select(s => s.ShowtimeSeat!.SeatId!.Value)
                        .ToList(),
                    Amount = attempt.Amount,
                    Status = attempt.Status,
                    ExpiresAt = attempt.ExpiresAt
                };
            }

            // Kiểm tra danh sách ghế
            var attemptSeats = attempt.PaymentAttemptSeats.ToList();

            if (attemptSeats.Count == 0 ||
                attemptSeats.Any(s => s.ShowtimeSeat == null))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin ghế của lượt thanh toán.");
            }

            var showtimeSeatIds = attemptSeats
                .Select(s => s.ShowtimeSeatId)
                .ToList();

            // Kiểm tra vé đã tồn tại
            var existingTickets = await _context.Tickets
                .AnyAsync(t =>
                    t.ShowtimeSeatId.HasValue &&
                    showtimeSeatIds.Contains(t.ShowtimeSeatId.Value));

            if (existingTickets)
            {
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế đã có vé.");
            }

            // Tạo hóa đơn
            var invoice = new Invoice
            {
                UserId = userId,
                CreateDate = now,
                Status = "PAID"
            };

            _context.Invoices.Add(invoice);

            await _context.SaveChangesAsync();

            // Tạo payment
            var payment = new Payment
            {
                TransactionNo = $"MOCK-{Guid.NewGuid():N}",
                Amount = attempt.Amount,
                PaymentMethod = "MOCK",
                PaymentStatus = "SUCCESS",
                PaymentDate = now,
                InvoiceId = invoice.InvoiceId
            };

            _context.Payments.Add(payment);

            // Tạo vé
            foreach (var seat in attemptSeats)
            {
                _context.Tickets.Add(new Ticket
                {
                    Price = seat.Price,
                    Status = "VALID",
                    InvoiceId = invoice.InvoiceId,
                    ShowtimeSeatId = seat.ShowtimeSeatId
                });

                seat.IsActive = false;
            }

            // Cập nhật lượt thanh toán
            attempt.Status = "SUCCESS";
            attempt.CompletedAt = now;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new PaymentAttemptResponse
            {
                PaymentAttemptId = attempt.PaymentAttemptId,
                ShowtimeId = attempt.ShowtimeId,
                SeatIds = attemptSeats
                    .Select(s => s.ShowtimeSeat!.SeatId!.Value)
                    .ToList(),
                Amount = attempt.Amount,
                Status = attempt.Status,
                ExpiresAt = attempt.ExpiresAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
