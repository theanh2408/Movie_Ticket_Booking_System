using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<Manager> Managers { get; set; }
    public virtual DbSet<Movie> Movies { get; set; }
    public virtual DbSet<Genre> Genres { get; set; }
    public virtual DbSet<MovieGenre> MovieGenres { get; set; }
    public virtual DbSet<Cinema> Cinemas { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<Seat> Seats { get; set; }
    public virtual DbSet<Showtime> Showtimes { get; set; }
    public virtual DbSet<ShowtimeSeat> ShowtimeSeats { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<Ticket> Tickets { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<BookingPromotion> BookingPromotions { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }
    public virtual DbSet<Invoice> Invoices { get; set; }
    public virtual DbSet<InvoiceDetail> InvoiceDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account
        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Account");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role).HasMaxLength(50);
        });

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");
            entity.HasKey(e => e.CustomerId);
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasIndex(e => e.UserId, "UQ_Customer_User").IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(p => p.Customer)
                .HasForeignKey<Customer>(e => e.UserId)
                .HasConstraintName("FK_Customer_Account");
        });

        // Manager
        modelBuilder.Entity<Manager>(entity =>
        {
            entity.ToTable("Manager");
            entity.HasKey(e => e.ManagerId);
            entity.Property(e => e.ManagerId).HasColumnName("ManagerID");
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.CinemaId).HasColumnName("CinemaID");

            entity.HasIndex(e => e.UserId, "UQ_Manager_User").IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(p => p.Manager)
                .HasForeignKey<Manager>(e => e.UserId)
                .HasConstraintName("FK_Manager_Account");

            entity.HasOne(e => e.Cinema)
                .WithMany(p => p.Managers)
                .HasForeignKey(e => e.CinemaId)
                .HasConstraintName("FK_Manager_Cinema");
        });

        // Movie
        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("Movie");
            entity.HasKey(e => e.MovieId);
            entity.Property(e => e.MovieId).HasColumnName("MovieID");
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.PosterUrl).HasColumnName("PosterURL").HasMaxLength(500);
            entity.Property(e => e.TrailerUrl).HasColumnName("TrailerURL").HasMaxLength(500);
            entity.Property(e => e.AgeRating).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // Genre
        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genre");
            entity.HasKey(e => e.GenreId);
            entity.Property(e => e.GenreId).HasColumnName("Genre_ID");
            entity.Property(e => e.GenreName).HasMaxLength(100);
        });

        // Movie_Genre
        modelBuilder.Entity<MovieGenre>(entity =>
        {
            entity.ToTable("Movie_Genre");
            entity.HasKey(e => new { e.MovieId, e.GenreId });
            entity.Property(e => e.MovieId).HasColumnName("MovieID");
            entity.Property(e => e.GenreId).HasColumnName("Genre_ID");

            entity.HasOne(e => e.Movie)
                .WithMany(p => p.MovieGenres)
                .HasForeignKey(e => e.MovieId)
                .HasConstraintName("FK_MovieGenre_Movie");

            entity.HasOne(e => e.Genre)
                .WithMany(p => p.MovieGenres)
                .HasForeignKey(e => e.GenreId)
                .HasConstraintName("FK_MovieGenre_Genre");
        });

        // Cinema
        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("Cinema");
            entity.HasKey(e => e.CinemaId);
            entity.Property(e => e.CinemaId).HasColumnName("CinemaID");
            entity.Property(e => e.CinemaName).HasMaxLength(150);
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // Room
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room");
            entity.HasKey(e => e.RoomId);
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.CinemaId).HasColumnName("CinemaID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Cinema)
                .WithMany(p => p.Rooms)
                .HasForeignKey(e => e.CinemaId)
                .HasConstraintName("FK_Room_Cinema");
        });

        // Seat
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("Seat");
            entity.HasKey(e => e.SeatId);
            entity.Property(e => e.SeatId).HasColumnName("SeatID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Row).HasColumnName("Row").HasMaxLength(20);
            entity.Property(e => e.Column).HasColumnName("Column").HasMaxLength(20);

            entity.HasOne(e => e.Room)
                .WithMany(p => p.Seats)
                .HasForeignKey(e => e.RoomId)
                .HasConstraintName("FK_Seat_Room");
        });

        // Showtime
        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.ToTable("Showtime");
            entity.HasKey(e => e.ShowtimeId);
            entity.Property(e => e.ShowtimeId).HasColumnName("ShowtimeID");
            entity.Property(e => e.StartHour).HasColumnName("Start_hour");
            entity.Property(e => e.Date).HasColumnName("Date");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.MovieId).HasColumnName("MovieID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");

            entity.HasOne(e => e.Movie)
                .WithMany(p => p.Showtimes)
                .HasForeignKey(e => e.MovieId)
                .HasConstraintName("FK_Showtime_Movie");

            entity.HasOne(e => e.Room)
                .WithMany(p => p.Showtimes)
                .HasForeignKey(e => e.RoomId)
                .HasConstraintName("FK_Showtime_Room");
        });

        // Showtime_Seat
        modelBuilder.Entity<ShowtimeSeat>(entity =>
        {
            entity.ToTable("Showtime_Seat");
            entity.HasKey(e => e.ShowtimeSeatId);
            entity.Property(e => e.ShowtimeSeatId).HasColumnName("Showtime_SeatID");
            entity.Property(e => e.ShowtimeId).HasColumnName("ShowtimeID");
            entity.Property(e => e.SeatId).HasColumnName("SeatID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Showtime)
                .WithMany(p => p.ShowtimeSeats)
                .HasForeignKey(e => e.ShowtimeId)
                .HasConstraintName("FK_ShowtimeSeat_Showtime");

            entity.HasOne(e => e.Seat)
                .WithMany(p => p.ShowtimeSeats)
                .HasForeignKey(e => e.SeatId)
                .HasConstraintName("FK_ShowtimeSeat_Seat");
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Booking");
            entity.HasKey(e => e.BookingId);
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Customer)
                .WithMany(p => p.Bookings)
                .HasForeignKey(e => e.CustomerId)
                .HasConstraintName("FK_Booking_Customer");
        });

        // Ticket
        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Ticket");
            entity.HasKey(e => e.TicketId);
            entity.Property(e => e.TicketId).HasColumnName("TicketID");
            entity.Property(e => e.ShowtimeSeatId).HasColumnName("Showtime_SeatID");
            entity.Property(e => e.BookingId).HasColumnName("Booking_ID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasIndex(e => e.ShowtimeSeatId, "UQ_Ticket_ShowtimeSeat").IsUnique();

            entity.HasOne(e => e.ShowtimeSeat)
                .WithOne(p => p.Ticket)
                .HasForeignKey<Ticket>(e => e.ShowtimeSeatId)
                .HasConstraintName("FK_Ticket_ShowtimeSeat");

            entity.HasOne(e => e.Booking)
                .WithMany(p => p.Tickets)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("FK_Ticket_Booking");
        });

        // Promotion
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotion");
            entity.HasKey(e => e.PromotionId);
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.PromotionName).HasMaxLength(255);
            entity.Property(e => e.DiscountType).HasMaxLength(50);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // Booking_Promotion
        modelBuilder.Entity<BookingPromotion>(entity =>
        {
            entity.ToTable("Booking_Promotion");
            entity.HasKey(e => e.BookingPromotionId);
            entity.Property(e => e.BookingPromotionId).HasColumnName("BookingPromotionID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.AppliedAt).HasColumnType("datetime");

            entity.HasOne(e => e.Booking)
                .WithMany(p => p.BookingPromotions)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("FK_BookingPromotion_Booking");

            entity.HasOne(e => e.Promotion)
                .WithMany(p => p.BookingPromotions)
                .HasForeignKey(e => e.PromotionId)
                .HasConstraintName("FK_BookingPromotion_Promotion");
        });

        // Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.TransactionNo).HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");

            entity.HasOne(e => e.Booking)
                .WithMany(p => p.Payments)
                .HasForeignKey(e => e.BookingId)
                .HasConstraintName("FK_Payment_Booking");
        });

        // Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoice");
            entity.HasKey(e => e.InvoiceId);
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.BookingId).HasColumnName("BookingID");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");

            entity.HasIndex(e => e.BookingId, "UQ_Invoice_Booking").IsUnique();

            entity.HasOne(e => e.Booking)
                .WithOne(p => p.Invoice)
                .HasForeignKey<Invoice>(e => e.BookingId)
                .HasConstraintName("FK_Invoice_Booking");
        });

        // Invoice_Detail
        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.ToTable("Invoice_Detail");
            entity.HasKey(e => e.DetailId);
            entity.Property(e => e.DetailId).HasColumnName("DetailID");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.TicketId).HasColumnName("TicketID");

            entity.HasOne(e => e.Invoice)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(e => e.InvoiceId)
                .HasConstraintName("FK_InvoiceDetail_Invoice");

            entity.HasOne(e => e.Ticket)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(e => e.TicketId)
                .HasConstraintName("FK_InvoiceDetail_Ticket");
        });
    }
}
