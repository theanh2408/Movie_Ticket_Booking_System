import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';

export default function Home() {
  const [movies, setMovies] = useState([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState('NOW_SHOWING'); // NOW_SHOWING or UPCOMING

  useEffect(() => {
    fetchMovies();
  }, []);

  const fetchMovies = async () => {
    try {
      setLoading(true);
      const res = await axiosClient.get('/movies');
      if (res.success && res.data) {
        setMovies(res.data);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách phim:', err);
    } finally {
      setLoading(false);
    }
  };

  const filteredMovies = movies.filter((m) =>
    filter === 'NOW_SHOWING' ? m.status === 'NOW_SHOWING' : m.status === 'UPCOMING'
  );

  const heroMovie = movies.find((m) => m.status === 'NOW_SHOWING') || movies[0];

  const getRatingBadge = (rating) => {
    const colors = {
      P: 'bg-emerald-500/20 text-emerald-400 border-emerald-500/40',
      K: 'bg-blue-500/20 text-blue-400 border-blue-500/40',
      T13: 'bg-amber-500/20 text-amber-400 border-amber-500/40',
      T16: 'bg-orange-500/20 text-orange-400 border-orange-500/40',
      T18: 'bg-red-500/20 text-red-400 border-red-500/40',
    };
    return colors[rating] || 'bg-slate-700 text-slate-300 border-slate-600';
  };

  return (
    <div className="min-h-screen">
      {/* Hero Banner Section */}
      {heroMovie && (
        <section className="relative h-[480px] bg-gradient-to-r from-slate-950 via-slate-900/90 to-transparent flex items-center overflow-hidden border-b border-slate-800">
          <div className="absolute inset-0 z-0">
            <img
              src={heroMovie.posterUrl || 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1200'}
              alt={heroMovie.title}
              className="w-full h-full object-cover object-center opacity-30 filter blur-sm scale-105"
            />
            <div className="absolute inset-0 bg-gradient-to-t from-slate-900 via-slate-900/70 to-transparent" />
          </div>

          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10 w-full flex flex-col md:flex-row items-center justify-between gap-8 py-10">
            <div className="max-w-2xl space-y-4 text-center md:text-left">
              <span className="inline-block px-3 py-1 rounded-full text-xs font-semibold bg-red-600/30 text-red-400 border border-red-500/40">
                🔥 Phim Đang Hot Nhất Rạp
              </span>
              <h1 className="text-3xl md:text-5xl font-extrabold text-white tracking-tight">
                {heroMovie.title}
              </h1>
              <div className="flex items-center justify-center md:justify-start gap-3 text-sm text-slate-300">
                <span className={`px-2 py-0.5 rounded border text-xs font-bold ${getRatingBadge(heroMovie.ageRating)}`}>
                  {heroMovie.ageRating || 'P'}
                </span>
                <span>⏱️ {heroMovie.duration} phút</span>
                <span>•</span>
                <span>{heroMovie.genres?.map((g) => g.genreName).join(', ')}</span>
              </div>
              <p className="text-slate-300 line-clamp-3 text-sm md:text-base leading-relaxed">
                {heroMovie.description}
              </p>
              <div className="flex flex-wrap items-center justify-center md:justify-start gap-4 pt-2">
                <Link
                  to={`/movie/${heroMovie.movieId}`}
                  className="bg-red-600 hover:bg-red-500 text-white font-semibold px-6 py-2.5 rounded-lg shadow-lg shadow-red-600/40 flex items-center gap-2 transition"
                >
                  <span>🎟️</span> Đặt Vé Ngay
                </Link>
                {heroMovie.trailerUrl && (
                  <a
                    href={heroMovie.trailerUrl}
                    target="_blank"
                    rel="noreferrer"
                    className="bg-slate-800/90 hover:bg-slate-800 text-slate-200 border border-slate-700 px-5 py-2.5 rounded-lg transition flex items-center gap-2"
                  >
                    <span>▶️</span> Xem Trailer
                  </a>
                )}
              </div>
            </div>

            <div className="hidden md:block w-64 aspect-[2/3] rounded-xl overflow-hidden shadow-2xl border-2 border-slate-700 transform hover:scale-105 transition duration-300">
              <img
                src={heroMovie.posterUrl}
                alt={heroMovie.title}
                className="w-full h-full object-cover"
              />
            </div>
          </div>
        </section>
      )}

      {/* Main Movie Catalog */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-800 pb-4 mb-8">
          <div>
            <h2 className="text-2xl font-bold text-white tracking-tight flex items-center gap-2">
              <span>🍿</span> Danh Sách Phim
            </h2>
            <p className="text-slate-400 text-sm mt-1">Chọn phim yêu thích và đặt vé chỉ trong vài cú click</p>
          </div>

          <div className="flex items-center gap-2 bg-slate-800/90 p-1 rounded-lg border border-slate-700 self-start sm:self-auto">
            <button
              onClick={() => setFilter('NOW_SHOWING')}
              className={`px-4 py-1.5 rounded-md text-sm font-medium transition ${
                filter === 'NOW_SHOWING'
                  ? 'bg-red-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Đang Chiếu
            </button>
            <button
              onClick={() => setFilter('UPCOMING')}
              className={`px-4 py-1.5 rounded-md text-sm font-medium transition ${
                filter === 'UPCOMING'
                  ? 'bg-red-600 text-white shadow-sm'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Sắp Chiếu
            </button>
          </div>
        </div>

        {loading ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-6">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="animate-pulse bg-slate-800/60 rounded-xl aspect-[2/3]" />
            ))}
          </div>
        ) : filteredMovies.length === 0 ? (
          <div className="text-center py-20 bg-slate-800/30 rounded-xl border border-dashed border-slate-700">
            <p className="text-slate-400 text-lg">Không có phim nào thuộc danh mục này.</p>
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-6">
            {filteredMovies.map((movie) => (
              <div
                key={movie.movieId}
                className="group bg-slate-800/60 hover:bg-slate-800 border border-slate-700/60 hover:border-slate-600 rounded-xl overflow-hidden flex flex-col transition duration-300 hover:-translate-y-1 hover:shadow-xl"
              >
                <Link to={`/movie/${movie.movieId}`} className="relative aspect-[2/3] overflow-hidden bg-slate-900 block">
                  <img
                    src={movie.posterUrl || 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=500'}
                    alt={movie.title}
                    className="w-full h-full object-cover group-hover:scale-105 transition duration-300"
                    loading="lazy"
                  />
                  <div className="absolute top-2.5 left-2.5">
                    <span className={`px-2 py-0.5 rounded text-xs font-bold border backdrop-blur-md ${getRatingBadge(movie.ageRating)}`}>
                      {movie.ageRating || 'P'}
                    </span>
                  </div>
                </Link>

                <div className="p-4 flex-1 flex flex-col justify-between">
                  <div>
                    <div className="text-xs text-slate-400 mb-1 flex items-center justify-between">
                      <span>⏱️ {movie.duration} phút</span>
                      <span className="truncate max-w-[90px]">{movie.genres?.[0]?.genreName}</span>
                    </div>
                    <Link
                      to={`/movie/${movie.movieId}`}
                      className="font-bold text-white group-hover:text-red-400 transition line-clamp-1 text-base"
                      title={movie.title}
                    >
                      {movie.title}
                    </Link>
                  </div>

                  <div className="mt-4 pt-3 border-t border-slate-700/60 flex items-center gap-2">
                    <Link
                      to={`/movie/${movie.movieId}`}
                      className="w-full text-center bg-red-600/90 hover:bg-red-600 text-white font-medium text-xs py-2 rounded-md transition shadow-sm"
                    >
                      {movie.status === 'NOW_SHOWING' ? 'Đặt Vé' : 'Chi Tiết'}
                    </Link>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
