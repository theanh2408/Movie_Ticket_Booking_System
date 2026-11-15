using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
}

public class AuthService : IAuthService
{
    private readonly IAccountRepository _accounts;
    private readonly ITokenService _tokens;

    public AuthService(IAccountRepository accounts, ITokenService tokens)
    {
        _accounts = accounts;
        _tokens = tokens;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var acc = await _accounts.GetByEmailAsync(dto.Email.Trim());

        if (acc?.PasswordHash == null || !PasswordHasher.Verify(dto.Password, acc.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng");

        if (acc.Status != AccountStatus.Active)
            throw new ForbiddenException("Tài khoản đã bị vô hiệu hóa");

        if (acc.Role is not (AppRoles.Admin or AppRoles.Manager or AppRoles.Staff))
            throw new ForbiddenException("Tài khoản này không có quyền vào trang quản trị");

        int? cinemaId = acc.Manager?.CinemaID ?? acc.Staff?.CinemaID;
        var (token, expires) = _tokens.Create(acc, cinemaId);

        return new LoginResponseDto
        {
            Token = token,
            ExpiresAt = expires,
            UserId = acc.UserID,
            FullName = acc.FullName,
            Role = acc.Role!,
            CinemaId = cinemaId
        };
    }
}
