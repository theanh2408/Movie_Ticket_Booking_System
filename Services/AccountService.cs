using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;

namespace Backend.Services;

public interface IAccountService
{
    Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? role, string? status, int? cinemaId);
    Task<AccountResponseDto> GetAsync(int userId);
    Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, bool requirePassword);
    Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto);
    Task SoftDeleteAsync(int userId);
    Task<AccountResponseDto> ChangeRoleAsync(int userId, ChangeRoleDto dto);
    Task<AccountResponseDto> GetMeAsync();
    Task<AccountResponseDto> UpdateMeAsync(UpdateAccountDto dto);
}

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repo;
    private readonly ICinemaRepository _cinemas;
    private readonly ICurrentUser _current;

    public AccountService(IAccountRepository repo, ICinemaRepository cinemas, ICurrentUser current)
    {
        _repo = repo;
        _cinemas = cinemas;
        _current = current;
    }

    public async Task<List<AccountResponseDto>> SearchAsync(string? keyword, string? role, string? status, int? cinemaId)
    {
        var list = await _repo.SearchAsync(keyword, role, status, cinemaId);
        return list.Select(ToDto).ToList();
    }

    public async Task<AccountResponseDto> GetAsync(int userId)
    {
        var acc = await _repo.GetDetailAsync(userId)
                  ?? throw new NotFoundException("Không tìm thấy tài khoản");
        return ToDto(acc);
    }

    public async Task<AccountResponseDto> CreateAsync(CreateAccountDto dto, bool requirePassword)
    {
        var role = NormalizeRole(dto.Role);
        var email = dto.Email.Trim();

        if (requirePassword && string.IsNullOrWhiteSpace(dto.Password))
            throw new BadRequestException("Mật khẩu khởi tạo không được để trống");

        if (await _repo.EmailExistsAsync(email))
            throw new ConflictException("Email đã được sử dụng");

        if (role is AppRoles.Manager or AppRoles.Staff)
        {
            if (dto.CinemaId == null) throw new BadRequestException("Vui lòng chọn rạp cho vai trò này");
            await EnsureCinemaExistsAsync(dto.CinemaId.Value);
        }

        var acc = new Account
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = email,
            DOB = dto.DOB,
            Gender = dto.Gender?.Trim(),
            Role = role,
            Status = AccountStatus.Active,
            PasswordHash = string.IsNullOrWhiteSpace(dto.Password) ? null : PasswordHasher.Hash(dto.Password)
        };

        switch (role)
        {
            case AppRoles.Manager:  acc.Manager = new Manager { CinemaID = dto.CinemaId }; break;
            case AppRoles.Staff:    acc.Staff = new Staff { CinemaID = dto.CinemaId }; break;
            case AppRoles.Customer: acc.Customer = new Customer(); break;
        }

        await _repo.AddAsync(acc);
        return await GetAsync(acc.UserID);
    }

    public async Task<AccountResponseDto> UpdateAsync(int userId, UpdateAccountDto dto)
    {
        var acc = await GetEntityAsync(userId);

        acc.FullName = dto.FullName.Trim();
        acc.Phone = dto.Phone?.Trim();
        acc.DOB = dto.DOB;
        acc.Gender = dto.Gender?.Trim();

        if (dto.CinemaId.HasValue)
        {
            if (acc.Manager == null && acc.Staff == null)
                throw new BadRequestException("Tài khoản này không thuộc rạp nào nên không thể đổi rạp");
            await EnsureCinemaExistsAsync(dto.CinemaId.Value);
            if (acc.Manager != null) acc.Manager.CinemaID = dto.CinemaId;
            if (acc.Staff != null) acc.Staff.CinemaID = dto.CinemaId;
        }

        await _repo.SaveAsync();
        return await GetAsync(userId);
    }

    /// <summary>
    /// Xóa mềm: giữ nguyên mọi dữ liệu liên quan, chỉ đặt Status = Inactive
    /// và xóa thông tin đăng nhập (PasswordHash) để tài khoản không đăng nhập được nữa.
    /// </summary>
    public async Task SoftDeleteAsync(int userId)
    {
        var acc = await GetEntityAsync(userId);

        if (acc.UserID == _current.UserId)
            throw new ConflictException("Không thể tự xóa tài khoản của chính mình");
        if (acc.Status == AccountStatus.Inactive)
            throw new ConflictException("Tài khoản này đã bị vô hiệu hóa trước đó");

        acc.Status = AccountStatus.Inactive;
        acc.PasswordHash = null;
        await _repo.SaveAsync();
    }

    public async Task<AccountResponseDto> ChangeRoleAsync(int userId, ChangeRoleDto dto)
    {
        var acc = await GetEntityAsync(userId);

        if (acc.Role == AppRoles.Admin)
            throw new ForbiddenException("Không thể thay đổi vai trò của Admin");

        var role = NormalizeRole(dto.Role);
        if (role == AppRoles.Admin)
            throw new BadRequestException("Không được gán vai trò Admin");

        int? cinemaId = dto.CinemaId ?? acc.Manager?.CinemaID ?? acc.Staff?.CinemaID;
        if (role is AppRoles.Manager or AppRoles.Staff)
        {
            if (cinemaId == null) throw new BadRequestException("Vui lòng chọn rạp cho vai trò này");
            await EnsureCinemaExistsAsync(cinemaId.Value);
        }

        // Đồng bộ các bảng hồ sơ theo vai trò mới (không bao giờ xóa hồ sơ Customer vì có thể có đơn đặt vé)
        if (role != AppRoles.Manager && acc.Manager != null) { _repo.RemoveManager(acc.Manager); acc.Manager = null; }
        if (role != AppRoles.Staff && acc.Staff != null)     { _repo.RemoveStaff(acc.Staff); acc.Staff = null; }

        if (role == AppRoles.Manager)
        {
            if (acc.Manager == null) acc.Manager = new Manager { CinemaID = cinemaId };
            else acc.Manager.CinemaID = cinemaId;
        }
        else if (role == AppRoles.Staff)
        {
            if (acc.Staff == null) acc.Staff = new Staff { CinemaID = cinemaId };
            else acc.Staff.CinemaID = cinemaId;
        }
        else if (role == AppRoles.Customer && acc.Customer == null)
        {
            acc.Customer = new Customer();
        }

        acc.Role = role;
        await _repo.SaveAsync();
        return await GetAsync(userId);
    }

    public Task<AccountResponseDto> GetMeAsync() => GetAsync(_current.UserId);

    public Task<AccountResponseDto> UpdateMeAsync(UpdateAccountDto dto)
    {
        dto.CinemaId = null;   // không tự đổi rạp của mình
        return UpdateAsync(_current.UserId, dto);
    }

    // ---------- helpers ----------
    private async Task<Account> GetEntityAsync(int userId) =>
        await _repo.GetByIdAsync(userId) ?? throw new NotFoundException("Không tìm thấy tài khoản");

    private async Task EnsureCinemaExistsAsync(int cinemaId)
    {
        if (!await _cinemas.ExistsAsync(cinemaId))
            throw new BadRequestException("Rạp được chọn không tồn tại");
    }

    private static string NormalizeRole(string? role)
    {
        var match = AppRoles.All.FirstOrDefault(r => string.Equals(r, role?.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new BadRequestException("Vai trò không hợp lệ (Admin, Manager, Staff hoặc Customer)");
    }

    private static AccountResponseDto ToDto(Account a)
    {
        var cinema = a.Manager?.Cinema ?? a.Staff?.Cinema;
        return new AccountResponseDto
        {
            UserID = a.UserID,
            FullName = a.FullName,
            Phone = a.Phone,
            Email = a.Email,
            DOB = a.DOB,
            Gender = a.Gender,
            Role = a.Role,
            Status = a.Status,
            CinemaID = a.Manager?.CinemaID ?? a.Staff?.CinemaID,
            CinemaName = cinema?.CinemaName
        };
    }
}
