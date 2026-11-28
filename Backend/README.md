# Backend Admin - Movie Ticket Booking (ASP.NET Core / .NET 10)

## 1. Database
Trong SSMS: xóa database `Movie_Ticket_Booking_System` cũ (nếu có), rồi chạy lần lượt:

    1. Create_Movie_Ticket_Booking_System.sql   (nằm cạnh thư mục Backend)
    2. Thêm bảng Staff.sql                      (tạo bảng Staff)
    3. Thêm cột Booking.ShowtimeID (lệnh bên dưới)

```sql
USE Movie_Ticket_Booking_System;
GO
IF COL_LENGTH('Booking', 'ShowtimeID') IS NULL
    ALTER TABLE Booking ADD ShowtimeID INT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Booking_Showtime')
    ALTER TABLE Booking ADD CONSTRAINT FK_Booking_Showtime FOREIGN KEY (ShowtimeID) REFERENCES Showtime(ShowtimeID);
GO
-- điền ShowtimeID cho các đơn cũ đã có vé
UPDATE b SET b.ShowtimeID = x.ShowtimeID
FROM Booking b
CROSS APPLY (SELECT TOP 1 ss.ShowtimeID FROM Ticket t JOIN Showtime_Seat ss ON ss.Showtime_SeatID = t.Showtime_SeatID
             WHERE t.Booking_ID = b.BookingID) x
WHERE b.ShowtimeID IS NULL;
GO
```

File này đã gồm toàn bộ cấu trúc, thay cho SE-database.sql và các bản cập nhật cũ.

## 2. Cấu hình
Sửa `Server=` trong `Backend/appsettings.json` cho đúng máy bạn (mặc định LocalDB).
Đổi `Jwt:Key` thành chuỗi bí mật riêng (>= 32 ký tự).

## 3. Chạy
    cd Backend
    dotnet restore
    dotnet run

Mở /swagger. Hướng dẫn demo xem API-GUIDE.md.

## Cấu trúc (Controller -> Service -> Repository -> Data)
Mỗi controller / service / repository nằm trong một file riêng, tên file trùng tên lớp
(interface nằm chung file với lớp cài đặt của nó).

    Backend/
      Controllers/   Movies, Genres, Showtimes, Cinemas, Rooms, Seats, Staff, Bookings, Customers, Promotions,
                     Auth, Profile, Accounts, CounterBookings, Tickets, Payments, Invoices, Statistics (+ "Controller")
      Services/      MovieService, GenreService, ShowtimeService, CinemaService, RoomService, SeatService, StaffService,
                     BookingService, CustomerService, PromotionService, AccountService, AuthService, TokenService,
                     CounterBookingService, TicketCheckService, PaymentService, InvoiceService, StatisticsService,
                     SeatMapBuilder, InvoiceDocument
      Repositories/  MovieRepository, GenreRepository, ShowtimeRepository, CinemaRepository, RoomRepository, SeatRepository,
                     BookingRepository, AccountRepository, PromotionRepository, PaymentRepository, InvoiceRepository,
                     StatisticsRepository, PagingExtensions, ScopeExtensions
      Data/          ApplicationDbContext, DbSeeder, UtcDateTimeConverter
      Models/        Movie, Cinema, Seat, Showtime, Ticket, Invoice, Account, Booking, Payment, Promotion, Constants
      DTOs/          MovieDtos, ShowtimeDtos, BookingDtos, PaymentDtos
      Common/        Exceptions, ExceptionMiddleware, CurrentUser, PasswordHasher, AppClock, DateRange, SwaggerExtensions

Common/ chứa phần dùng chung (xử lý lỗi, phân quyền, mật khẩu, giờ hệ thống, Swagger) - namespace Backend.Common.

## EF Core Migrations (kiểm soát version schema)
Database hiện được tạo bằng các file SQL, nên migration đầu tiên chỉ dùng để "ghi nhận" schema hiện tại.
Chạy trong thư mục Backend:

    dotnet tool install --global dotnet-ef      (chỉ cần 1 lần, nếu chưa có)
    dotnet ef migrations add InitialCreate

Lệnh trên tạo thư mục Migrations/ (commit thư mục này lên GitHub). Với database ĐÃ CÓ SẴN từ file SQL,
KHÔNG chạy `dotnet ef database update` (nó sẽ cố tạo lại bảng). Thay vào đó đánh dấu migration là đã áp dụng:

    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);
    INSERT INTO __EFMigrationsHistory VALUES (N'<tên-migration-vừa-tạo>', N'10.0.0');

Các thay đổi schema sau này: sửa Models rồi `dotnet ef migrations add <TenThayDoi>` và `dotnet ef database update`.
