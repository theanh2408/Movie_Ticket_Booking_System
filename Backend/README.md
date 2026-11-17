# Backend Admin - Movie Ticket Booking (ASP.NET Core / .NET 10)

## 1. Database
Trong SSMS: xóa database `Movie_Ticket_Booking_System` cũ (nếu có), rồi chạy **một file duy nhất**:

    Create_Movie_Ticket_Booking_System.sql   (nằm cạnh thư mục Backend)

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
    Backend/
      Controllers/   MoviesController, ShowtimesController, BookingsController, PaymentsController
      Services/      MovieService,     ShowtimeService,     BookingService,     PaymentService
      Repositories/  MovieRepository,  ShowtimeRepository,  BookingRepository,  PaymentRepository
      Data/          ApplicationDbContext
      Models/        Movie, Cinema, Seat, Showtime, Ticket, Invoice, Account, Booking, Payment, Promotion, Constants
      DTOs/          MovieDtos, ShowtimeDtos, BookingDtos, PaymentDtos

Mỗi file gom các lớp cùng nhóm nghiệp vụ:

| File | Gồm các lớp |
|---|---|
| Movies*      | Movies, Genres |
| Showtimes*   | Showtimes, Cinemas, Rooms, Seats, Staff |
| Bookings*    | Bookings, Customers, Promotions, Auth, Profile, Accounts |
| Payments*    | Payments, Invoices, Statistics |

(* áp dụng cho cả Controllers, Services, Repositories.) Các lớp tiện ích dùng chung
(xử lý lỗi, phân quyền, JWT, Swagger) nằm cuối file Program.cs, namespace Backend.Common.
