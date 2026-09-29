using System;
using System.Collections.Generic;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Invoice> Invoices { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<ShowtimeSeat> ShowtimeSeats { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=Pé\\SQLEXPRESS;Database=Movie_Ticket_Booking_System;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Account");

            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role).HasMaxLength(50);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("Cinema");

            entity.Property(e => e.CinemaId).HasColumnName("CinemaID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CinemaName).HasMaxLength(150);
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.ToTable("Genre");

            entity.Property(e => e.GenreId).HasColumnName("Genre_ID");
            entity.Property(e => e.GenreName).HasMaxLength(100);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("Invoice");

            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasColumnName("UserID");

            entity.HasOne(d => d.User).WithMany(p => p.Invoices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Invoice_Account");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("Movie");

            entity.Property(e => e.MovieId).HasColumnName("MovieID");
            entity.Property(e => e.AgeRating).HasMaxLength(20);
            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500)
                .HasColumnName("PosterURL");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .HasColumnName("TrailerURL");

            entity.HasMany(d => d.Genres).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieGenre",
                    r => r.HasOne<Genre>().WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenre_Genre"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieGenre_Movie"),
                    j =>
                    {
                        j.HasKey("MovieId", "GenreId");
                        j.ToTable("Movie_Genre");
                        j.IndexerProperty<int>("MovieId").HasColumnName("MovieID");
                        j.IndexerProperty<int>("GenreId").HasColumnName("Genre_ID");
                    });
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentID");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasMaxLength(50);
            entity.Property(e => e.TransactionNo).HasMaxLength(100);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Payments)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Payment_Invoice");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("Room");

            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.CinemaId).HasColumnName("CinemaID");
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Cinema).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CinemaId)
                .HasConstraintName("FK_Room_Cinema");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("Seat");

            entity.Property(e => e.SeatId).HasColumnName("SeatID");
            entity.Property(e => e.Column).HasMaxLength(20);
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.Row).HasMaxLength(20);

            entity.HasOne(d => d.Room).WithMany(p => p.Seats)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_Seat_Room");
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.ToTable("Showtime");

            entity.Property(e => e.ShowtimeId).HasColumnName("ShowtimeID");
            entity.Property(e => e.BasePrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MovieId).HasColumnName("MovieID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.StartHour).HasColumnName("Start_hour");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .HasConstraintName("FK_Showtime_Movie");

            entity.HasOne(d => d.Room).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_Showtime_Room");
        });

        modelBuilder.Entity<ShowtimeSeat>(entity =>
        {
            entity.ToTable("Showtime_Seat");

            entity.Property(e => e.ShowtimeSeatId).HasColumnName("Showtime_SeatID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SeatId).HasColumnName("SeatID");
            entity.Property(e => e.ShowtimeId).HasColumnName("ShowtimeID");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Seat).WithMany(p => p.ShowtimeSeats)
                .HasForeignKey(d => d.SeatId)
                .HasConstraintName("FK_ShowtimeSeat_Seat");

            entity.HasOne(d => d.Showtime).WithMany(p => p.ShowtimeSeats)
                .HasForeignKey(d => d.ShowtimeId)
                .HasConstraintName("FK_ShowtimeSeat_Showtime");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("Ticket");

            entity.HasIndex(e => e.ShowtimeSeatId, "UQ_Ticket_ShowtimeSeat").IsUnique();

            entity.Property(e => e.TicketId).HasColumnName("TicketID");
            entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ShowtimeSeatId).HasColumnName("Showtime_SeatID");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Invoice).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.InvoiceId)
                .HasConstraintName("FK_Ticket_Invoice");

            entity.HasOne(d => d.ShowtimeSeat).WithOne(p => p.Ticket)
                .HasForeignKey<Ticket>(d => d.ShowtimeSeatId)
                .HasConstraintName("FK_Ticket_ShowtimeSeat");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
