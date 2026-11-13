import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';

export default function MovieDetail() {
  const { id } = useParams();
  const [movie, setMovie] = useState(null);
  const [showtimes, setShowtimes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [selectedDate, setSelectedDate] = useState(() => {
    return new Date().toISOString().split('T')[0];
  });

  // Tạo danh sách 3 ngày chiếu: Hôm nay, Ngày mai, Ngày kia
  const dates = [0, 1, 2].map((offset) => {
    const d = new Date();
    d.setDate(d.getDate() + offset);
    return {
      iso: d.toISOString().split('T')[0],
      dayName: offset === 0 ? 'Hôm nay' : offset === 1 ? 'Ngày mai' : `Thứ ${d.getDay() + 1 || 'CN'}`,
      dateStr: `${d.getDate()}/${d.getMonth() + 1}`,
    };
  });

  useEffect(() => {
    fetchMovieDetail();
  }, [id]);

  useEffect(() => {
    if (id) {
      fetchShowtimes();
    }
  }, [id, selectedDate]);

  const fetchMovieDetail = async () => {
    try {
      setLoading(true);
      const res = await axiosClient.get(`/movies/${id}`);
      if (res.success && res.data) {
        setMovie(res.data);
      }
    } catch (err) {
      console.error('Lỗi tải chi tiết phim:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchShowtimes = async () => {
    try {
      const res = await axiosClient.get(`/showtimes?movieId=${id}&date=${selectedDate}&pageSize=50`);
      if (res.success && res.data?.items) {
        setShowtimes(res.data.items);
      } else {
        setShowtimes([]);
      }
    } catch (err) {
      console.error('Lỗi tải suất chiếu:', err);
      setShowtimes([]);
    }
  };

  // Nhóm suất chiếu theo rạp (cinemaName)
  const groupedShowtimes = showtimes.reduce((acc, st) => {
    const cinema = st.cinemaName || 'Rạp Chiếu Phim';
    if (!acc[cinema]) acc[cinema] = [];
    acc[cinema].push(st);
    return acc;
  }, {});

  const getYoutubeEmbedUrl = (url) => {
    if (!url) return null;
    const match = url.match(/(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w-]{11})/);
    return match ? `https://www.youtube.com/embed/${match[1]}` : null;
  };

  if (loading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center text-slate-400">
        <div className="animate-spin text-4xl mb-4">⌛</div>
        <p>Đang tải thông tin phim...</p>
      </div>
    );
  }

  if (!movie) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-20 text-center text-slate-400">
        <p className="text-xl font-semibold mb-4">Không tìm thấy thông tin phim.</p>
        <Link to="/" className="text-red-500 hover:underline">Quay lại trang chủ</Link>
      </div>
    );
  }

  const embedUrl = getYoutubeEmbedUrl(movie.trailerUrl);

  return (
    <div className="min-h-screen pb-20">
      {/* Banner / Poster Header */}
      <div className="relative bg-slate-950 py-12 border-b border-slate-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col md:flex-row gap-8 items-start">
          <div className="w-60 sm:w-72 aspect-[2/3] rounded-xl overflow-hidden shadow-2xl border-2 border-slate-700 shrink-0 mx-auto md:mx-0">
            <img
              src={movie.posterUrl || 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=600'}
              alt={movie.title}
              className="w-full h-full object-cover"
            />
          </div>

          <div className="flex-1 space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <span className="px-2.5 py-0.5 rounded text-xs font-bold bg-red-600/20 text-red-400 border border-red-500/30">
                {movie.ageRating || 'P'}
              </span>
              <span className="text-sm text-slate-400">⏱️ {movie.duration} phút</span>
              <span className="text-sm text-slate-400">• Khởi chiếu: {movie.releaseDate || 'Đang cập nhật'}</span>
            </div>

            <h1 className="text-3xl md:text-5xl font-black text-white">{movie.title}</h1>

            <div className="flex flex-wrap gap-2 pt-1">
              {movie.genres?.map((g) => (
                <span key={g.genreId} className="px-3 py-1 rounded-full text-xs font-medium bg-slate-800 text-slate-300 border border-slate-700">
                  {g.genreName}
                </span>
              ))}
            </div>

            <div className="pt-2">
              <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider mb-1">Nội dung phim</h3>
              <p className="text-slate-300 text-sm md:text-base leading-relaxed">{movie.description}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Trailer & Showtimes Section */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 mt-12 grid grid-cols-1 lg:grid-cols-3 gap-12">
        {/* Left Column: Showtime Picker (2 Cols) */}
        <div className="lg:col-span-2 space-y-8">
          <div>
            <h2 className="text-2xl font-bold text-white flex items-center gap-2 mb-4">
              <span>🎟️</span> Lịch Chiếu & Suất Chiếu
            </h2>

            {/* Date selection bar */}
            <div className="flex gap-3 overflow-x-auto pb-2">
              {dates.map((d) => (
                <button
                  key={d.iso}
                  onClick={() => setSelectedDate(d.iso)}
                  className={`px-5 py-3 rounded-xl border text-center font-medium transition shrink-0 ${
                    selectedDate === d.iso
                      ? 'bg-red-600 border-red-500 text-white shadow-lg shadow-red-600/30'
                      : 'bg-slate-800/80 border-slate-700 text-slate-300 hover:bg-slate-800'
                  }`}
                >
                  <div className="text-xs opacity-80">{d.dayName}</div>
                  <div className="text-lg font-bold">{d.dateStr}</div>
                </button>
              ))}
            </div>
          </div>

          {/* Grouped Showtimes */}
          {Object.keys(groupedShowtimes).length === 0 ? (
            <div className="p-8 text-center bg-slate-800/30 border border-dashed border-slate-700 rounded-xl">
              <p className="text-slate-400">Không có suất chiếu nào cho ngày đã chọn.</p>
              <p className="text-xs text-slate-500 mt-1">Vui lòng thử chọn một ngày khác phía trên.</p>
            </div>
          ) : (
            <div className="space-y-6">
              {Object.entries(groupedShowtimes).map(([cinemaName, list]) => (
                <div key={cinemaName} className="bg-slate-800/50 border border-slate-700/80 rounded-xl p-5">
                  <h3 className="font-bold text-lg text-amber-400 flex items-center gap-2 mb-4">
                    <span>🏢</span> {cinemaName}
                  </h3>
                  <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
                    {list.map((st) => {
                      const startTime = st.startTime
                        ? new Date(st.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                        : '10:00';
                      return (
                        <Link
                          key={st.showtimeId}
                          to={`/showtime/${st.showtimeId}/seats`}
                          className="group bg-slate-900/90 hover:bg-red-600 border border-slate-700 hover:border-red-500 p-3 rounded-lg text-center transition shadow-sm"
                        >
                          <div className="text-lg font-bold text-white group-hover:text-white">
                            {startTime}
                          </div>
                          <div className="text-xs text-slate-400 group-hover:text-red-100 mt-0.5 truncate">
                            {st.roomName || 'Phòng 01'}
                          </div>
                          <div className="text-xs font-semibold text-amber-400 group-hover:text-white mt-1">
                            {st.basePrice ? `${st.basePrice.toLocaleString()} đ` : '85.000 đ'}
                          </div>
                        </Link>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Right Column: Trailer */}
        <div className="space-y-4">
          <h2 className="text-xl font-bold text-white flex items-center gap-2">
            <span>🎬</span> Trailer Phim
          </h2>
          {embedUrl ? (
            <div className="aspect-video rounded-xl overflow-hidden border border-slate-700 shadow-xl bg-slate-900">
              <iframe
                src={embedUrl}
                title={movie.title}
                className="w-full h-full"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          ) : (
            <div className="aspect-video rounded-xl bg-slate-800 flex items-center justify-center text-slate-500 border border-slate-700">
              Chưa có trailer nhúng
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
