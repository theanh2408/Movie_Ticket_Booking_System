using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default);
    Task<InvoiceDetailDto> GetAsync(int id, CancellationToken ct = default);
    Task<InvoiceDetailDto> CancelAsync(int id, CancellationToken ct = default);
    Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(int id, string format, CancellationToken ct = default);
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

    public async Task<PagedResult<InvoiceListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var (items, total) = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to), paging, ct: ct);
        var dtos = items.Select(i => new InvoiceListItemDto
        {
            InvoiceID = i.InvoiceID,
            CreateDate = i.CreateDate,
            TotalPrice = i.TotalPrice,
            Status = i.Status,
            BookingID = i.BookingID,
            CustomerName = i.Booking?.Customer?.Account?.FullName
        }).ToList();
        return PagedResult<InvoiceListItemDto>.Create(dtos, total, paging);
    }

    public async Task<InvoiceDetailDto> GetAsync(int id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(id, _current.ScopeCinemaId(), ct: ct)
                  ?? throw new NotFoundException("Không tìm thấy hóa đơn");
        return await BuildDetailAsync(inv, ct: ct);
    }

    /// <summary>Hủy hóa đơn = đổi trạng thái thành Cancelled (không xóa khỏi DB). Chỉ khi đơn đặt vé đã bị khách hủy.</summary>
    public async Task<InvoiceDetailDto> CancelAsync(int id, CancellationToken ct = default)
    {
        var inv = await _repo.GetByIdAsync(id, null, ct: ct) ?? throw new NotFoundException("Không tìm thấy hóa đơn");

        if (inv.Status == InvoiceStatus.Cancelled)
            throw new ConflictException("Hóa đơn này đã được hủy trước đó");
        if (inv.Booking?.Status != BookingStatus.Cancelled)
            throw new ConflictException("Chỉ được hủy hóa đơn khi khách hàng đã hủy vé (đơn đặt vé ở trạng thái Cancelled)");

        inv.Status = InvoiceStatus.Cancelled;
        await _repo.SaveAsync(ct: ct);
        return await BuildDetailAsync(inv, ct: ct);
    }

    /// <summary>Xuất hóa đơn ra PDF hoặc XML. Quyền xem theo rạp được kiểm tra qua GetAsync.</summary>
    public async Task<(byte[] Content, string ContentType, string FileName)> ExportAsync(int id, string format, CancellationToken ct = default)
    {
        var invoice = await GetAsync(id, ct: ct);
        switch ((format ?? "pdf").Trim().ToLowerInvariant())
        {
            case "pdf": return (InvoiceDocument.ToPdf(invoice), "application/pdf", $"invoice-{id}.pdf");
            case "xml": return (InvoiceDocument.ToXml(invoice), "application/xml", $"invoice-{id}.xml");
            default: throw new BadRequestException("format chỉ nhận pdf hoặc xml");
        }
    }

    private async Task<InvoiceDetailDto> BuildDetailAsync(Invoice inv, CancellationToken ct = default)
    {
        var booking = inv.BookingID.HasValue ? await _bookings.GetDetailAsync(inv.BookingID.Value, null, ct: ct) : null;
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
