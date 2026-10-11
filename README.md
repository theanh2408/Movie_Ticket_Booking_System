# Quản trị rạp – Frontend Admin (React + JWT)

Frontend trang quản trị cho hệ thống đặt vé xem phim. Làm theo SRS (UC11–UC14) và ERD, thêm **báo cáo doanh thu**, **quản lý thanh toán** và **hủy/đổi ghế** theo yêu cầu của nhóm.

## Chạy thử

```bash
npm install
npm run dev        # http://localhost:5173
```

Mặc định chạy bằng **dữ liệu giả** (không cần backend). Đăng nhập: `admin@cinema.vn` / `admin123`.
(Tài khoản khách `minhanh@mail.com` / `customer123` sẽ bị từ chối vì không phải Admin.)

## Chuyển sang backend thật

1. Sửa `.env`: `VITE_USE_MOCK=false`, và `VITE_PROXY_TARGET` (trong lúc dev) trỏ về backend.
2. Các endpoint frontend gọi nằm trong `src/api/index.js` (`realApi`). Đây cũng là đề xuất hợp đồng API cho backend.

## JWT

- `POST /auth/login` trả `{ token }`. Token cần có claims: `sub` (UserID), `name`, `email`, `role`, `exp`.
- Frontend chỉ đọc claims để hiện tên, kiểm tra `role === 'Admin'` và hạn dùng; **backend vẫn phải kiểm tra chữ ký và quyền ở mọi API `/admin/*`** (BR10).
- Mọi request tự gắn `Authorization: Bearer <token>`. Gặp `401` hoặc token hết hạn thì tự đăng xuất về trang đăng nhập.
- Token đang lưu ở `localStorage` cho đơn giản. Nếu muốn an toàn hơn trước XSS, backend nên cấp cookie `HttpOnly` và bỏ phần lưu token ở `src/lib/token.js`.

## Cấu trúc

```
src/
  api/        index.js (chọn mock/thật), mockApi.js (nghiệp vụ giả), mockDb.js (dữ liệu giả theo ERD)
  context/    AuthContext (JWT), ToastContext
  components/ Layout, ProtectedRoute, ui.jsx (Modal, Field, Badge, SeatGrid...)
  lib/        constants.js (TRẠNG THÁI – sửa ở đây khi backend chốt), format.js, jwt.js, http.js
  pages/      Dashboard, Movies, Rooms, Showtimes, Bookings, BookingDetail, Payments, Login
```

## Các quyết định đã chốt với nhóm

| Mục | Cách làm |
|---|---|
| Admin hủy/sửa vé | Hủy một hoặc nhiều vé (giải phóng ghế) và **đổi ghế** trong cùng suất. Chỉ khi suất chưa bắt đầu |
| Doanh thu | Trang Tổng quan: theo ngày, top phim, theo phương thức; tính từ thanh toán thành công |
| Thanh toán | Xác nhận / đánh dấu thất bại giao dịch đang chờ; hoàn tiền cho hóa đơn đã hủy |
| Cinema | Chưa có màn hình quản lý (chỉ một rạp) |
| Bộ lọc đặt vé | Mã đặt vé / tên / SĐT / email, phim, ngày chiếu, trạng thái |
| Giờ kết thúc | Tính từ `Start_hour` + `Movie.Duration`; kiểm tra trùng giờ có thêm 15 phút dọn phòng |

## Trạng thái đề xuất (mock, sửa trong `src/lib/constants.js`)

- Movie: `Showing`, `ComingSoon`, `Hidden`
- Room: `Active`, `Maintenance`
- Showtime (lưu): `Scheduled`, `Cancelled` (Sắp chiếu / Đang chiếu / Đã chiếu được suy ra từ giờ)
- Invoice: `Pending`, `Paid`, `Cancelled`
- Ticket: `Booked`, `Cancelled`
- Payment: `Pending`, `Success`, `Failed`, `Refunded`
- Showtime_Seat: `Available`, `Held`, `Booked`

## Quy tắc nghiệp vụ đã thể hiện ở giao diện

- Phim, phòng, suất chiếu đã có vé không bị xóa. Phim chỉ ngừng hiển thị, và bị từ chối nếu còn suất sắp tới đã có người đặt (BR09).
- Suất chiếu trùng giờ trong cùng phòng: báo ngay trong form và liệt kê các suất xung đột (BR08).
- Suất đã có vé chỉ được đổi giá. Ghế đã có vé trong sơ đồ phòng bị khóa, không bỏ được.
- Hóa đơn đã hủy chưa hoàn tiền sẽ hiện ở bộ lọc “Cần hoàn tiền”.

## Việc nên làm tiếp

- Nối API thật và chốt enum trạng thái với backend.
- Thêm kiểm thử (Vitest + Testing Library) cho form và luồng hủy vé.
- Quản lý thể loại phim (hiện chỉ chọn từ danh sách có sẵn).
