using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IBookingService
{
    Task<PagedResult<BookingListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default);
    Task<BookingDetailDto> GetAsync(int id, CancellationToken ct = default);
}

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repo;
    private readonly ICurrentUser _current;

    public BookingService(IBookingRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<PagedResult<BookingListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var (items, total) = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to), paging, ct: ct);
        var dtos = items.Select(b => new BookingListItemDto
        {
            BookingID = b.BookingID,
            CustomerID = b.CustomerID,
            CustomerName = b.Customer?.Account?.FullName,
            CustomerPhone = b.Customer?.Account?.Phone,
            CreatedDate = b.CreatedDate,
            TotalAmount = b.TotalAmount,
            Status = b.Status
        }).ToList();
        return PagedResult<BookingListItemDto>.Create(dtos, total, paging);
    }

    public async Task<BookingDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var b = await _repo.GetDetailAsync(id, _current.ScopeCinemaId(), ct: ct)
                ?? throw new NotFoundException("Không tìm thấy đơn đặt vé");
        return ToDetail(b);
    }

    public static BookingDetailDto ToDetail(Booking b)
    {
        var st = b.Tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime ?? b.Showtime;   // đơn chưa có vé: lấy qua Booking.ShowtimeID
        return new BookingDetailDto
        {
            BookingID = b.BookingID,
            Status = b.Status,
            CreatedDate = b.CreatedDate,
            ExpiredAt = b.ExpiredAt,
            SubTotal = b.SubTotal,
            DiscountAmount = b.DiscountAmount,
            TotalAmount = b.TotalAmount,
            CustomerID = b.CustomerID,
            CustomerName = b.Customer?.Account?.FullName,
            CustomerPhone = b.Customer?.Account?.Phone,
            CustomerEmail = b.Customer?.Account?.Email,
            MovieTitle = st?.Movie?.Title,
            CinemaName = st?.Room?.Cinema?.CinemaName,
            RoomName = st?.Room?.RoomName,
            ShowDate = st?.Date,
            StartTime = st?.StartHour,
            Tickets = b.Tickets.Select(t => new TicketDto
            {
                TicketID = t.TicketID,
                SeatCode = (t.ShowtimeSeat?.Seat?.Row ?? "") + (t.ShowtimeSeat?.Seat?.Column ?? ""),
                Price = t.Price,
                Status = t.Status
            }).ToList(),
            Payments = b.Payments.Select(PaymentService.ToDto).ToList(),
            InvoiceID = b.Invoice?.InvoiceID
        };
    }
}
