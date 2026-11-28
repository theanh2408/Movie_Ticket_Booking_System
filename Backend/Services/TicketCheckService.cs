using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface ITicketCheckService
{
    /// <summary>Chỉ kiểm tra, không đổi trạng thái vé.</summary>
    Task<TicketCheckResultDto> VerifyAsync(string token, CancellationToken ct = default);

    /// <summary>Kiểm tra rồi xác nhận sử dụng vé (Unused -> Used). Vé không hợp lệ thì trả 409 kèm lý do.</summary>
    Task<TicketCheckResultDto> UseAsync(string token, CancellationToken ct = default);
}

/// <summary>Token có thể là mã QR của đơn (BookingQR.Token) hoặc mã vạch của từng vé (Ticket.Barcode).</summary>
public class TicketCheckService : ITicketCheckService
{
    private const int EarlyEntryMinutes = 30;   // cho vào phòng sớm tối đa 30 phút trước giờ chiếu

    private readonly IBookingRepository _bookings;
    private readonly ICurrentUser _current;
    private readonly ILogger<TicketCheckService> _logger;

    public TicketCheckService(IBookingRepository bookings, ICurrentUser current, ILogger<TicketCheckService> logger)
    {
        _bookings = bookings;
        _current = current;
        _logger = logger;
    }

    public async Task<TicketCheckResultDto> VerifyAsync(string token, CancellationToken ct = default)
    {
        var (booking, tickets, reason) = await EvaluateAsync(token, ct: ct);
        return ToResult(booking, tickets, reason);
    }

    public async Task<TicketCheckResultDto> UseAsync(string token, CancellationToken ct = default)
    {
        var (booking, tickets, reason) = await EvaluateAsync(token, ct: ct);
        if (reason != null) throw new ConflictException(reason);

        var usable = tickets.Where(t => t.Status == TicketStatus.Unused).ToList();
        foreach (var t in usable) t.Status = TicketStatus.Used;
        await _bookings.SaveAsync(ct: ct);

        // Chưa có cột lưu "ai kiểm tra / lúc nào" nên ghi vào log
        _logger.LogInformation("Người dùng {UserId} đã xác nhận sử dụng {Count} vé của đơn {BookingId}",
            _current.UserId, usable.Count, booking!.BookingID);

        return ToResult(booking, tickets, null);
    }

    private async Task<(Booking? Booking, List<Ticket> Tickets, string? Reason)> EvaluateAsync(string token, CancellationToken ct = default)
    {
        token = token.Trim();
        Booking? booking = null;
        var tickets = new List<Ticket>();

        var qr = await _bookings.GetQrByTokenAsync(token, ct: ct);
        if (qr != null)
        {
            if (qr.Status != QrStatus.Active)
                return (null, tickets, "Mã QR đã bị thu hồi");
            booking = await _bookings.GetForCheckInAsync(qr.BookingID, ct: ct);
            if (booking != null) tickets = booking.Tickets.ToList();
        }
        else
        {
            var bookingId = await _bookings.GetBookingIdByBarcodeAsync(token, ct: ct);
            if (bookingId != null)
            {
                booking = await _bookings.GetForCheckInAsync(bookingId.Value, ct: ct);
                tickets = booking?.Tickets.Where(t => t.Barcode == token).ToList() ?? new List<Ticket>();
            }
        }

        if (booking == null || tickets.Count == 0)
            return (null, new List<Ticket>(), "Mã không hợp lệ hoặc không tìm thấy vé");

        var st = tickets[0].ShowtimeSeat?.Showtime;
        if (st == null) return (booking, tickets, "Không xác định được suất chiếu của vé");

        _current.EnsureCinemaAccess(st.Room?.CinemaID);   // vé của rạp khác -> 403

        if (booking.Status != BookingStatus.Confirmed)
            return (booking, tickets, "Đơn đặt vé chưa thanh toán thành công hoặc đã bị hủy");
        if (st.Status == ShowtimeStatus.Cancelled)
            return (booking, tickets, "Suất chiếu đã bị hủy");

        if (!tickets.Any(t => t.Status == TicketStatus.Unused))
        {
            var allUsed = tickets.All(t => t.Status == TicketStatus.Used);
            return (booking, tickets, allUsed ? "Vé đã được sử dụng" : "Vé đã hủy hoặc hết hiệu lực");
        }

        if (st.Date.HasValue && st.StartHour.HasValue)
        {
            var start = st.Date.Value.ToDateTime(st.StartHour.Value);
            var end = start.AddMinutes(st.Movie?.Duration ?? 0);
            var now = AppClock.LocalNow;
            if (now < start.AddMinutes(-EarlyEntryMinutes))
                return (booking, tickets, $"Chưa tới giờ vào phòng (chỉ cho vào sớm tối đa {EarlyEntryMinutes} phút trước giờ chiếu)");
            if (now > end)
                return (booking, tickets, "Suất chiếu đã kết thúc");
        }

        return (booking, tickets, null);
    }

    private static TicketCheckResultDto ToResult(Booking? booking, List<Ticket> tickets, string? reason)
    {
        var st = tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime;
        return new TicketCheckResultDto
        {
            IsValid = reason == null,
            Reason = reason,
            BookingID = booking?.BookingID,
            MovieTitle = st?.Movie?.Title,
            CinemaName = st?.Room?.Cinema?.CinemaName,
            RoomName = st?.Room?.RoomName,
            ShowDate = st?.Date,
            StartTime = st?.StartHour,
            Tickets = tickets.Select(t => new TicketDto
            {
                TicketID = t.TicketID,
                SeatCode = (t.ShowtimeSeat?.Seat?.Row ?? "") + (t.ShowtimeSeat?.Seat?.Column ?? ""),
                Price = t.Price,
                Status = t.Status
            }).ToList()
        };
    }
}
