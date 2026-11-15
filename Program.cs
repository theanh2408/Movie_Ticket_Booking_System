using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Backend.Common;
using Backend.Data;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ===== Repositories (DAO) =====
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICinemaRepository, CinemaRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();

// ===== Services (BUS) =====
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// ===== JWT =====
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Thiếu Jwt:Key trong appsettings.json");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;   // giữ nguyên tên claim: sub, role, cinemaId
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();   // xem Extensions/SwaggerExtensions.cs

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Configuration.GetValue<bool>("Seed:Enabled"))
{
    await DbSeeder.SeedAsync(app.Services);
}

app.Run();

// ======================================================================
// Các lớp tiện ích dùng chung (xử lý lỗi, phân quyền, JWT, Swagger...)
// được để trong Program.cs để giữ đúng cấu trúc thư mục của nhóm.
// ======================================================================
namespace Backend.Common
{
    public class AppException : Exception
    {
        public int StatusCode { get; }
        public AppException(string message, int statusCode) : base(message) => StatusCode = statusCode;
    }

    public class BadRequestException : AppException   { public BadRequestException(string m) : base(m, 400) { } }
    public class UnauthorizedException : AppException { public UnauthorizedException(string m) : base(m, 401) { } }
    public class ForbiddenException : AppException    { public ForbiddenException(string m) : base(m, 403) { } }
    public class NotFoundException : AppException     { public NotFoundException(string m) : base(m, 404) { } }
    public class ConflictException : AppException     { public ConflictException(string m) : base(m, 409) { } }

    /// <summary>
    /// Bắt mọi lỗi và trả JSON { "message": "..." } với mã HTTP phù hợp,
    /// nên Controller không cần try/catch.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly bool _showDetails;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IConfiguration config)
        {
            _next = next;
            _logger = logger;
            _showDetails = config.GetValue<bool>("ShowErrorDetails");
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                if (ctx.Response.HasStarted) throw;

                int status;
                string message;

                switch (ex)
                {
                    case AppException app:
                        status = app.StatusCode;
                        message = app.Message;
                        break;

                    case DbUpdateException:
                        _logger.LogError(ex, "Lỗi lưu database");
                        status = 409;
                        message = "Không thể lưu: dữ liệu bị trùng hoặc đang được sử dụng ở nơi khác."
                                  + (_showDetails ? " Chi tiết: " + ex.GetBaseException().Message : "");
                        break;

                    default:
                        _logger.LogError(ex, "Lỗi không xử lý được");
                        status = 500;
                        message = _showDetails
                            ? ex.GetBaseException().Message
                            : "Lỗi hệ thống, vui lòng thử lại sau.";
                        break;
                }

                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { message });
            }
        }
    }

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

    /// <summary>Băm mật khẩu bằng PBKDF2-SHA256. Định dạng lưu: base64(salt).base64(hash)</summary>
    public static class PasswordHasher
    {
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            var parts = stored.Split('.');
            if (parts.Length != 2) return false;
            try
            {
                var salt = Convert.FromBase64String(parts[0]);
                var expected = Convert.FromBase64String(parts[1]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    internal static class DateRange
    {
        public static DateTime? Start(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue);

        /// <summary>Cận trên loại trừ: đầu ngày kế tiếp của ngày "đến".</summary>
        public static DateTime? EndExclusive(DateOnly? d) => d?.AddDays(1).ToDateTime(TimeOnly.MinValue);
    }

    /// <summary>
    /// Thêm nút "Authorize" vào Swagger để dán JWT token.
    /// Viết cho Swashbuckle 10.x (Microsoft.OpenApi 2.x).
    /// Nếu build báo lỗi ở file này (do khác phiên bản), xem phần ghi chú cuối file.
    /// </summary>
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Dán token lấy từ POST /api/auth/login (không cần gõ chữ Bearer)."
                });

                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
                });
            });
            return services;
        }
    }

    /* ----- Nếu dùng Swashbuckle 6.x - 9.x (Microsoft.OpenApi 1.x), thay toàn bộ bằng:

    using Microsoft.OpenApi.Models;

    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    */
}
