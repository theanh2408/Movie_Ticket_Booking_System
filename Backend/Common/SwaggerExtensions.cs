using Microsoft.OpenApi;

namespace Backend.Common;

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
