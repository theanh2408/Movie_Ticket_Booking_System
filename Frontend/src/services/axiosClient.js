import axios from "axios";

// Đọc URL từ file .env (Hỗ trợ cả Vite lẫn CRA)
const baseURL = import.meta.env?.VITE_API_BASE_URL || process.env?.REACT_APP_API_BASE_URL || "http://localhost:5231/api";
const TOKEN_KEY = import.meta.env?.VITE_AUTH_TOKEN_KEY || "vibe_cinema_access_token";

const axiosClient = axios.create({
  baseURL: baseURL,
  headers: {
    "Content-Type": "application/json",
  },
});

// 1. Request Interceptor: Tự động đính kèm JWT Bearer Token và Idempotency-Key
axiosClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem(TOKEN_KEY);
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    // Tự động gắn Idempotency-Key dạng GUID khi tạo đơn đặt vé nếu chưa có
    if (config.method === 'post' && config.url?.includes('/bookings') && !config.headers['Idempotency-Key']) {
      try {
        config.headers['Idempotency-Key'] = crypto.randomUUID();
      } catch {
        // Fallback tạo GUID nếu crypto.randomUUID không khả dụng
        config.headers['Idempotency-Key'] = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
          const r = (Math.random() * 16) | 0;
          const v = c === 'x' ? r : (r & 0x3) | 0x8;
          return v.toString(16);
        });
      }
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// 2. Response Interceptor: Chuẩn hóa dữ liệu trả về theo cấu trúc { success, data, message } của Specs
axiosClient.interceptors.response.use(
  (response) => {
    return response.data; // Trả thẳng về object { success: true, data: ..., message: ... }
  },
  (error) => {
    // Nếu lỗi 401 (Hết hạn token hoặc chưa đăng nhập) -> Xóa token và thông báo
    if (error.response && error.response.status === 401) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem('vibe_cinema_user');
      localStorage.removeItem('Vibe Cinema_user');
      window.dispatchEvent(new Event('authChange'));
    }
    return Promise.reject(error.response ? error.response.data : error);
  }
);

export default axiosClient;
