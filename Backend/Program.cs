using Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký AppDbContext với DefaultConnection từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Controllers và tuần tự hóa JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Cấu hình Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Cấu hình CORS cho phép http://localhost:5173 và http://localhost:3000
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Kích hoạt CORS trước Controllers
app.UseCors("AllowFrontend");

// Kích hoạt Swagger UI trong môi trường Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie Ticket Booking API v1");
        c.RoutePrefix = "swagger"; // Đường dẫn truy cập: http://localhost:5231/swagger
    });
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

// Map Controllers
app.MapControllers();

// Tự động seed dữ liệu mẫu khi khởi động nếu database đang trống
await DbSeeder.SeedAsync(app.Services);

app.Run();
