using System.Security.Claims;
using Backend.Models;

namespace Backend.Common;

public interface ICurrentUser
{
    int UserId { get; }
    string Role { get; }
    int? CinemaId { get; }
    bool IsAdmin { get; }
    bool IsManager { get; }

    /// <summary>Admin -> null (toàn chuỗi). Quản lý -> id rạp của mình. Vai trò khác -> 403.</summary>
    int? ScopeCinemaId();

    /// <summary>Ném 403 nếu người dùng không được thao tác trên rạp này.</summary>
    void EnsureCinemaAccess(int? cinemaId);
}

public class CurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public CurrentUser(IHttpContextAccessor accessor) => _user = accessor.HttpContext?.User;

    public int UserId =>
        int.TryParse(_user?.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new UnauthorizedException("Chưa đăng nhập");

    public string Role => _user?.FindFirst("role")?.Value ?? "";

    public int? CinemaId =>
        int.TryParse(_user?.FindFirst("cinemaId")?.Value, out var id) ? id : null;

    public bool IsAdmin => Role == AppRoles.Admin;
    public bool IsManager => Role == AppRoles.Manager;

    public int? ScopeCinemaId()
    {
        if (IsAdmin) return null;
        if (IsManager)
            return CinemaId ?? throw new ForbiddenException("Tài khoản quản lý chưa được gán rạp");
        throw new ForbiddenException("Bạn không có quyền thực hiện thao tác này");
    }

    public void EnsureCinemaAccess(int? cinemaId)
    {
        var scope = ScopeCinemaId();
        if (scope != null && scope != cinemaId)
            throw new ForbiddenException("Bạn chỉ được thao tác trong rạp mình phụ trách");
    }
}
