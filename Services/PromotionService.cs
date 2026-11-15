using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IPromotionService
{
    Task<List<PromotionResponseDto>> SearchAsync(string? keyword, string? status);
    Task<PromotionResponseDto> GetAsync(int id);
    Task<PromotionResponseDto> CreateAsync(PromotionUpsertDto dto);
    Task<PromotionResponseDto> UpdateAsync(int id, PromotionUpsertDto dto);
    Task DeleteAsync(int id);
    Task<PromotionResponseDto> UpdateStatusAsync(int id, string status);
    Task<ApplyPromotionResultDto> ApplyAsync(ApplyPromotionDto dto);
}

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _repo;

    public PromotionService(IPromotionRepository repo) => _repo = repo;

    public async Task<List<PromotionResponseDto>> SearchAsync(string? keyword, string? status) =>
        (await _repo.SearchAsync(keyword, status)).Select(ToDto).ToList();

    public async Task<PromotionResponseDto> GetAsync(int id) =>
        ToDto(await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi"));

    public async Task<PromotionResponseDto> CreateAsync(PromotionUpsertDto dto)
    {
        var type = ValidateAndNormalize(dto);
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repo.CodeExistsAsync(code))
            throw new ConflictException($"Mã khuyến mãi \"{code}\" đã tồn tại");

        var p = new Promotion { Code = code };
        Apply(p, dto, type);
        p.Status = NormalizeStatus(dto.Status) ?? PromotionStatus.Active;

        await _repo.AddAsync(p);
        return ToDto(p);
    }

    public async Task<PromotionResponseDto> UpdateAsync(int id, PromotionUpsertDto dto)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        var type = ValidateAndNormalize(dto);
        var code = dto.Code.Trim().ToUpperInvariant();

        if (await _repo.CodeExistsAsync(code, id))
            throw new ConflictException($"Mã khuyến mãi \"{code}\" đã tồn tại");

        p.Code = code;
        Apply(p, dto, type);
        var status = NormalizeStatus(dto.Status);
        if (status != null) p.Status = status;

        await _repo.SaveAsync();
        return ToDto(p);
    }

    public async Task DeleteAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        if (await _repo.IsUsedAsync(id))
            throw new ConflictException("Chương trình đã được áp dụng cho đơn đặt vé nên không thể xóa. Hãy chuyển sang Inactive.");
        await _repo.RemoveAsync(p);
    }

    public async Task<PromotionResponseDto> UpdateStatusAsync(int id, string status)
    {
        var p = await _repo.GetByIdAsync(id) ?? throw new NotFoundException("Không tìm thấy chương trình khuyến mãi");
        var newStatus = NormalizeStatus(status) ?? throw new BadRequestException("Trạng thái chỉ nhận Active hoặc Inactive");

        if (newStatus == PromotionStatus.Active && p.EndDate.HasValue && p.EndDate < DateTime.Now)
            throw new ConflictException("Chương trình đã hết hạn nên không thể bật Active");

        p.Status = newStatus;
        await _repo.SaveAsync();
        return ToDto(p);
    }

    /// <summary>Kiểm tra mã và tính số tiền giảm (frontend gọi ở bước xác nhận đơn đặt vé).</summary>
    public async Task<ApplyPromotionResultDto> ApplyAsync(ApplyPromotionDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var p = await _repo.GetByCodeAsync(code) ?? throw new NotFoundException("Mã khuyến mãi không hợp lệ");

        if (p.Status != PromotionStatus.Active)
            throw new ConflictException("Chương trình khuyến mãi hiện không hoạt động");

        var now = DateTime.Now;
        if (p.StartDate > now) throw new ConflictException("Chương trình khuyến mãi chưa bắt đầu");
        if (p.EndDate < now) throw new ConflictException("Mã khuyến mãi đã hết hạn");

        if (p.MinOrderAmount.HasValue && dto.OrderAmount < p.MinOrderAmount)
            throw new ConflictException($"Đơn hàng tối thiểu {p.MinOrderAmount:N0} để dùng mã này");

        if (p.UsageLimit.HasValue && await _repo.CountUsageAsync(p.PromotionID) >= p.UsageLimit)
            throw new ConflictException("Mã khuyến mãi đã hết lượt sử dụng");

        // TODO: kiểm tra UsagePerCustomer khi có thông tin khách hàng từ module đặt vé

        var value = p.DiscountValue ?? 0;
        var discount = p.DiscountType == DiscountTypes.Percent ? dto.OrderAmount * value / 100m : value;

        if (p.MaxDiscountAmount.HasValue && p.MaxDiscountAmount > 0 && discount > p.MaxDiscountAmount)
            discount = p.MaxDiscountAmount.Value;

        discount = Math.Round(Math.Min(discount, dto.OrderAmount), 2);

        return new ApplyPromotionResultDto
        {
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
