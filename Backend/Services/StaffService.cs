using Backend.Common;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface IStaffService
{
    Task<PagedResult<AccountResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default);
    Task<AccountResponseDto> GetAsync(int userId, CancellationToken ct = default);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto, CancellationToken ct = default);
    Task DeleteAsync(int userId, CancellationToken ct = default);
}

/// <summary>
/// Nhân viên = tài khoản có Role = Staff, gắn với một rạp qua bảng Staff.
/// BUG-04: Admin quản lý toàn chuỗi; Quản lý chỉ xem / sửa / xóa / thêm nhân viên thuộc rạp của mình (EnsureCinemaAccess).
/// </summary>
public class StaffService : IStaffService
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUser _current;

    public StaffService(IAccountService accounts, ICurrentUser current)
    {
        _accounts = accounts;
        _current = current;
    }

    public Task<PagedResult<AccountResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default) =>
        _accounts.SearchAsync(keyword, AppRoles.Staff, status, _current.ScopeCinemaId(), paging, ct: ct);

    public Task<AccountResponseDto> GetAsync(int userId, CancellationToken ct = default) => LoadStaffAsync(userId, ct: ct);

    public Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default)
    {
        var scope = _current.ScopeCinemaId();
        if (scope != null && dto.CinemaId != null && dto.CinemaId != scope)
            throw new ForbiddenException("Quản lý chỉ được thêm nhân viên vào rạp của mình");

        dto.Role = AppRoles.Staff;
        dto.CinemaId = scope ?? dto.CinemaId;   // Quản lý: tự gắn rạp của mình; Admin: chọn rạp
        return _accounts.CreateAsync(dto, requirePassword: true, ct: ct);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto, CancellationToken ct = default)
    {
        await LoadStaffAsync(userId, ct: ct);
        if (!_current.IsAdmin) dto.CinemaId = null;   // chỉ Admin được chuyển nhân viên sang rạp khác
        return await _accounts.UpdateAsync(userId, dto, ct: ct);
    }

    public async Task DeleteAsync(int userId, CancellationToken ct = default)
    {
        await LoadStaffAsync(userId, ct: ct);
        await _accounts.SoftDeleteAsync(userId, ct: ct);
    }

    private async Task<AccountResponseDto> LoadStaffAsync(int userId, CancellationToken ct = default)
    {
        var dto = await _accounts.GetAsync(userId, ct: ct);
        if (dto.Role != AppRoles.Staff) throw new NotFoundException("Không tìm thấy nhân viên");
        _current.EnsureCinemaAccess(dto.CinemaID);   // Quản lý rạp khác -> 403
        return dto;
    }
}
