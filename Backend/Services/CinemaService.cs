using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface ICinemaService
{
    Task<List<CinemaDto>> GetPublicAsync(CancellationToken ct = default);
    Task<List<CinemaDto>> GetAllAsync(CancellationToken ct = default);
    Task<CinemaDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CinemaDto> CreateAsync(CinemaUpsertDto dto, CancellationToken ct = default);
    Task<CinemaDto> UpdateAsync(int id, CinemaUpsertDto dto, CancellationToken ct = default);
    Task<CinemaDto> UpdateStatusAsync(int id, string status, CancellationToken ct = default);
}

public class CinemaService : ICinemaService
{
    private readonly ICinemaRepository _repo;
    private readonly ICurrentUser _current;

    public CinemaService(ICinemaRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    /// <summary>Công khai cho Customer: chỉ các rạp đang mở cửa (Status = Opened), không giới hạn theo người dùng.</summary>
    public async Task<List<CinemaDto>> GetPublicAsync(CancellationToken ct = default) =>
        (await _repo.GetAllAsync(null, CinemaStatus.Opened, ct: ct)).Select(ToDto).ToList();

    /// <summary>Quản trị: Admin thấy tất cả rạp (mọi trạng thái); Quản lý/Nhân viên chỉ thấy rạp của mình.</summary>
    public async Task<List<CinemaDto>> GetAllAsync(CancellationToken ct = default)
    {
        int? filter = _current.IsAdmin ? null : (_current.CinemaId ?? -1);
        return (await _repo.GetAllAsync(filter, ct: ct)).Select(ToDto).ToList();
    }

    public async Task<CinemaDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _repo.GetByIdAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy rạp");
        _current.EnsureCinemaAccess(c.CinemaID);   // Quản lý / Nhân viên chỉ xem được rạp của mình
        return ToDto(c);
    }

    public async Task<CinemaDto> CreateAsync(CinemaUpsertDto dto, CancellationToken ct = default)
    {
        var name = dto.CinemaName.Trim();
        if (await _repo.NameExistsAsync(name, null, ct: ct))
            throw new ConflictException($"Đã có rạp tên \"{name}\"");

        var cinema = new Cinema
        {
            CinemaName = name,
            Address = dto.Address.Trim(),
            Status = NormalizeStatus(dto.Status) ?? CinemaStatus.Opened
        };
        await _repo.AddAsync(cinema, ct: ct);
        return ToDto(cinema);
    }

    public async Task<CinemaDto> UpdateAsync(int id, CinemaUpsertDto dto, CancellationToken ct = default)
    {
        var cinema = await _repo.GetForUpdateAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy rạp");
        var name = dto.CinemaName.Trim();
        if (await _repo.NameExistsAsync(name, id, ct: ct))
            throw new ConflictException($"Đã có rạp tên \"{name}\"");

        var newStatus = NormalizeStatus(dto.Status);
        if (newStatus != null) await EnsureCanChangeStatusAsync(cinema, newStatus, ct: ct);

        cinema.CinemaName = name;
        cinema.Address = dto.Address.Trim();
        if (newStatus != null) cinema.Status = newStatus;

        await _repo.SaveAsync(ct: ct);
        return ToDto(cinema);
    }

    public async Task<CinemaDto> UpdateStatusAsync(int id, string status, CancellationToken ct = default)
    {
        var cinema = await _repo.GetForUpdateAsync(id, ct: ct) ?? throw new NotFoundException("Không tìm thấy rạp");
        var newStatus = NormalizeStatus(status) ?? throw new BadRequestException("Trạng thái chỉ nhận Opened hoặc Closed");
        await EnsureCanChangeStatusAsync(cinema, newStatus, ct: ct);

        cinema.Status = newStatus;
        await _repo.SaveAsync(ct: ct);
        return ToDto(cinema);
    }

    /// <summary>Không cho đóng cửa rạp khi còn suất chiếu sắp tới.</summary>
    private async Task EnsureCanChangeStatusAsync(Cinema cinema, string newStatus, CancellationToken ct = default)
    {
        if (newStatus == CinemaStatus.Closed && cinema.Status != CinemaStatus.Closed
            && await _repo.HasUpcomingShowtimesAsync(cinema.CinemaID, ct: ct))
            throw new ConflictException("Rạp còn suất chiếu sắp tới nên không thể chuyển sang Closed. Hãy hủy các suất chiếu đó trước.");
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        if (string.Equals(status.Trim(), CinemaStatus.Opened, StringComparison.OrdinalIgnoreCase)) return CinemaStatus.Opened;
        if (string.Equals(status.Trim(), CinemaStatus.Closed, StringComparison.OrdinalIgnoreCase)) return CinemaStatus.Closed;
        throw new BadRequestException("Trạng thái rạp chỉ nhận Opened hoặc Closed");
    }

    private static CinemaDto ToDto(Cinema c) => new()
    {
        CinemaID = c.CinemaID,
        CinemaName = c.CinemaName,
        Address = c.Address,
        Status = c.Status
    };
}
