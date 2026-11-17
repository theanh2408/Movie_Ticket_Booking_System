using Backend.DTOs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>UC-6.2, 6.6 Xem / tìm kiếm đơn đặt vé (Admin: toàn chuỗi; Quản lý: rạp của mình)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _service;

    public BookingsController(IBookingService service) => _service = service;

    /// <summary>keyword: mã đơn, tên/SĐT/email khách. from/to: ngày tạo đơn (yyyy-MM-dd)</summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingListItemDto>>> Search(
        [FromQuery] int? cinemaId, [FromQuery] string? keyword, [FromQuery] string? status,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to) =>
        Ok(await _service.SearchAsync(cinemaId, keyword, status, from, to));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingDetailDto>> Get(int id) => Ok(await _service.GetAsync(id));
}

/// <summary>UC-2.x Quản lý khách hàng (Admin, Quản lý). Route dùng UserID.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.AdminOrManager)]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;

    public CustomersController(ICustomerService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> Search([FromQuery] string? keyword, [FromQuery] string? status) =>
        Ok(await _service.SearchAsync(keyword, status));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId) => Ok(await _service.GetAsync(userId));

    /// <summary>Thêm khách hàng. Password không bắt buộc (khách có thể tự đăng ký mật khẩu sau).</summary>
    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { userId = created.UserID }, created);
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int userId, [FromBody] UpdateAccountDto dto) =>
        Ok(await _service.UpdateAsync(userId, dto));

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Delete(int userId)
    {
        await _service.DeleteAsync(userId);
        return NoContent();
    }
}

/// <summary>UC-10.x Chương trình khuyến mãi</summary>
[ApiController]
[Route("api/[controller]")]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionService _service;

    public PromotionsController(IPromotionService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<ActionResult<List<PromotionResponseDto>>> Search([FromQuery] string? keyword, [FromQuery] string? status) =>
        Ok(await _service.SearchAsync(keyword, status));

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public async Task<ActionResult<PromotionResponseDto>> Get(int id) => Ok(await _service.GetAsync(id));

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> Create([FromBody] PromotionUpsertDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = created.PromotionID }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> Update(int id, [FromBody] PromotionUpsertDto dto) =>
        Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>UC-10.6 Bật/tắt chương trình: Active | Inactive</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<ActionResult<PromotionResponseDto>> UpdateStatus(int id, [FromBody] StatusDto dto) =>
        Ok(await _service.UpdateStatusAsync(id, dto.Status));

    /// <summary>UC-10.7 Kiểm tra mã và tính số tiền giảm (khách hàng nhập mã thủ công)</summary>
    [HttpPost("validate")]
    [Authorize]
    public async Task<ActionResult<ApplyPromotionResultDto>> Validate([FromBody] ApplyPromotionDto dto) =>
        Ok(await _service.ApplyAsync(dto));
}

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service) => _service = service;

    /// <summary>Đăng nhập (Admin / Manager / Staff). Trả về JWT token.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto dto) =>
        Ok(await _service.LoginAsync(dto));
}

/// <summary>Thông tin tài khoản của chính người đang đăng nhập (UC-3.2, 3.3 phía nhân viên).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IAccountService _service;

    public ProfileController(IAccountService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<AccountResponseDto>> Get() => Ok(await _service.GetMeAsync());

    [HttpPut]
    public async Task<ActionResult<AccountResponseDto>> Update([FromBody] UpdateAccountDto dto) =>
        Ok(await _service.UpdateMeAsync(dto));
}

/// <summary>UC-1.x Quản lý tài khoản (chỉ Admin)</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountsController(IAccountService service) => _service = service;

    /// <summary>Danh sách + tìm kiếm theo tên/email/SĐT, lọc theo vai trò, trạng thái, rạp</summary>
    [HttpGet]
    public async Task<ActionResult<List<AccountResponseDto>>> Search(
        [FromQuery] string? keyword, [FromQuery] string? role, [FromQuery] string? status, [FromQuery] int? cinemaId) =>
        Ok(await _service.SearchAsync(keyword, role, status, cinemaId));

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Get(int userId) => Ok(await _service.GetAsync(userId));

    [HttpPost]
    public async Task<ActionResult<AccountResponseDto>> Create([FromBody] CreateAccountDto dto)
    {
        var created = await _service.CreateAsync(dto, requirePassword: true);
        return CreatedAtAction(nameof(Get), new { userId = created.UserID }, created);
    }

    /// <summary>Sửa thông tin (không cho sửa email)</summary>
    [HttpPut("{userId:int}")]
    public async Task<ActionResult<AccountResponseDto>> Update(int userId, [FromBody] UpdateAccountDto dto) =>
        Ok(await _service.UpdateAsync(userId, dto));

    /// <summary>Xóa mềm: Status = Inactive, xóa thông tin đăng nhập</summary>
    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Delete(int userId)
    {
        await _service.SoftDeleteAsync(userId);
        return NoContent();
    }

    /// <summary>UC-1.6 Phân quyền (không được gán/đổi vai trò Admin)</summary>
    [HttpPut("{userId:int}/role")]
    public async Task<ActionResult<AccountResponseDto>> ChangeRole(int userId, [FromBody] ChangeRoleDto dto) =>
        Ok(await _service.ChangeRoleAsync(userId, dto));
}
