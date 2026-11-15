# API Guide - Backend Admin

## Cách demo nhanh (Swagger)
1. Chạy `Database/SE-database-cap-nhat-lan-2.sql` trong SSMS (sau 2 file SQL cũ).
2. `dotnet run`, mở `http://localhost:<cổng>/swagger`.
3. Lần chạy đầu app tự tạo: tài khoản **admin@cinema.local / Admin@123**, 5 thể loại, 1 rạp mẫu.
4. Gọi `POST /api/auth/login` với tài khoản trên, copy giá trị `token`.
5. Bấm nút **Authorize** (góc phải trên Swagger), dán token, bấm Authorize.
6. Gọi `POST /api/movies` như cũ.

Lỗi luôn trả JSON `{ "message": "..." }`. Mã: 400 dữ liệu sai, 401 chưa đăng nhập, 403 không đủ quyền, 404 không tìm thấy, 409 xung đột/trùng.

## Phân quyền
| Vai trò | Phạm vi |
|---|---|
| Admin | Toàn chuỗi |
| Manager | Chỉ dữ liệu thuộc rạp mình (token chứa cinemaId) |
| Staff | Đăng nhập, xem/sửa hồ sơ của mình (`/api/profile`), xem phim |

## Danh sách endpoint theo chức năng
| Chức năng | Endpoint | Quyền |
|---|---|---|
| Đăng nhập | POST /api/auth/login | Công khai |
| Hồ sơ của tôi | GET, PUT /api/profile | Đã đăng nhập |
| 1. Tài khoản | GET/POST /api/accounts, GET/PUT/DELETE /api/accounts/{userId}, PUT /api/accounts/{userId}/role | Admin |
| 2. Khách hàng | GET/POST /api/customers, GET/PUT/DELETE /api/customers/{userId} | Admin, Manager |
| 3. Nhân viên | GET/POST /api/staff, GET/PUT/DELETE /api/staff/{userId} | Admin, Manager (rạp mình) |
| Rạp | GET /api/cinemas | Đã đăng nhập |
| 4. Phòng chiếu | GET/POST /api/rooms, GET/PUT/DELETE /api/rooms/{id} | Admin, Manager |
| 4. Ghế | GET/POST /api/rooms/{roomId}/seats, POST .../seats/bulk, GET .../seat-map, PUT/DELETE /api/seats/{seatId} | Admin, Manager |
| 5. Suất chiếu | GET/POST /api/showtimes, GET /api/showtimes/{id}, PUT .../start-time, POST .../cancel | Admin, Manager |
| Sơ đồ ghế suất chiếu | GET /api/showtimes/{id}/seat-map, PUT /api/showtimes/{id}/seats/{seatId}/status | Admin, Manager |
| 8. Phim | GET (công khai), POST/PUT/DELETE /api/movies (Admin), PATCH /api/movies/{id}/status (Admin, Manager) | xem hàng |
| 8.7 Thể loại | GET (công khai), POST/PUT/DELETE /api/genres | Admin |
| 6. Đơn đặt vé | GET /api/bookings, GET /api/bookings/{id} | Admin, Manager |
| 6.7 Giao dịch | GET /api/payments, GET /api/payments/{id} | Admin, Manager |
| 9. Hóa đơn | GET /api/invoices, GET /api/invoices/{id}, PUT /api/invoices/{id}/cancel (Admin) | Admin, Manager |
| 10. Khuyến mãi | GET/POST/PUT/DELETE /api/promotions, PATCH .../status (Admin), POST /api/promotions/validate (đã đăng nhập) | xem hàng |
| 11. Thống kê | GET /api/statistics/{revenue, tickets-sold, bookings, occupancy, revenue-by-movie, showtime-performance} | Admin, Manager |

## Chưa làm (thuộc luồng khách hàng / cần phối hợp nhóm)
Tạo đơn, chọn & giữ ghế, thanh toán online (VNPay/Momo...), hủy đơn của khách, QR check-in,
hoàn tiền, xuất PDF hóa đơn / báo cáo, đánh giá phim, gửi thông báo khi đổi giờ chiếu.
