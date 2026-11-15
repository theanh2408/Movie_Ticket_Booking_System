using Backend.Common;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface IStaffService
{
    Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status);
    Task<AccountResponseDto> GetAsync(int userId);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto);
    Task DeleteAsync(int userId);
}

/// <summary>Admin: toàn chuỗi. Quản lý: chỉ nhân viên thuộc rạp của mình.</summary>
public class StaffService : IStaffService
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUser _current;

    public StaffService(IAccountService accounts, ICurrentUser current)
    {
        _accounts = accounts;
        _current = current;
    }

    public Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status) =>
        _accounts.SearchAsync(keyword, AppRoles.Staff, status, _current.ScopeCinemaId());

    public Task<AccountResponseDto> GetAsync(int userId) => LoadStaffAsync(userId);

    public Task<AccountResponseDto> CreateAsync(CreateAccountDto dto)
    {
        var scope = _current.ScopeCinemaId();
        if (scope != null && dto.CinemaId != null && dto.CinemaId != scope)
            throw new ForbiddenException("Quản lý chỉ được thêm nhân viên vào rạp của mình");

        dto.Role = AppRoles.Staff;
        dto.CinemaId = scope ?? dto.CinemaId;
        return _accounts.CreateAsync(dto, requirePassword: true);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto)
    {
        await LoadStaffAsync(userId);
        if (!_current.IsAdmin) dto.CinemaId = null;   // chỉ Admin được chuyển rạp
        return await _accounts.UpdateAsync(userId, dto);
    }

    public async Task DeleteAsync(int userId)
    {
        await LoadStaffAsync(userId);
        await _accounts.SoftDeleteAsync(userId);
    }

    private async Task<AccountResponseDto> LoadStaffAsync(int userId)
    {
        var dto = await _accounts.GetAsync(userId);
        if (dto.Role != AppRoles.Staff) throw new NotFoundException("Không tìm thấy nhân viên");
        _current.EnsureCinemaAccess(dto.CinemaID);
        return dto;
    }
}
