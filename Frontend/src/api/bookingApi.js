import axiosClient from '../services/axiosClient';

const bookingApi = {
  // Tạo đơn đặt vé và giữ ghế trong 10 phút
  createBooking: (customerId, data, idempotencyKey = null) => {
    const config = idempotencyKey ? { headers: { 'Idempotency-Key': idempotencyKey } } : {};
    return axiosClient.post(`/customers/${customerId}/bookings`, data, config);
  },

  // Lấy chi tiết đơn đặt vé
  getBookingDetail: (customerId, bookingId) => {
    return axiosClient.get(`/customers/${customerId}/bookings/${bookingId}`);
  },

  // Lấy lịch sử vé đã thanh toán
  getBookingHistory: (customerId) => {
    return axiosClient.get(`/customers/${customerId}/bookings/history`);
  },

  // Hủy vé trong vòng 30 phút sau khi thanh toán thành công
  cancelBooking: (customerId, bookingId) => {
    return axiosClient.post(`/customers/${customerId}/bookings/${bookingId}/cancel`);
  },

  // Áp dụng mã khuyến mãi cho đơn PENDING
  applyPromotion: (customerId, bookingId, code) => {
    return axiosClient.post(`/customers/${customerId}/bookings/${bookingId}/promotion`, { code });
  },

  // Tạo hoặc xuất hóa đơn cho đơn PAID
  createInvoice: (customerId, bookingId) => {
    return axiosClient.post(`/customers/${customerId}/bookings/${bookingId}/invoice`);
  },

  // Hoàn tất thanh toán mô phỏng (SUCCESS hoặc FAILED)
  completePaymentMock: (paymentAttemptId, result = 'SUCCESS') => {
    return axiosClient.post(`/payments/mock/${paymentAttemptId}/complete`, { result });
  },
};

export default bookingApi;
