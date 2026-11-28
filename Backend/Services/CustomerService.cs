using Backend.Common;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface ICustomerService
{
    Task<PagedResult<AccountResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default);
    Task<AccountResponseDto> GetAsync(int userId, CancellationToken ct = default);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto, CancellationToken ct = default);
    Task DeleteAsync(int userId, CancellationToken ct = default);
}

public class CustomerService : ICustomerService
{
    private readonly IAccountService _accounts;

    public CustomerService(IAccountService accounts) => _accounts = accounts;

    public Task<PagedResult<AccountResponseDto>> SearchAsync(string? keyword, string? status, PageQuery paging, CancellationToken ct = default) =>
        _accounts.SearchAsync(keyword, AppRoles.Customer, status, null, paging, ct: ct);

    public Task<AccountResponseDto> GetAsync(int userId, CancellationToken ct = default) => LoadCustomerAsync(userId, ct: ct);

    public Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, CancellationToken ct = default)
    {
        dto.Role = AppRoles.Customer;
        dto.CinemaId = null;
        return _accounts.CreateAsync(dto, requirePassword: false, ct: ct);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto, CancellationToken ct = default)
    {
        await LoadCustomerAsync(userId, ct: ct);
        dto.CinemaId = null;
        return await _accounts.UpdateAsync(userId, dto, ct: ct);
    }

    public async Task DeleteAsync(int userId, CancellationToken ct = default)
    {
        await LoadCustomerAsync(userId, ct: ct);
        await _accounts.SoftDeleteAsync(userId, ct: ct);
    }

    private async Task<AccountResponseDto> LoadCustomerAsync(int userId, CancellationToken ct = default)
    {
        var dto = await _accounts.GetAsync(userId, ct: ct);
        if (dto.Role != AppRoles.Customer) throw new NotFoundException("Không tìm thấy khách hàng");
        return dto;
    }
}
