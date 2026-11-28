using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IPromotionService
{
    Task<PagedResult<PromotionResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default);
    Task<PromotionResponseDto> GetAsync(int id, CancellationToken ct = default);
    Task<PromotionResponseDto> CreateAsync(PromotionUpsertDto dto, CancellationToken ct = default);
    Task<PromotionResponseDto> UpdateAsync(int id, PromotionUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<PromotionResponseDto> UpdateStatusAsync(int id, string status, CancellationToken ct = default);
    Task<ApplyPromotionResultDto> ApplyAsync(ApplyPromotionDto dto, CancellationToken ct = default);
}

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _repo;

    public PromotionService(IPromotionRepository repo) => _repo = repo;

    public async Task<PagedResult<PromotionResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default)
    {
        var (items, total) = await _repo.SearchAsync(keyword, status, paging, ct: ct);
        return PagedResult<PromotionResponseDto>.Create(items.Select(ToDto).ToList(), total, paging);
    }

    public async Task<PromotionResponseDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi"));

    public async Task<PromotionResponseDto> CreateAsync(PromotionUpsertDto dto, CancellationToken ct = default)
    {
        var type = ValidateAndNormalize(dto);
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repo.CodeExistsAsync(code, ct: ct))
            throw new ConflictException($"Mã khuyến mãi \"{code}\" đã tồn tại");

        var status = NormalizeStatus(dto.Status) ?? PromotionStatus.Active;
        EnsureNotExpiredIfActive(status, dto.EndDate);

        var p = new Promotion { Code = code };
        Apply(p, dto, type);
        p.Status = status;

        await _repo.AddAsync(p, ct: ct);
        return ToDto(p);
    }

    public async Task<PromotionResponseDto> UpdateAsync(int id, PromotionUpsertDto dto, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        var type = ValidateAndNormalize(dto);
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repo.CodeExistsAsync(code, id, ct: ct))
            throw new ConflictException($"Mã khuyến mãi \"{code}\" đã tồn tại");

        // BUG-10: đã có đơn dùng mã này thì không được đổi các trường quyết định số tiền giảm / điều kiện áp dụng,
        // nếu không các đơn cũ sẽ "đổi giá" ngầm. Tên, mô tả, thời hạn, giới hạn lượt dùng và trạng thái vẫn sửa được.
        if (await _repo.IsUsedAsync(id, ct: ct))
        {
            var coreChanged = !string.Equals(code, p.Code, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(type, p.DiscountType, StringComparison.OrdinalIgnoreCase)
                || dto.DiscountValue != p.DiscountValue
                || dto.MinOrderAmount != p.MinOrderAmount
                || dto.MaxDiscountAmount != p.MaxDiscountAmount
                || dto.UsagePerCustomer != p.UsagePerCustomer;
            if (coreChanged)
                throw new ConflictException(
                    "Chương trình đã có lượt sử dụng nên không được sửa mã, loại/mức giảm, đơn tối thiểu, giảm tối đa " +
                    "và giới hạn mỗi khách. Hãy tạo chương trình mới (vẫn sửa được tên, mô tả, thời hạn, giới hạn lượt dùng, trạng thái).");
        }

        var status = NormalizeStatus(dto.Status) ?? p.Status;
        EnsureNotExpiredIfActive(status, dto.EndDate);

        p.Code = code;
        Apply(p, dto, type);
        p.Status = status;

        await _repo.SaveAsync(ct: ct);
        return ToDto(p);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        if (await _repo.IsUsedAsync(id, ct: ct))
            throw new ConflictException("Chương trình đã được áp dụng cho đơn đặt vé nên không thể xóa. Hãy chuyển sang Inactive.");
        await _repo.RemoveAsync(p, ct: ct);
    }

    public async Task<PromotionResponseDto> UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        var p = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        var newStatus = NormalizeStatus(status) ?? throw new BadRequestException("Trạng thái chỉ nhận Active hoặc Inactive");

        if (newStatus == PromotionStatus.Active && p.EndDate.HasValue && p.EndDate < AppClock.UtcNow)
            throw new ConflictException("Chương trình đã hết hạn nên không thể bật Active");

        p.Status = newStatus;
        await _repo.SaveAsync(ct: ct);
        return ToDto(p);
    }

    /// <summary>Kiểm tra mã và tính số tiền giảm (frontend gọi ở bước xác nhận đơn đặt vé).</summary>
    public async Task<ApplyPromotionResultDto> ApplyAsync(ApplyPromotionDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var p = await _repo.GetByCodeAsync(code, ct: ct) ?? throw new NotFoundException("Mã khuyến mãi không hợp lệ");

        if (p.Status != PromotionStatus.Active)
            throw new ConflictException("Chương trình khuyến mãi hiện không hoạt động");

        var now = AppClock.UtcNow;
        if (p.StartDate > now) throw new ConflictException("Chương trình khuyến mãi chưa bắt đầu");
        if (p.EndDate < now) throw new ConflictException("Mã khuyến mãi đã hết hạn");

        if (p.MinOrderAmount.HasValue && dto.OrderAmount < p.MinOrderAmount)
            throw new ConflictException($"Đơn hàng tối thiểu {p.MinOrderAmount:N0} để dùng mã này");

        if (p.UsageLimit.HasValue && await _repo.CountUsageAsync(p.PromotionID, ct: ct) >= p.UsageLimit)
            throw new ConflictException("Mã khuyến mãi đã hết lượt sử dụng");

        // Giới hạn số lần dùng cho mỗi khách hàng
        if (p.UsagePerCustomer.HasValue)
        {
            if (dto.CustomerId == null)
                throw new BadRequestException("Mã này giới hạn số lần dùng cho mỗi khách hàng, vui lòng truyền customerId");
            if (await _repo.CountCustomerUsageAsync(p.PromotionID, dto.CustomerId.Value, ct: ct) >= p.UsagePerCustomer)
                throw new ConflictException("Khách hàng đã dùng hết số lần được phép của mã khuyến mãi này");
        }

        // Quy tắc không kết hợp nhiều ưu đãi: mỗi đơn đặt vé chỉ áp dụng 1 khuyến mãi
        if (dto.BookingId.HasValue && await _repo.BookingHasPromotionAsync(dto.BookingId.Value, ct: ct))
            throw new ConflictException("Đơn đặt vé đã áp dụng một khuyến mãi khác, không thể kết hợp nhiều ưu đãi");

        var value = p.DiscountValue ?? 0;
        var discount = p.DiscountType == DiscountTypes.Percent ? dto.OrderAmount * value / 100m : value;

        if (p.MaxDiscountAmount.HasValue && p.MaxDiscountAmount > 0 && discount > p.MaxDiscountAmount)
            discount = p.MaxDiscountAmount.Value;

        discount = Math.Round(Math.Min(discount, dto.OrderAmount), 2);

        return new ApplyPromotionResultDto
        {
            PromotionId = p.PromotionID,
            Code = p.Code,
            PromotionName = p.PromotionName,
            OrderAmount = dto.OrderAmount,
            DiscountAmount = discount,
            FinalAmount = dto.OrderAmount - discount
        };
    }

    // ---------- helpers ----------
    private static string ValidateAndNormalize(PromotionUpsertDto dto)
    {
        string type;
        if (string.Equals(dto.DiscountType, DiscountTypes.Percent, StringComparison.OrdinalIgnoreCase)) type = DiscountTypes.Percent;
        else if (string.Equals(dto.DiscountType, DiscountTypes.Fixed, StringComparison.OrdinalIgnoreCase)) type = DiscountTypes.Fixed;
        else throw new BadRequestException("Loại giảm giá chỉ nhận Percent hoặc Fixed");

        if (type == DiscountTypes.Percent && dto.DiscountValue > 100)
            throw new BadRequestException("Giảm theo phần trăm không được vượt quá 100");

        if (dto.EndDate <= dto.StartDate)
            throw new BadRequestException("Ngày kết thúc phải sau ngày bắt đầu");

        if (dto.Status != null && NormalizeStatus(dto.Status) == null && dto.Status.Trim().Length > 0)
            throw new BadRequestException("Trạng thái chỉ nhận Active hoặc Inactive");

        return type;
    }

    /// <summary>Chương trình Active mà ngày kết thúc đã qua là vô nghĩa (không ai dùng được) nên không cho lưu.</summary>
    private static void EnsureNotExpiredIfActive(string? status, DateTime? endDate)
    {
        if (status == PromotionStatus.Active && endDate.HasValue && endDate.Value < AppClock.UtcNow)
            throw new BadRequestException(
                "Không thể để chương trình ở trạng thái Active khi ngày kết thúc đã qua. Hãy chọn ngày kết thúc ở tương lai hoặc trạng thái Inactive.");
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (string.Equals(status.Trim(), PromotionStatus.Active, StringComparison.OrdinalIgnoreCase)) return PromotionStatus.Active;
        if (string.Equals(status.Trim(), PromotionStatus.Inactive, StringComparison.OrdinalIgnoreCase)) return PromotionStatus.Inactive;
        return null;
    }

    private static void Apply(Promotion p, PromotionUpsertDto dto, string type)
    {
        p.PromotionName = dto.PromotionName.Trim();
        p.Description = dto.Description;
        p.DiscountType = type;
        p.DiscountValue = dto.DiscountValue;
        p.MinOrderAmount = dto.MinOrderAmount;
        p.MaxDiscountAmount = dto.MaxDiscountAmount;
        p.StartDate = dto.StartDate;
        p.EndDate = dto.EndDate;
        p.UsageLimit = dto.UsageLimit;
        p.UsagePerCustomer = dto.UsagePerCustomer;
    }

    private static PromotionResponseDto ToDto(Promotion p) => new()
    {
        PromotionID = p.PromotionID,
        Code = p.Code,
        PromotionName = p.PromotionName,
        Description = p.Description,
        DiscountType = p.DiscountType,
        DiscountValue = p.DiscountValue,
        MinOrderAmount = p.MinOrderAmount,
        MaxDiscountAmount = p.MaxDiscountAmount,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        UsageLimit = p.UsageLimit,
        UsagePerCustomer = p.UsagePerCustomer,
        Status = p.Status
    };
}
