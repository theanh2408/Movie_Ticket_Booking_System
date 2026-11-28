using System.Text;
using Backend.Common;
using Backend.Data;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;


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
builder.Services.AddScoped<ITicketCheckService, TicketCheckService>();
builder.Services.AddScoped<ICounterBookingService, CounterBookingService>();
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

        // BUG-02: JWT tự nó không biết tài khoản đã bị xóa mềm / đổi quyền. Mỗi request có token đều đối chiếu lại DB,
        // nên token cũ bị từ chối (401) ngay lập tức thay vì sống tới hết ExpireMinutes.
        o.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                if (!int.TryParse(principal?.FindFirst("sub")?.Value, out var userId))
                {
                    context.Fail("Token không hợp lệ");
                    return;
                }

                var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountRepository>();
                var state = await accounts.GetAuthStateAsync(userId, ct: context.HttpContext.RequestAborted);

                var tokenRole = principal?.FindFirst("role")?.Value;
                var tokenCinema = principal?.FindFirst("cinemaId")?.Value;

                if (state == null                                   // tài khoản không còn tồn tại
                    || state.Status != AccountStatus.Active         // đã xóa mềm / vô hiệu hóa
                    || state.Role != tokenRole                      // bị đổi vai trò
                    || tokenCinema != state.CinemaId?.ToString())   // bị đổi rạp phụ trách
                {
                    context.Fail("Phiên đăng nhập không còn hiệu lực, vui lòng đăng nhập lại");
                }
            }
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
