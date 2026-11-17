using System.Text;
using Backend.Common;
using Backend.DTOs;
using Backend.Models;
using Backend.Repositories;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public interface IBookingService
{
    Task<List<BookingListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to);
    Task<BookingDetailDto> GetAsync(int id);
}

public class BookingService : IBookingService
{
    private readonly IBookingRepository _repo;
    private readonly ICurrentUser _current;

    public BookingService(IBookingRepository repo, ICurrentUser current)
    {
        _repo = repo;
        _current = current;
    }

    public async Task<List<BookingListItemDto>> SearchAsync(int? cinemaId, string? keyword, string? status, DateOnly? from, DateOnly? to)
    {
        var scope = _current.ScopeCinemaId() ?? cinemaId;
        var list = await _repo.SearchAsync(scope, keyword, status, DateRange.Start(from), DateRange.EndExclusive(to));
        return list.Select(b => new BookingListItemDto
        {
            BookingID = b.BookingID,
            CustomerID = b.CustomerID,
            CustomerName = b.Customer?.Account?.FullName,
            CustomerPhone = b.Customer?.Account?.Phone,
            CreatedDate = b.CreatedDate,
            TotalAmount = b.TotalAmount,
            Status = b.Status
        }).ToList();
    }

    public async Task<BookingDetailDto> GetAsync(int id)
    {
        var b = await _repo.GetDetailAsync(id, _current.ScopeCinemaId())
                ?? throw new NotFoundException("Không tìm thấy đơn đặt vé");
        return ToDetail(b);
    }

    public static BookingDetailDto ToDetail(Booking b)
    {
        var st = b.Tickets.FirstOrDefault()?.ShowtimeSeat?.Showtime;
        return new BookingDetailDto
        {
            BookingID = b.BookingID,
            Status = b.Status,
            CreatedDate = b.CreatedDate,
            ExpiredAt = b.ExpiredAt,
            SubTotal = b.SubTotal,
            DiscountAmount = b.DiscountAmount,
            TotalAmount = b.TotalAmount,
            CustomerID = b.CustomerID,
            CustomerName = b.Customer?.Account?.FullName,
            CustomerPhone = b.Customer?.Account?.Phone,
            CustomerEmail = b.Customer?.Account?.Email,
            MovieTitle = st?.Movie?.Title,
            CinemaName = st?.Room?.Cinema?.CinemaName,
            RoomName = st?.Room?.RoomName,
            ShowDate = st?.Date,
            StartTime = st?.StartHour,
            Tickets = b.Tickets.Select(t => new TicketDto
            {
                TicketID = t.TicketID,
                SeatCode = (t.ShowtimeSeat?.Seat?.Row ?? "") + (t.ShowtimeSeat?.Seat?.Column ?? ""),
                Price = t.Price,
                Status = t.Status
            }).ToList(),
            Payments = b.Payments.Select(PaymentService.ToDto).ToList(),
            InvoiceID = b.Invoice?.InvoiceID
        };
    }
}

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

        if (role == AppRoles.Manager)
        {
            if (dto.CinemaId == null) throw new BadRequestException("Vui lòng chọn rạp mà quản lý phụ trách");
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
            if (acc.Manager == null)
                throw new BadRequestException("Chỉ tài khoản quản lý mới gắn với rạp nên không thể đổi rạp");
            await EnsureCinemaExistsAsync(dto.CinemaId.Value);
            acc.Manager.CinemaID = dto.CinemaId;
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

        int? cinemaId = dto.CinemaId ?? acc.Manager?.CinemaID;
        if (role == AppRoles.Manager)
        {
            if (cinemaId == null) throw new BadRequestException("Vui lòng chọn rạp mà quản lý phụ trách");
            await EnsureCinemaExistsAsync(cinemaId.Value);
        }

        // Đồng bộ các bảng hồ sơ theo vai trò mới (không bao giờ xóa hồ sơ Customer vì có thể có đơn đặt vé)
        if (role != AppRoles.Manager && acc.Manager != null) { _repo.RemoveManager(acc.Manager); acc.Manager = null; }

        if (role == AppRoles.Manager)
        {
            if (acc.Manager == null) acc.Manager = new Manager { CinemaID = cinemaId };
            else acc.Manager.CinemaID = cinemaId;
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
        var cinema = a.Manager?.Cinema;
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
            CinemaID = a.Manager?.CinemaID,
            CinemaName = cinema?.CinemaName
        };
    }
}

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

        int? cinemaId = acc.Manager?.CinemaID;
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

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) Create(Account account, int? cinemaId);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAt) Create(Account account, int? cinemaId)
    {
        var key = _config["Jwt:Key"]!;
        var expires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpireMinutes", 480));

        var claims = new Dictionary<string, object>
        {
            ["sub"] = account.UserID.ToString(),
            ["role"] = account.Role ?? "",
            ["name"] = account.FullName ?? "",
            ["email"] = account.Email ?? ""
        };
        if (cinemaId.HasValue) claims["cinemaId"] = cinemaId.Value.ToString();

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = expires,
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expires);
    }
}
