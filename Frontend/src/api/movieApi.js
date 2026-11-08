import axiosClient from '../services/axiosClient';

const movieApi = {
  getMovies: (params) => axiosClient.get('/movies', { params }),
  getMovieById: (movieId) => axiosClient.get(`/movies/${movieId}`),
  getGenres: () => axiosClient.get('/genres'),
};

export default movieApi;
