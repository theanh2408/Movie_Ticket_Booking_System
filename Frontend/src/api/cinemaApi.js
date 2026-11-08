import axiosClient from '../services/axiosClient';

const cinemaApi = {
  getCinemas: () => axiosClient.get('/cinemas'),
  getCinemaById: (id) => axiosClient.get(`/cinemas/${id}`),
  getCinemaRooms: (id) => axiosClient.get(`/cinemas/${id}/rooms`),
};

export default cinemaApi;
