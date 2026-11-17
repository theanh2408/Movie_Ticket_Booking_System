using Backend.Common;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Cinema> Cinemas => Set<Cinema>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<ShowtimeSeat> ShowtimeSeats => Set<ShowtimeSeat>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<BookingPromotion> BookingPromotions => Set<BookingPromotion>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<decimal>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ----- Tài khoản -----
        mb.Entity<Account>(e =>
        {
            e.ToTable("Account");
            e.HasKey(x => x.UserID);
            e.HasOne(a => a.Manager).WithOne(m => m.Account).HasForeignKey<Manager>(m => m.UserID);
            e.HasOne(a => a.Customer).WithOne(c => c.Account).HasForeignKey<Customer>(c => c.UserID);
        });
        mb.Entity<Manager>(e =>
        {
            e.ToTable("Manager"); e.HasKey(x => x.ManagerID);
            e.HasOne(m => m.Cinema).WithMany().HasForeignKey(m => m.CinemaID);
        });
        mb.Entity<Customer>(e => { e.ToTable("Customer"); e.HasKey(x => x.CustomerID); });

        // ----- Rạp / phòng / ghế -----
        mb.Entity<Cinema>(e => { e.ToTable("Cinema"); e.HasKey(x => x.CinemaID); });
        mb.Entity<Room>(e =>
        {
            e.ToTable("Room"); e.HasKey(x => x.RoomID);
            e.HasOne(r => r.Cinema).WithMany(c => c.Rooms).HasForeignKey(r => r.CinemaID);
        });
        mb.Entity<Seat>(e =>
        {
            e.ToTable("Seat"); e.HasKey(x => x.SeatID);
            e.Property(x => x.Row).HasColumnName("Row");
            e.Property(x => x.Column).HasColumnName("Column");
            e.HasOne(s => s.Room).WithMany(r => r.Seats).HasForeignKey(s => s.RoomID);
        });

        // ----- Phim -----
        mb.Entity<Movie>(e => { e.ToTable("Movie"); e.HasKey(x => x.MovieID); });
        mb.Entity<Genre>(e =>
        {
            e.ToTable("Genre"); e.HasKey(x => x.GenreID);
            e.Property(x => x.GenreID).HasColumnName("Genre_ID");
        });
        mb.Entity<MovieGenre>(e =>
        {
            e.ToTable("Movie_Genre");
            e.HasKey(x => new { x.MovieID, x.GenreID });
            e.Property(x => x.GenreID).HasColumnName("Genre_ID");
            e.HasOne(x => x.Movie).WithMany(m => m.MovieGenres).HasForeignKey(x => x.MovieID);
            e.HasOne(x => x.Genre).WithMany(g => g.MovieGenres).HasForeignKey(x => x.GenreID);
        });

        // ----- Suất chiếu -----
        mb.Entity<Showtime>(e =>
        {
            e.ToTable("Showtime"); e.HasKey(x => x.ShowtimeID);
            e.Property(x => x.StartHour).HasColumnName("Start_hour");
            e.HasOne(s => s.Movie).WithMany().HasForeignKey(s => s.MovieID);
            e.HasOne(s => s.Room).WithMany(r => r.Showtimes).HasForeignKey(s => s.RoomID);
        });
        mb.Entity<ShowtimeSeat>(e =>
        {
            e.ToTable("Showtime_Seat"); e.HasKey(x => x.ShowtimeSeatID);
            e.Property(x => x.ShowtimeSeatID).HasColumnName("Showtime_SeatID");
            e.HasOne(x => x.Showtime).WithMany(s => s.ShowtimeSeats).HasForeignKey(x => x.ShowtimeID);
            e.HasOne(x => x.Seat).WithMany().HasForeignKey(x => x.SeatID);
        });

        // ----- Đặt vé / thanh toán / hóa đơn -----
        mb.Entity<Booking>(e =>
        {
            e.ToTable("Booking"); e.HasKey(x => x.BookingID);
            e.HasOne(b => b.Customer).WithMany(c => c.Bookings).HasForeignKey(b => b.CustomerID);
        });
        mb.Entity<Ticket>(e =>
        {
            e.ToTable("Ticket"); e.HasKey(x => x.TicketID);
            e.Property(x => x.ShowtimeSeatID).HasColumnName("Showtime_SeatID");
            e.Property(x => x.BookingID).HasColumnName("Booking_ID");
            e.HasOne(t => t.ShowtimeSeat).WithMany().HasForeignKey(t => t.ShowtimeSeatID);
            e.HasOne(t => t.Booking).WithMany(b => b.Tickets).HasForeignKey(t => t.BookingID);
        });
        mb.Entity<Payment>(e =>
        {
            e.ToTable("Payment"); e.HasKey(x => x.PaymentID);
            e.HasOne(p => p.Booking).WithMany(b => b.Payments).HasForeignKey(p => p.BookingID);
        });
        mb.Entity<Invoice>(e =>
        {
            e.ToTable("Invoice"); e.HasKey(x => x.InvoiceID);
            e.HasOne(i => i.Booking).WithOne(b => b.Invoice).HasForeignKey<Invoice>(i => i.BookingID);
        });

        // ----- Khuyến mãi -----
        mb.Entity<Promotion>(e => { e.ToTable("Promotion"); e.HasKey(x => x.PromotionID); });
        mb.Entity<BookingPromotion>(e => { e.ToTable("Booking_Promotion"); e.HasKey(x => x.BookingPromotionID); });
    }
}

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
                db.Cinemas.Add(new Cinema { CinemaName = "Rạp trung tâm", Address = "Quận 1, TP.HCM", Status = "Active" });
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Không tạo được dữ liệu mẫu. Bạn đã chạy file Create_Movie_Ticket_Booking_System.sql chưa?");
        }
    }
}
