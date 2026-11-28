using Backend.Common;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

/// <summary>
/// Tạo dữ liệu mẫu tối thiểu nếu DB còn trống: 1 tài khoản Admin, vài thể loại, 1 rạp.
/// Chỉ chèn khi chưa có dữ liệu tương ứng. Tắt bằng "Seed:Enabled": false trong appsettings.json.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        try
        {
            if (!await db.Accounts.AnyAsync(a => a.Role == AppRoles.Admin))
            {
                db.Accounts.Add(new Account
                {
                    FullName = "Administrator",
                    Email = "admin@cinema.local",
                    Role = AppRoles.Admin,
                    Status = AccountStatus.Active,
                    PasswordHash = PasswordHasher.Hash("Admin@123")
                });
                logger.LogInformation("Đã tạo tài khoản admin mẫu: admin@cinema.local / Admin@123");
            }

            if (!await db.Genres.AnyAsync())
            {
                db.Genres.AddRange(
                    new Genre { GenreName = "Hành động" },
                    new Genre { GenreName = "Hài" },
                    new Genre { GenreName = "Kinh dị" },
                    new Genre { GenreName = "Tình cảm" },
                    new Genre { GenreName = "Hoạt hình" });
            }

            if (!await db.Cinemas.AnyAsync())
            {
                db.Cinemas.Add(new Cinema { CinemaName = "Rạp trung tâm", Address = "Quận 1, TP.HCM", Status = CinemaStatus.Opened });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không tạo được dữ liệu mẫu. Bạn đã chạy file Create_Movie_Ticket_Booking_System.sql và Thêm bảng Staff.sql chưa?");
        }
    }
}
