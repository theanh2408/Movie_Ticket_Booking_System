using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IInvoiceService
{
    Task<List<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to);
    Task<InvoiceDetailDto> GetAsync(int id);
    Task<InvoiceDetailDto> CancelAsync(int id);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _repo;
    private readonly IBookingRepository _bookings;
    private readonly ICurrentUser _current;

    public InvoiceService(IInvoiceRepository repo, IBookingRepository bookings, ICurrentUser current)
    {
        _repo = repo;
        _bookings = bookings;
        _current = current;
    }

    public async Task<List<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var list = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to));
        return list.Select(i => new InvoiceListItemDto
        {
            InvoiceID = i.InvoiceID,
            CreateDate = i.CreateDate,
            TotalPrice = i.TotalPrice,
            Status = i.Status,
            BookingID = i.BookingID,
            CustomerName = i.Booking?.Customer?.Account?.FullName
        }).ToList();
    }

    public async Task<InvoiceDetailDto> GetAsync(int id)
    {
        var inv = await _repo.GetByIdAsync(id, _current.ScopeCinemaId())
                  ?? throw new NotFoundException("Không tìm thấy hóa đơn");
        return await BuildDetailAsync(inv);
    }

    /// <summary>Hủy hóa đơn = đổi trạng thái thành Cancelled (không xóa khỏi DB). Chỉ khi đơn đặt vé đã bị khách hủy.</summary>
    public async Task<InvoiceDetailDto> CancelAsync(int id)
    {
        var inv = await _repo.GetByIdAsync(id, null) ?? throw new NotFoundException("Không tìm thấy hóa đơn");

        if (inv.Status == InvoiceStatus.Cancelled)
            throw new ConflictException("Hóa đơn này đã được hủy trước đó");
        if (inv.Booking?.Status != BookingStatus.Cancelled)
            throw new ConflictException("Chỉ được hủy hóa đơn khi khách hàng đã hủy vé (đơn đặt vé ở trạng thái Cancelled)");

        inv.Status = InvoiceStatus.Cancelled;
        await _repo.SaveAsync();
        return await BuildDetailAsync(inv);
    }

    private async Task<InvoiceDetailDto> BuildDetailAsync(Invoice inv)
    {
        var booking = inv.BookingID.HasValue ? await _bookings.GetDetailAsync(inv.BookingID.Value, null) : null;
        return new InvoiceDetailDto
        {
            InvoiceID = inv.InvoiceID,
            CreateDate = inv.CreateDate,
            TotalPrice = inv.TotalPrice,
            Status = inv.Status,
            Booking = booking == null ? null : BookingService.ToDetail(booking)
        };
    }
}
