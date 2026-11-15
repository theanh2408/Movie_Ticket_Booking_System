using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IPaymentService
{
    Task<List<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to);
    Task<PaymentDto> GetAsync(int id);
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

    public async Task<List<PaymentDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var list = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to));
        return list.Select(ToDto).ToList();
    }

    public async Task<PaymentDto> GetAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id, _current.ScopeCinemaId())
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
