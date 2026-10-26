import React, { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { MOVIES, THEATERS, DATES } from '../../assets/mockData';
import { cn } from '../../lib/cn';

export const Showtime = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [selectedMovieId, setSelectedMovieId] = useState(id || MOVIES[0].id);
  const [selectedTheaterId, setSelectedTheaterId] = useState(THEATERS[0].id);
  const [selectedDate, setSelectedDate] = useState(DATES[0].label);

  const selectedMovie = MOVIES.find((m) => m.id === selectedMovieId) || MOVIES[0];
  const selectedTheater = THEATERS.find((t) => t.id === selectedTheaterId) || THEATERS[0];

  const nowShowingMovies = MOVIES.filter((m) => m.status === 'now-showing');

  const currentTheaterShowtimes = selectedMovie.theaters?.find(
    (t) => t.theaterId === selectedTheater.id
  )?.showtimes || [
    { showtimeId: 'st-general-1', time: '09:30', room: 'Cinema 1', format: '2D Phụ Đề', price: 95000 },
    { showtimeId: 'st-general-2', time: '13:00', room: 'Cinema 2', format: '2D Phụ Đề', price: 95000 },
    { showtimeId: 'st-general-3', time: '16:30', room: 'Cinema 1', format: '2D Phụ Đề', price: 95000 },
    { showtimeId: 'st-general-4', time: '19:45', room: 'Cinema 3', format: '2D Phụ Đề', price: 95000 },
    { showtimeId: 'st-general-5', time: '22:15', room: 'Cinema 2', format: '2D Phụ Đề', price: 95000 },
  ];

  const handleBook = (st) => {
    const bookingPayload = {
      movieId: selectedMovie.id,
      movieTitle: selectedMovie.title,
      moviePoster: selectedMovie.poster,
      theaterId: selectedTheater.id,
      theaterName: selectedTheater.name,
      theaterAddress: selectedTheater.address,
      showtimeId: st.showtimeId,
      time: st.time,
      room: st.room,
      format: st.format,
      date: selectedDate,
      price: st.price,
    };

    const user = localStorage.getItem('Vibe Cinema_user');
    if (!user) {
      navigate('/login', {
        state: {
          returnUrl: `/showtime/${st.showtimeId}/seats`,
          bookingData: bookingPayload,
          message: 'Vui lòng đăng nhập để tiếp tục chọn suất chiếu và đặt vé.',
        },
      });
      return;
    }

    navigate(`/showtime/${st.showtimeId}/seats`, {
      state: bookingPayload,
    });
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-10 space-y-8">
        {/* Header Title */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-white/5">
          <div>
            <div className="text-xs font-bold text-secondary uppercase tracking-wider">
              Tra Cứu Lịch Chiếu Toàn Hệ Thống
            </div>
            <h1 className="font-headline text-2xl lg:text-3xl font-bold text-on-surface mt-1">
              Lịch Chiếu Phim Tại Rạp Vibe Cinema
            </h1>
          </div>

          <div className="flex items-center gap-2 text-xs text-outline">
            <span className="material-symbols-outlined text-tertiary text-[18px]">theaters</span>
            <span>Cập nhật liên tục 24/7 theo từng cụm rạp</span>
          </div>
        </div>

        {/* 1. Pick Movie Selector */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            1. Chọn bộ phim đang chiếu:
          </label>
          <div className="flex items-center gap-3 overflow-x-auto pb-2 scrollbar-none">
            {nowShowingMovies.map((m) => (
              <button
                key={m.id}
                onClick={() => setSelectedMovieId(m.id)}
                className={cn(
                  'p-2.5 rounded-2xl border text-left shrink-0 w-48 sm:w-56 transition-all flex items-center gap-3 cursor-pointer',
                  selectedMovieId === m.id
                    ? 'bg-primary/15 border-primary text-white shadow-lg'
                    : 'bg-surface-low border-white/5 text-outline hover:bg-surface-container hover:text-on-surface'
                )}
              >
                <img
                  src={m.poster}
                  alt={m.title}
                  className="w-12 h-16 object-cover rounded-lg shrink-0"
                />
                <div className="min-w-0">
                  <div className="font-headline text-xs font-bold text-on-surface line-clamp-1">
                    {m.title}
                  </div>
                  <div className="text-[10px] text-secondary mt-0.5">{m.duration} phút</div>
                  <div className="text-[10px] text-outline truncate">{m.genre}</div>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* 2. Pick Theater Selector */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            2. Chọn cụm rạp:
          </label>
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {THEATERS.map((t) => (
              <button
                key={t.id}
                onClick={() => setSelectedTheaterId(t.id)}
                className={cn(
                  'px-4 py-2.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer flex items-center gap-2',
                  selectedTheaterId === t.id
                    ? 'bg-surface-high text-on-surface border border-white/20 shadow-md'
                    : 'bg-surface-low text-outline hover:bg-surface-container hover:text-on-surface'
                )}
              >
                <span className="material-symbols-outlined text-[#ffb95f] text-[16px]">location_on</span>
                <span>{t.name}</span>
              </button>
            ))}
          </div>
        </div>

        {/* 3. Pick Date Selector */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            3. Chọn ngày chiếu:
          </label>
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {DATES.slice(0, 5).map((d) => (
              <button
                key={d.id}
                onClick={() => setSelectedDate(d.label)}
                className={cn(
                  'px-4 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer',
                  selectedDate === d.label
                    ? 'bg-primary text-white shadow-md'
                    : 'bg-surface-low text-outline hover:bg-surface-container hover:text-on-surface'
                )}
              >
                {d.label}
              </button>
            ))}
          </div>
        </div>

        {/* 4. Showtimes Matrix */}
        <div className="p-6 rounded-2xl bg-surface-low border border-white/5 space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-white/5">
            <div className="flex items-center gap-4">
              <img
                src={selectedMovie.poster}
                alt={selectedMovie.title}
                className="w-14 aspect-[2/3] object-cover rounded-xl shadow-md shrink-0"
              />
              <div>
                <h3 className="font-headline text-lg font-bold text-on-surface">
                  {selectedMovie.title}
                </h3>
                <p className="text-xs text-outline mt-0.5">
                  {selectedTheater.name} • {selectedDate}
                </p>
              </div>
            </div>

            <Link
              to={`/movie/${selectedMovie.id}`}
              className="px-4 py-2 rounded-xl bg-surface-high hover:bg-surface-highest text-xs font-semibold text-on-surface self-start sm:self-auto transition-colors"
            >
              Chi tiết phim
            </Link>
          </div>

          <div>
            <label className="text-xs font-semibold text-outline uppercase tracking-wider block mb-3">
              Suất chiếu sẵn sàng (Bấm vào giờ chiếu để chọn ghế):
            </label>
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-3">
              {currentTheaterShowtimes.map((st) => (
                <button
                  key={st.showtimeId}
                  onClick={() => handleBook(st)}
                  className="p-4 rounded-xl bg-surface hover:bg-primary/20 border border-white/5 hover:border-primary text-center transition-all cursor-pointer group shadow-sm"
                >
                  <div className="font-headline text-xl font-bold text-on-surface group-hover:text-primary-light">
                    {st.time}
                  </div>
                  <div className="text-[10px] text-outline mt-1">{st.room}</div>
                  <div className="text-xs text-tertiary font-bold mt-1.5">
                    {st.price.toLocaleString('vi-VN')}đ / vé
                  </div>
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
    </MainLayout>
  );
};
export default Showtime;
