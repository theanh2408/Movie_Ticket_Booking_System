import axiosClient from '../services/axiosClient';

const showtimeApi = {
  getShowtimes: (params) => axiosClient.get('/showtimes', { params }),
  getShowtimeDetail: (showtimeId) => axiosClient.get(`/showtimes/${showtimeId}`),
  getShowtimeSeats: (showtimeId) => axiosClient.get(`/showtimes/${showtimeId}/seats`),
};

export default showtimeApi;
