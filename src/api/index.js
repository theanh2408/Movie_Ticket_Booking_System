// Điểm vào duy nhất của tầng API. Giao diện chỉ import `api` từ đây.
// VITE_USE_MOCK=true  -> dữ liệu giả (mockApi.js)
// VITE_USE_MOCK=false -> gọi backend thật, các endpoint dưới đây cũng là "hợp đồng API" đề xuất cho backend.
import { http } from '../lib/http';
import { mockApi } from './mockApi';

const realApi = {
  auth: { login: (body) => http.post('/auth/login', body) }, // -> { token }
  movies: {
    list: (params) => http.get('/admin/movies', params),
    get: (id) => http.get(`/admin/movies/${id}`),
    create: (body) => http.post('/admin/movies', body),
    update: (id, body) => http.put(`/admin/movies/${id}`, body),
    setStatus: (id, status) => http.patch(`/admin/movies/${id}/status`, { status }),
  },
  genres: { list: () => http.get('/admin/genres') },
  rooms: {
    cinema: () => http.get('/admin/cinema'),
    list: () => http.get('/admin/rooms'),
    get: (id) => http.get(`/admin/rooms/${id}`), // kèm Seats[{..., Locked}]
    create: (body) => http.post('/admin/rooms', body),
    update: (id, body) => http.put(`/admin/rooms/${id}`, body),
    saveSeats: (id, seats) => http.put(`/admin/rooms/${id}/seats`, { seats }),
  },
  showtimes: {
    list: (params) => http.get('/admin/showtimes', params),
    conflicts: (body, excludeId) => http.post('/admin/showtimes/check-conflicts', { ...body, excludeId }),
    seatMap: (id) => http.get(`/admin/showtimes/${id}/seats`),
    create: (body) => http.post('/admin/showtimes', body),
    update: (id, body) => http.put(`/admin/showtimes/${id}`, body),
    cancel: (id) => http.post(`/admin/showtimes/${id}/cancel`),
  },
  bookings: {
    list: (params) => http.get('/admin/bookings', params),
    get: (id) => http.get(`/admin/bookings/${id}`),
    cancelTickets: (id, ticketIds) => http.post(`/admin/bookings/${id}/cancel-tickets`, { ticketIds }),
    changeSeat: (id, ticketId, showtimeSeatId) => http.post(`/admin/bookings/${id}/tickets/${ticketId}/change-seat`, { showtimeSeatId }),
  },
  payments: {
    list: (params) => http.get('/admin/payments', params),
    updateStatus: (id, status) => http.patch(`/admin/payments/${id}/status`, { status }),
  },
  reports: { revenue: (params) => http.get('/admin/reports/revenue', params) },
};

export const USE_MOCK = import.meta.env.VITE_USE_MOCK !== 'false';
export const api = USE_MOCK ? mockApi : realApi;
