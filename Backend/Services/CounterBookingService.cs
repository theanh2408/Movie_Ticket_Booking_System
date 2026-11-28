using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface ICounterBookingService
{
    Task<CounterBookingResultDto> CreateAsync(CounterBookingDto dto, CancellationToken ct = default);
}

/// <summary>
/// Staff bán vé trực tiếp: tạo đơn Confirmed + vé + thanh toán (tiền mặt) + hóa đơn + mã QR trong MỘT transaction.
/// Ghế phải đang Available; nếu 2 nhân viên cùng chọn 1 ghế thì chỉ 1 người thành công.
/// </summary>
public class CounterBookingService : ICounterBookingService
{
    private const int MaxSeatsPerOrder = 10;

    private readonly IShowtimeRepository _showtimes;
    private readonly IBookingRepository _bookings;
    private readonly IPromotionService _promotions;
    private readonly ICurrentUser _current;

    public CounterBookingService(IShowtimeRepository showtimes, IBookingRepository bookings,
                                 IPromotionService promotions, ICurrentUser current)
    {
        _showtimes = showtimes;
        _bookings = bookings;
        _promotions = promotions;
        _current = current;
    }

    public async Task<CounterBookingResultDto> CreateAsync(CounterBookingDto dto, CancellationToken ct = default)
    {
        var seatIds = dto.SeatIds.Distinct().ToList();
        if (seatIds.Count == 0) throw new BadRequestException("Vui lòng chọn ít nhất 1 ghế");
        if (seatIds.Count > MaxSeatsPerOrder) throw new BadRequestException($"Mỗi đơn tối đa {MaxSeatsPerOrder} ghế");

        var st = await _showtimes.GetByIdAsync(dto.ShowtimeId, ct: ct) ?? throw new NotFoundException("Không tìm thấy suất chiếu");
        _current.EnsureCinemaAccess(st.Room?.CinemaID);   // nhân viên chỉ bán vé của rạp mình

        if (st.Status != ShowtimeStatus.Active) throw new ConflictException("Suất chiếu hiện không mở bán");
        if (st.Room?.Cinema?.Status != CinemaStatus.Opened) throw new ConflictException("Rạp đang đóng cửa nên không thể bán vé");
        if (!st.Date.HasValue || !st.StartHour.HasValue || st.Date.Value.ToDateTime(st.StartHour.Value) <= AppClock.LocalNow)
            throw new ConflictException("Suất chiếu đã bắt đầu nên không thể bán thêm vé");

        if (dto.CustomerId.HasValue && !await _bookings.CustomerExistsAsync(dto.CustomerId.Value, ct: ct))
            throw new NotFoundException("Khách hàng không tồn tại");

        var now = AppClock.UtcNow;
        var qrToken = NewToken();
        Booking booking = null!;

        await _showtimes.ExecuteInTransactionAsync(async () =>
        {
            var seats = await _showtimes.GetShowtimeSeatsByIdsAsync(st.ShowtimeID, seatIds, ct: ct);
            if (seats.Count != seatIds.Count)
                throw new NotFoundException("Có ghế không thuộc suất chiếu này");

            var unavailable = seats.Where(x => x.Status != ShowtimeSeatStatus.Available).ToList();
            if (unavailable.Count > 0)
                throw new ConflictException("Ghế không còn trống: " + string.Join(", ", unavailable.Select(SeatCode)));

            var subTotal = seats.Sum(x => x.Price ?? st.BasePrice ?? 0m);
            var discount = 0m;
            int? promotionId = null;
            if (!string.IsNullOrWhiteSpace(dto.PromotionCode))
            {
                var applied = await _promotions.ApplyAsync(new ApplyPromotionDto
                {
                    Code = dto.PromotionCode,
                    OrderAmount = subTotal,
                    CustomerId = dto.CustomerId
                }, ct: ct);
                discount = applied.DiscountAmount;
                promotionId = applied.PromotionId;
            }
            var total = subTotal - discount;

            booking = new Booking
            {
                CustomerID = dto.CustomerId,
                ShowtimeID = st.ShowtimeID,
                CreatedDate = now,
                SubTotal = subTotal,
                DiscountAmount = discount,
                TotalAmount = total,
                Status = BookingStatus.Confirmed
            };

            foreach (var seat in seats)
            {
                seat.Status = ShowtimeSeatStatus.Booked;
                booking.Tickets.Add(new Ticket
                {
                    Price = seat.Price ?? st.BasePrice,
                    Status = TicketStatus.Unused,
                    ShowtimeSeatID = seat.ShowtimeSeatID,
                    Barcode = NewToken()
                });
            }

            booking.Payments.Add(new Payment
            {
                TransactionNo = $"POS-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
                Amount = total,
                PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? PaymentMethods.Cash : dto.PaymentMethod.Trim(),
                PaymentStatus = PaymentStatus.Success,
                PaymentDate = now
            });

            var invoice = new Invoice { CreateDate = now, Status = InvoiceStatus.Issued, TotalPrice = total };
            foreach (var ticket in booking.Tickets)
                invoice.Details.Add(new InvoiceDetail { Ticket = ticket });
            booking.Invoice = invoice;

            booking.QRs.Add(new BookingQR { Token = qrToken, Status = QrStatus.Active });

            if (promotionId.HasValue)
                booking.BookingPromotions.Add(new BookingPromotion
                {
                    PromotionID = promotionId.Value,
                    DiscountAmount = discount,
                    AppliedAt = now
                });

            await _bookings.AddAsync(booking, ct: ct);
        }, ct: ct);

        var saved = await _bookings.GetDetailAsync(booking.BookingID, null, ct: ct);
        return new CounterBookingResultDto
        {
            Booking = BookingService.ToDetail(saved!),
            InvoiceId = saved!.Invoice?.InvoiceID ?? 0,
            QrToken = qrToken
        };
    }

    private static string SeatCode(ShowtimeSeat s) => (s.Seat?.Row ?? "") + (s.Seat?.Column ?? "");

    private static string NewToken() =>
        Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
}
