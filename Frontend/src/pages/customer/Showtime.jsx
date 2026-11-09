import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { useMovies } from '../../hooks/useMovies';
import cinemaApi from '../../api/cinemaApi';
import showtimeApi from '../../api/showtimeApi';
import { cn } from '../../lib/cn';

// Tạo danh sách 5 ngày liên tiếp
const generateNextDates = () => {
  const dates = [];
  const today = new Date();

  for (let i = 0; i < 5; i++) {
    const d = new Date(today);
    d.setDate(today.getDate() + i);

    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const dateStr = `${year}-${month}-${day}`;

    let label = `${day}/${month}`;
    if (i === 0) label = `Hôm nay (${day}/${month})`;
    else if (i === 1) label = `Ngày mai (${day}/${month})`;
    else {
      const weekdays = ['Chủ Nhật', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy'];
      label = `${weekdays[d.getDay()]} (${day}/${month})`;
    }

    dates.push({ id: dateStr, label });
  }

  return dates;
};

export const Showtime = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  // 1. Lấy danh sách phim từ API
  const { items: allMovies, loading: loadingMovies } = useMovies();

  const [selectedMovieId, setSelectedMovieId] = useState(id ? parseInt(id, 10) : null);

  // 2. Lấy danh sách rạp từ API
  const [cinemas, setCinemas] = useState([]);
  const [selectedCinemaId, setSelectedCinemaId] = useState(null);

  // 3. Danh sách ngày
  const dateList = useMemo(() => generateNextDates(), []);
  const [selectedDate, setSelectedDate] = useState(dateList[0]);

  // 4. Danh sách suất chiếu theo phim, rạp, ngày
  const [showtimes, setShowtimes] = useState([]);
  const [loadingShowtimes, setLoadingShowtimes] = useState(false);
  const [showtimeError, setShowtimeError] = useState(null);

  // Tự động chọn phim đầu tiên khi danh sách phim tải xong nếu chưa có lựa chọn
  useEffect(() => {
    if (allMovies.length > 0 && !selectedMovieId) {
      const target = id ? parseInt(id, 10) : allMovies[0].movieId;
      setSelectedMovieId(target);
    }
  }, [allMovies, id, selectedMovieId]);

  // Tải danh sách rạp
  useEffect(() => {
    const fetchCinemas = async () => {
      try {
        const res = await cinemaApi.getCinemas();
        if (res && res.data && res.data.length > 0) {
          setCinemas(res.data);
          setSelectedCinemaId(res.data[0].cinemaId);
        }
      } catch (err) {
        console.error('Lỗi khi tải danh sách rạp:', err);
      }
    };
    fetchCinemas();
  }, []);

  const selectedMovie = useMemo(
    () => allMovies.find((m) => m.movieId === selectedMovieId) || allMovies[0],
    [allMovies, selectedMovieId]
  );

  const selectedCinema = useMemo(
    () => cinemas.find((c) => c.cinemaId === selectedCinemaId) || cinemas[0],
    [cinemas, selectedCinemaId]
  );

  // Tải danh sách suất chiếu
  const fetchShowtimes = useCallback(async () => {
    if (!selectedMovieId || !selectedCinemaId || !selectedDate) return;
    setLoadingShowtimes(true);
    setShowtimeError(null);
    try {
      const params = {
        movieId: selectedMovieId,
        cinemaId: selectedCinemaId,
        date: selectedDate.id,
      };
      const res = await showtimeApi.getShowtimes(params);
      if (res && res.data) {
        setShowtimes(Array.isArray(res.data) ? res.data : (res.data?.items || []));
      } else {
        setShowtimes([]);
      }
    } catch (err) {
      console.error('Lỗi khi tải suất chiếu:', err);
      setShowtimeError('Không thể tải danh sách suất chiếu.');
      setShowtimes([]);
    } finally {
      setLoadingShowtimes(false);
    }
  }, [selectedMovieId, selectedCinemaId, selectedDate]);

  useEffect(() => {
    fetchShowtimes();
  }, [fetchShowtimes]);

  const handleBook = (st) => {
    if (!selectedMovie || !selectedCinema) return;

    const bookingPayload = {
      movieId: selectedMovie.movieId,
      movieTitle: selectedMovie.title,
      moviePoster: selectedMovie.poster,
      theaterId: selectedCinema.cinemaId,
      theaterName: selectedCinema.cinemaName,
      theaterAddress: selectedCinema.address,
      showtimeId: st.showtimeId,
      time: st.startTime || st.startHour,
      room: st.roomName || 'Phòng chiếu',
      format: '2D Kỹ thuật số',
      date: selectedDate.label,
      price: st.basePrice || 85000,
    };

    const token =
      localStorage.getItem(import.meta.env?.VITE_AUTH_TOKEN_KEY || 'vibe_cinema_access_token') ||
      localStorage.getItem('Vibe Cinema_user') ||
      localStorage.getItem('vibe_cinema_user');

    if (!token) {
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
            1. Chọn bộ phim:
          </label>

          {loadingMovies ? (
            <div className="flex gap-3 overflow-x-auto pb-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="w-52 h-20 rounded-2xl bg-surface-low border border-white/5 animate-pulse" />
              ))}
            </div>
          ) : (
            <div className="flex items-center gap-3 overflow-x-auto pb-2 scrollbar-none">
              {allMovies.map((m) => (
                <button
                  key={m.movieId}
                  onClick={() => setSelectedMovieId(m.movieId)}
                  className={cn(
                    'p-2.5 rounded-2xl border text-left shrink-0 w-48 sm:w-56 transition-all flex items-center gap-3 cursor-pointer',
                    selectedMovieId === m.movieId
                      ? 'bg-primary/15 border-primary text-white shadow-lg'
                      : 'bg-surface-low border-white/5 text-outline hover:bg-surface-container hover:text-on-surface'
                  )}
                >
                  <img
                    src={m.poster}
                    alt={m.title}
                    className="w-10 h-14 object-cover rounded-lg shrink-0 shadow-md"
                  />
                  <div className="overflow-hidden">
                    <span className="font-headline text-xs font-bold truncate block text-on-surface">
                      {m.title}
                    </span>
                    <span className="text-[11px] text-outline block mt-0.5">
                      {m.duration} phút
                    </span>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* 2. Pick Cinema Branch */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            2. Chọn cụm rạp xem phim:
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
            {cinemas.map((t) => (
              <button
                key={t.cinemaId}
                onClick={() => setSelectedCinemaId(t.cinemaId)}
                className={cn(
                  'p-3.5 rounded-2xl border text-left transition-all flex flex-col justify-between cursor-pointer',
                  selectedCinemaId === t.cinemaId
                    ? 'bg-primary/15 border-primary text-white shadow-md'
                    : 'bg-surface-low border-white/5 text-outline hover:bg-surface-container hover:text-on-surface'
                )}
              >
                <div>
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-headline text-sm font-bold text-on-surface">
                      {t.cinemaName}
                    </span>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-surface-high text-secondary font-bold">
                      {t.status || 'ACTIVE'}
                    </span>
                  </div>
                  <span className="text-xs text-outline line-clamp-1">{t.address}</span>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* 3. Pick Date */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            3. Chọn ngày xem:
          </label>
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {dateList.map((d) => (
              <button
                key={d.id}
                onClick={() => setSelectedDate(d)}
                className={cn(
                  'px-4 py-2.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer',
                  selectedDate.id === d.id
                    ? 'bg-primary text-white shadow-glow-primary'
                    : 'bg-surface-low text-outline hover:text-on-surface hover:bg-surface-high'
                )}
              >
                {d.label}
              </button>
            ))}
          </div>
        </div>

        {/* 4. Display Matched Showtimes */}
        <div className="p-6 rounded-3xl bg-surface-low border border-white/5 space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-white/5">
            <div className="flex items-center gap-3">
              {selectedMovie && (
                <img
                  src={selectedMovie.poster}
                  alt={selectedMovie.title}
                  className="w-12 h-16 object-cover rounded-xl shadow-md hidden sm:block"
                />
              )}
              <div>
                <h2 className="font-headline text-base sm:text-lg font-bold text-on-surface">
                  {selectedMovie ? selectedMovie.title : 'Đang chọn phim'}
                </h2>
                <div className="flex items-center gap-2 text-xs text-outline mt-0.5">
                  <span className="text-secondary font-semibold">
                    {selectedCinema ? selectedCinema.cinemaName : ''}
                  </span>
                  <span>•</span>
                  <span>{selectedDate.label}</span>
                </div>
              </div>
            </div>

            <span className="text-xs text-outline">
              Định dạng: 2D Kỹ Thuật Số Chuẩn Quốc Tế
            </span>
          </div>

          {/* Loading state */}
          {loadingShowtimes && (
            <div className="py-16 text-center space-y-3">
              <div className="size-8 border-3 border-primary border-t-transparent rounded-full animate-spin mx-auto" />
              <p className="text-xs text-outline">Đang tìm kiếm suất chiếu phù hợp...</p>
            </div>
          )}

          {/* Error state */}
          {!loadingShowtimes && showtimeError && (
            <div className="py-12 text-center space-y-2">
              <span className="material-symbols-outlined text-primary text-[40px]">error</span>
              <p className="text-xs text-primary">{showtimeError}</p>
              <button
                onClick={() => fetchShowtimes()}
                className="px-4 py-2 rounded-xl bg-surface-high text-xs font-semibold hover:bg-surface-highest text-on-surface"
              >
                Thử lại
              </button>
            </div>
          )}

          {/* Empty state */}
          {!loadingShowtimes && !showtimeError && showtimes.length === 0 && (
            <div className="py-16 text-center space-y-2">
              <span className="material-symbols-outlined text-[48px] text-outline">event_busy</span>
              <h3 className="text-sm font-semibold text-on-surface">
                Không có suất chiếu phù hợp
              </h3>
              <p className="text-xs text-outline max-w-sm mx-auto">
                Hiện tại cụm rạp chưa có lịch chiếu cho phim này vào ngày đã chọn. Quý khách vui lòng chọn ngày khác hoặc cụm rạp khác.
              </p>
            </div>
          )}

          {/* Showtimes Grid */}
          {!loadingShowtimes && !showtimeError && showtimes.length > 0 && (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
              {showtimes.map((st) => (
                <button
                  key={st.showtimeId}
                  onClick={() => handleBook(st)}
                  className="p-3.5 rounded-2xl bg-surface-container border border-white/5 hover:border-primary/50 hover:bg-surface-high transition-all flex flex-col items-center justify-center gap-1 group cursor-pointer shadow-md hover:scale-105"
                >
                  <span className="font-headline text-lg font-bold text-on-surface group-hover:text-primary transition-colors">
                    {st.startTime || st.startHour}
                  </span>
                  <span className="text-[11px] text-outline line-clamp-1">
                    {st.roomName || 'Phòng chiếu'}
                  </span>
                  <span className="text-[11px] font-bold text-secondary">
                    {(st.basePrice || 85000).toLocaleString('vi-VN')}đ
                  </span>
                </button>
              ))}
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
};

export default Showtime;
