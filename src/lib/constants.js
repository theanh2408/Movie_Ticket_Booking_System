// Các giá trị trạng thái dưới đây là ĐỀ XUẤT tạm để làm giao diện.
// Khi backend chốt enum chính thức, chỉ cần sửa tại file này.
export const MOVIE_STATUS = {
  Showing: { label: 'Đang chiếu', tone: 'ok' },
  ComingSoon: { label: 'Sắp chiếu', tone: 'info' },
  Hidden: { label: 'Ngừng hiển thị', tone: 'muted' },
};
export const ROOM_STATUS = {
  Active: { label: 'Hoạt động', tone: 'ok' },
  Maintenance: { label: 'Bảo trì', tone: 'warn' },
};
// Trạng thái suất chiếu lưu trong DB chỉ có Scheduled/Cancelled. Upcoming/Playing/Ended được suy ra từ giờ chiếu.
export const SHOWTIME_STATUS = {
  Upcoming: { label: 'Sắp chiếu', tone: 'info' },
  Playing: { label: 'Đang chiếu', tone: 'ok' },
  Ended: { label: 'Đã chiếu', tone: 'muted' },
  Cancelled: { label: 'Đã hủy', tone: 'danger' },
};
export const INVOICE_STATUS = {
  Pending: { label: 'Chờ thanh toán', tone: 'warn' },
  Paid: { label: 'Đã thanh toán', tone: 'ok' },
  Cancelled: { label: 'Đã hủy', tone: 'danger' },
};
export const TICKET_STATUS = {
  Booked: { label: 'Đã đặt', tone: 'ok' },
  Cancelled: { label: 'Đã hủy', tone: 'danger' },
};
export const PAYMENT_STATUS = {
  Pending: { label: 'Chờ xử lý', tone: 'warn' },
  Success: { label: 'Thành công', tone: 'ok' },
  Failed: { label: 'Thất bại', tone: 'danger' },
  Refunded: { label: 'Đã hoàn tiền', tone: 'muted' },
};
export const PAYMENT_METHODS = ['MoMo', 'VNPay', 'ZaloPay', 'Smartlink'];
export const AGE_RATINGS = [
  { value: 'P', label: 'P – Mọi lứa tuổi' },
  { value: 'K', label: 'K – Dưới 13 tuổi, có người lớn đi cùng' },
  { value: 'T13', label: 'T13 – Từ 13 tuổi' },
  { value: 'T16', label: 'T16 – Từ 16 tuổi' },
  { value: 'T18', label: 'T18 – Từ 18 tuổi' },
];

// Thời gian dọn phòng giữa hai suất (phút) – dùng khi kiểm tra trùng lịch.
export const CLEANING_MINUTES = 15;

export function showtimeState(st, now = new Date()) {
  if (st.Status === 'Cancelled') return 'Cancelled';
  const start = new Date(`${st.Date}T${st.Start_hour}:00`);
  const end = new Date(start.getTime() + (st.Duration || 0) * 60000);
  if (now < start) return 'Upcoming';
  if (now < end) return 'Playing';
  return 'Ended';
}
