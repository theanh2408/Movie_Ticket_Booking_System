# API Guide - Backend Admin

## Cách demo nhanh (Swagger)
1. Xóa database cũ (nếu có), chạy `Create_Movie_Ticket_Booking_System.sql`, rồi chạy tiếp `Thêm bảng Staff.sql` (tạo bảng Staff) trong SSMS. Không cần các file SQL cũ khác.
2. `dotnet run`, mở `http://localhost:<cổng>/swagger`.
3. Lần chạy đầu app tự tạo: tài khoản **admin@cinema.local / Admin@123**, 5 thể loại, 1 rạp mẫu.
4. Gọi `POST /api/auth/login` với tài khoản trên, copy giá trị `token`.
5. Bấm nút **Authorize** (góc phải trên Swagger), dán token, bấm Authorize.
6. Gọi `POST /api/movies` như cũ.

Lỗi luôn trả JSON `{ "message": "..." }`. Mã: 400 dữ liệu sai, 401 chưa đăng nhập, 403 không đủ quyền, 404 không tìm thấy, 409 xung đột/trùng.

## Phân trang và sắp xếp (áp dụng cho 10 danh sách)
Các API danh sách/tìm kiếm của **Accounts, Customers, Staff, Rooms, Showtimes, Movies, Bookings, Payments, Invoices, Promotions** nhận thêm:

| Tham số | Ý nghĩa |
|---|---|
| `page` | Trang, bắt đầu từ 1 (mặc định 1) |
| `pageSize` | Số dòng mỗi trang (mặc định 20, tối đa 100) |
| `sortBy` | Trường sắp xếp (bảng dưới). Bỏ trống = thứ tự mặc định |
| `sortDir` | `asc` hoặc `desc`. Có `sortBy` mà không có `sortDir` = tăng dần |

Kết quả trả về dạng `{ "items": [...], "page": 1, "pageSize": 20, "totalItems": 135, "totalPages": 7 }` (không còn là mảng trần). `sortBy` sai trả 400 kèm danh sách trường hợp lệ.

| Danh sách | `sortBy` hỗ trợ | Mặc định |
|---|---|---|
| Accounts / Customers / Staff | id, fullName, email, role, status | id giảm dần |
| Rooms | id, name, cinemaId, status | theo rạp rồi tên phòng |
| Showtimes | date, id, price, status | ngày + giờ giảm dần |
| Movies | id, title, releaseDate, duration, status | id giảm dần |
| Bookings | id, createdDate, totalAmount, status | id giảm dần |
| Payments | date, id, amount, status | ngày giảm dần |
| Invoices | id, createDate, totalPrice, status | id giảm dần |
| Promotions | id, code, startDate, endDate, status | id giảm dần |

Ví dụ: `GET /api/movies?page=2&pageSize=10&sortBy=title&sortDir=asc`

## Quy ước thời gian (dùng chung Admin và Customer)
- Mọi cột DateTime (CreatedDate, PaymentDate, CreateDate, khuyến mãi...) lưu và trả về theo **UTC** (có chữ `Z`, ví dụ `2026-11-20T03:15:00Z`). Frontend tự đổi sang giờ máy.
- Riêng **Showtime.Date + Start_hour** là **giờ địa phương của rạp (Việt Nam, UTC+7)**, không đổi sang UTC.
- Ngày `from` / `to` trong các API lọc và thống kê là **ngày giờ Việt Nam**, server tự đổi sang UTC khi truy vấn.
- Cả hệ thống không dùng `DateTime.Now`; chỉ dùng `AppClock.UtcNow` / `AppClock.LocalNow` (nằm trong `Common/AppClock.cs`).

## Phân quyền
| Vai trò | Phạm vi |
|---|---|
| Admin | Toàn chuỗi |
| Manager | Chỉ dữ liệu thuộc rạp mình (token chứa cinemaId), kể cả nhân viên: không xem/sửa/xóa được nhân viên của rạp khác (403) |
| Staff | Tài khoản Role = Staff, gắn với một rạp qua bảng Staff. Đăng nhập, xem/sửa hồ sơ của mình (`/api/profile`), xem phim |

## Danh sách endpoint theo chức năng
| Chức năng | Endpoint | Quyền |
|---|---|---|
| Đăng nhập | POST /api/auth/login | Công khai |
| Hồ sơ của tôi | GET, PUT /api/profile | Đã đăng nhập |
| 1. Tài khoản | GET/POST /api/accounts, GET/PUT/DELETE /api/accounts/{userId}, PUT /api/accounts/{userId}/role | Admin |
| 2. Khách hàng | GET/POST /api/customers, GET/PUT/DELETE /api/customers/{userId} | Admin, Manager |
| 3. Nhân viên | GET/POST /api/staff, GET/PUT/DELETE /api/staff/{userId} | Admin, Manager (rạp mình) |
| Rạp (quản trị) | GET /api/cinemas | Admin, Manager, Staff (Admin thấy tất cả, Manager/Staff thấy rạp của mình) |
| Rạp (công khai) | GET /api/cinemas/public (chỉ rạp Status = Opened) | Công khai, dùng cho Customer |
| Rạp (thêm/sửa) | GET /api/cinemas/{id}, POST /api/cinemas, PUT /api/cinemas/{id}, PATCH /api/cinemas/{id}/status (Opened/Closed) | Admin (GET theo id: Admin, Manager, Staff) |
| 4. Phòng chiếu | GET/POST /api/rooms, GET/PUT/DELETE /api/rooms/{id} (sức chứa = `seatCount`, tính từ số ghế) | Admin, Manager |
| 4. Ghế | GET/POST /api/rooms/{roomId}/seats, POST .../seats/bulk, GET .../seat-map, PUT/DELETE /api/seats/{seatId} | Admin, Manager |
| 5. Suất chiếu | GET/POST /api/showtimes, GET /api/showtimes/{id}, PUT /api/showtimes/{id} (roomId, basePrice, status Active/Inactive), PUT .../start-time, POST .../cancel | Admin, Manager |
| Sơ đồ ghế suất chiếu | GET /api/showtimes/{id}/seat-map, PUT /api/showtimes/{id}/seats/{seatId}/status | Admin, Manager |
| 8. Phim | GET (công khai), POST/PUT/DELETE /api/movies và PATCH /api/movies/{id}/status (đều chỉ Admin) | xem hàng |
| 8.7 Thể loại | GET (công khai), POST/PUT/DELETE /api/genres | Admin |
| 6. Đơn đặt vé | GET /api/bookings, GET /api/bookings/{id} | Admin, Manager |
| 6.7 Giao dịch | GET /api/payments, GET /api/payments/{id} | Admin, Manager |
| 9. Hóa đơn | GET /api/invoices, GET /api/invoices/{id}, GET /api/invoices/{id}/export?format=pdf\|xml, PUT /api/invoices/{id}/cancel (Admin) | Admin, Manager |
| 10. Khuyến mãi | GET/POST/PUT/DELETE /api/promotions, PATCH .../status (Admin), POST /api/promotions/validate (đã đăng nhập) | xem hàng |
| 11. Thống kê | GET /api/statistics/{revenue, tickets-sold, bookings, occupancy, revenue-by-movie, showtime-performance}, GET /api/statistics/export?type=... (CSV) | Admin, Manager |
| 6.9 Kiểm tra vé QR | POST /api/tickets/verify (chỉ xem), POST /api/tickets/use (xác nhận vào phòng); body `{ "token": "..." }` là mã QR của đơn hoặc mã vạch của vé | Admin, Manager, Staff (rạp mình) |
| Đặt vé tại quầy | POST /api/counter-bookings (showtimeId, seatIds, customerId?, promotionCode?, paymentMethod?) | Admin, Manager, Staff (rạp mình) |

## Chưa làm (thuộc luồng khách hàng / cần phối hợp nhóm)
Tạo đơn, chọn & giữ ghế, thanh toán online (VNPay/Momo...), hủy đơn của khách,
hoàn tiền, đánh giá phim, gửi thông báo khi đổi giờ chiếu.
