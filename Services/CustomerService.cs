using Backend.Common;
using Backend.DTOs;
using Backend.Models;

namespace Backend.Services;

public interface ICustomerService
{
    Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status);
    Task<AccountResponseDto> GetAsync(int userId);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto);
    Task DeleteAsync(int userId);
}

public class CustomerService : ICustomerService
{
    private readonly IAccountService _accounts;

    public CustomerService(IAccountService accounts) => _accounts = accounts;

    public Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? status) =>
        _accounts.SearchAsync(keyword, AppRoles.Customer, status, null);

    public Task<AccountResponseDto> GetAsync(int userId) => LoadCustomerAsync(userId);

    public Task<AccountResponseDto> CreateAsync(CreateAccountDto dto)
    {
        dto.Role = AppRoles.Customer;
        dto.CinemaId = null;
        return _accounts.CreateAsync(dto, requirePassword: false);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto)
    {
        await LoadCustomerAsync(userId);
        dto.CinemaId = null;
        return await _accounts.UpdateAsync(userId, dto);
    }

    public async Task DeleteAsync(int userId)
    {
        await LoadCustomerAsync(userId);
        await _accounts.SoftDeleteAsync(userId);
    }

    private async Task<AccountResponseDto> LoadCustomerAsync(int userId)
    {
        var dto = await _accounts.GetAsync(userId);
        if (dto.Role != AppRoles.Customer) throw new NotFoundException("Không tìm thấy khách hàng");
        return dto;
    }
}
