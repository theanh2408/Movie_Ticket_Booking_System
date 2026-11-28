using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default);
    Task<PaymentDto> GetAsync(int id, CancellationToken ct = default);
}

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repo;
    private readonly ICurrentUser _current;

    public PaymentService(IPaymentRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<PagedResult<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to, PageQuery paging, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var (items, total) = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to), paging, ct: ct);
        return PagedResult<PaymentDto>.Create(items.Select(ToDto).ToList(), total, paging);
    }

    public async Task<PaymentDto> GetAsync(int id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, _current.ScopeCinemaId(), ct: ct)
                ?? throw new NotFoundException("Không tìm thấy giao dịch");
        return ToDto(p);
    }

    public static PaymentDto ToDto(Payment p) => new()
    {
        PaymentID = p.PaymentID,
        TransactionNo = p.TransactionNo,
        Amount = p.Amount,
        PaymentMethod = p.PaymentMethod,
        PaymentStatus = p.PaymentStatus,
        PaymentDate = p.PaymentDate,
        BookingID = p.BookingID,
        InvoiceID = p.Booking?.Invoice?.InvoiceID
    };
}
