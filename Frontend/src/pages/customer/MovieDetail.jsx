import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import movieApi from '../../api/movieApi';
import cinemaApi from '../../api/cinemaApi';
import showtimeApi from '../../api/showtimeApi';
import { cn } from '../../lib/cn';

// Tạo danh sách 5 ngày liên tiếp bắt đầu từ hôm nay
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

export const MovieDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  const [movie, setMovie] = useState(null);
  const [loadingMovie, setLoadingMovie] = useState(true);
  const [movieError, setMovieError] = useState(null);

  const [cinemas, setCinemas] = useState([]);
  const [selectedCinema, setSelectedCinema] = useState(null);

  const dateList = useMemo(() => generateNextDates(), []);
  const [selectedDate, setSelectedDate] = useState(dateList[0]);

  const [showtimes, setShowtimes] = useState([]);
  const [loadingShowtimes, setLoadingShowtimes] = useState(false);
  const [showtimesError, setShowtimesError] = useState(null);

  const [showTrailerModal, setShowTrailerModal] = useState(false);

  // 1. Tải thông tin phim theo ID
  const fetchMovie = useCallback(async () => {
    if (!id) return;
    setLoadingMovie(true);
    setMovieError(null);
    try {
      const res = await movieApi.getMovieById(id);
      if (res && res.data) {
        const m = res.data;
        setMovie({
          ...m,
          movieId: m.movieId ?? m.id,
          id: m.movieId ?? m.id,
          poster: m.posterUrl || m.poster || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800',
          backdrop: m.posterUrl || m.poster,
          duration: m.durationMinutes ?? m.duration ?? 120,
          synopsis: m.description || m.synopsis || '',
          genre: Array.isArray(m.genres) ? m.genres.map((g) => g.name || g).join(' • ') : (m.genre || 'Hành động'),
          rating: 9.2,
          voteCount: '12.4k đánh giá',
          ageRating: m.ageRating || 'C13',
          ageRatingLabel: `${m.ageRating || 'C13'} - Phù hợp theo độ tuổi quy định`,
          language: 'Tiếng Việt - Phụ đề',
          releaseDate: m.releaseDate || '2026-10-01',
          trailerUrl: m.trailerUrl || 'https://www.youtube.com/embed/TcMBFSGVi1c',
          isNowShowing: m.status?.toUpperCase() === 'NOW_SHOWING',
        });
      } else {
        setMovieError('Không tìm thấy thông tin phim.');
      }
    } catch (err) {
      console.error('Lỗi khi tải chi tiết phim:', err);
      setMovieError(err?.message || 'Không thể tải thông tin phim từ máy chủ.');
    } finally {
      setLoadingMovie(false);
    }
  }, [id]);

  useEffect(() => {
    fetchMovie();
  }, [fetchMovie]);

  // 2. Tải danh sách rạp
  useEffect(() => {
    const fetchCinemas = async () => {
      try {
        const res = await cinemaApi.getCinemas();
        if (res && res.data && res.data.length > 0) {
          setCinemas(res.data);
          setSelectedCinema(res.data[0]);
        }
      } catch (err) {
        console.error('Lỗi khi tải danh sách rạp:', err);
      }
    };
    fetchCinemas();
  }, []);

  // 3. Tải danh sách suất chiếu của phim tại rạp và ngày đã chọn
  const fetchShowtimes = useCallback(async () => {
    if (!id || !selectedCinema) return;
    setLoadingShowtimes(true);
    setShowtimesError(null);
    try {
      const params = {
        movieId: id,
        cinemaId: selectedCinema.cinemaId,
        date: selectedDate.id,
      };
      const res = await showtimeApi.getShowtimes(params);
      if (res && res.data) {
        setShowtimes(Array.isArray(res.data) ? res.data : []);
      } else {
        setShowtimes([]);
      }
    } catch (err) {
      console.error('Lỗi khi tải suất chiếu:', err);
      setShowtimesError('Không thể tải danh sách suất chiếu.');
      setShowtimes([]);
    } finally {
      setLoadingShowtimes(false);
    }
  }, [id, selectedCinema, selectedDate]);

  useEffect(() => {
    fetchShowtimes();
  }, [fetchShowtimes]);

  // Xử lý khi chọn suất chiếu
  const handleSelectShowtime = (st) => {
    const bookingPayload = {
      movieId: movie.movieId,
      movieTitle: movie.title,
      moviePoster: movie.poster,
      theaterId: selectedCinema?.cinemaId,
      theaterName: selectedCinema?.cinemaName,
      theaterAddress: selectedCinema?.address,
      showtimeId: st.showtimeId,
      time: st.startTime || st.startHour,
      room: st.roomName || 'Phòng 1',
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

  // Trạng thái Loading Phim
  if (loadingMovie) {
    return (
      <MainLayout>
        <div className="w-full px-4 sm:px-8 lg:px-12 py-20 flex flex-col items-center justify-center space-y-4">
          <div className="size-12 border-4 border-primary border-t-transparent rounded-full animate-spin" />
          <p className="text-on-surface text-sm font-semibold">Đang tải dữ liệu phim...</p>
        </div>
      </MainLayout>
    );
  }

  // Trạng thái Lỗi khi tải phim
  if (movieError || !movie) {
    return (
      <MainLayout>
        <div className="w-full px-4 sm:px-8 lg:px-12 py-24 text-center space-y-4">
          <span className="material-symbols-outlined text-[64px] text-primary">error</span>
          <h2 className="font-headline text-2xl font-bold text-on-surface">Không tìm thấy phim</h2>
          <p className="text-outline text-sm">{movieError || 'Phim không tồn tại hoặc đã bị gỡ.'}</p>
          <button
            onClick={() => navigate('/')}
            className="px-6 py-2.5 rounded-xl bg-primary text-white text-xs font-bold hover:bg-primary-dark transition-all cursor-pointer"
          >
            Quay lại trang chủ
          </button>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      {/* 1. BACKDROP & HERO BANNER */}
      <section className="relative w-full -mt-20 overflow-hidden bg-surface-lowest">
        <div
          style={{
            '--movie-backdrop': `url('${movie.backdrop || movie.poster}')`,
          }}
          className="absolute inset-0 w-full h-[450px] sm:h-[520px] bg-[image:var(--movie-backdrop)] bg-cover bg-center opacity-40 mix-blend-luminosity scale-105"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/70 to-surface/90" />
        <div className="absolute inset-0 bg-gradient-to-r from-surface via-surface/85 to-transparent" />

        <div className="relative w-full px-4 sm:px-8 lg:px-12 pt-32 pb-12 flex flex-col md:flex-row gap-8 items-start">
          {/* Poster Card */}
          <div className="w-44 sm:w-56 aspect-[2/3] rounded-2xl overflow-hidden shadow-2xl border-2 border-white/10 shrink-0 bg-surface-high relative group">
            <img
              src={movie.poster}
              alt={movie.title}
              className="size-full object-cover"
            />
            {movie.trailerUrl && (
              <button
                onClick={() => setShowTrailerModal(true)}
                className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 flex flex-col items-center justify-center gap-2 transition-opacity cursor-pointer"
              >
                <span className="material-symbols-outlined text-secondary text-[48px]">
                  play_circle
                </span>
                <span className="text-xs font-bold text-white uppercase tracking-wider">
                  Xem Trailer
                </span>
              </button>
            )}
          </div>

          {/* Movie Metadata */}
          <div className="flex-1 space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <span className="px-2.5 py-0.5 rounded bg-primary-dark text-[#ffdad6] font-headline text-xs font-bold">
                {movie.ageRating}
              </span>
              <span className="px-2.5 py-0.5 rounded bg-surface-high text-secondary text-xs font-semibold flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">star</span>
                {movie.rating}/10 ({movie.voteCount})
              </span>
              {movie.isNowShowing ? (
                <span className="px-2 py-0.5 rounded bg-emerald-600/90 text-white font-headline text-xs font-bold flex items-center gap-1 shadow-sm">
                  <span className="size-1.5 rounded-full bg-white animate-pulse" />
                  Đang công chiếu
                </span>
              ) : (
                <span className="px-2 py-0.5 rounded bg-amber-500/90 text-black font-headline text-xs font-bold flex items-center gap-1 shadow-sm">
                  <span className="size-1.5 rounded-full bg-black/50" />
                  Sắp khởi chiếu
                </span>
              )}
            </div>

            <h1 className="font-headline text-2xl sm:text-3xl lg:text-4xl font-bold text-on-surface">
              {movie.title}
            </h1>

            <p className="text-xs text-outline">{movie.ageRatingLabel}</p>

            <div className="flex flex-wrap items-center gap-4 sm:gap-6 text-xs text-on-surface-variant py-1 border-y border-white/5">
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-primary-light text-[16px]">schedule</span>
                <span>{movie.duration} phút</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-secondary text-[16px]">movie</span>
                <span>{movie.genre}</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-tertiary text-[16px]">translate</span>
                <span>{movie.language}</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-tertiary-light text-[16px]">calendar_month</span>
                <span>Khởi chiếu: {movie.releaseDate}</span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3 pt-2">
              {movie.isNowShowing ? (
                <button
                  onClick={() => {
                    const showtimesSection = document.getElementById('showtimes-section');
                    if (showtimesSection) {
                      showtimesSection.scrollIntoView({ behavior: 'smooth' });
                    }
                  }}
                  className="px-6 py-3 rounded-xl bg-primary text-white font-headline text-sm font-bold flex items-center gap-2 shadow-glow-primary hover:brightness-110 cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[18px]">airplane_ticket</span>
                  MUA VÉ NGAY
                </button>
              ) : (
                <span className="px-4 py-2.5 rounded-xl bg-surface-high text-secondary text-xs font-semibold">
                  Phim sắp chiếu - Chưa mở bán vé
                </span>
              )}

              {movie.trailerUrl && (
                <button
                  onClick={() => setShowTrailerModal(true)}
                  className="px-5 py-3 rounded-xl bg-surface-high hover:bg-surface-highest text-on-surface text-sm font-medium flex items-center gap-2 transition-all cursor-pointer"
                >
                  <span className="material-symbols-outlined text-secondary text-[18px]">play_circle</span>
                  Xem Trailer
                </button>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* 2. SYNOPSIS & RULES */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-10">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
          <div className="lg:col-span-8 space-y-6">
            <div className="space-y-3">
              <h3 className="font-headline text-base sm:text-lg font-bold text-on-surface uppercase tracking-wider flex items-center gap-2">
                <span className="w-1.5 h-4 bg-primary rounded-full" />
                Nội Dung Phim
              </h3>
              <p className="text-sm text-outline leading-relaxed whitespace-pre-line">
                {movie.synopsis || 'Nội dung đang được cập nhật.'}
              </p>
            </div>
          </div>

          <div className="lg:col-span-4 space-y-4">
            <div className="p-5 rounded-2xl bg-surface-low border border-white/5 space-y-3">
              <h4 className="font-headline text-xs font-bold text-secondary uppercase tracking-wider">
                Quy định độ tuổi &amp; Tiêu chuẩn rạp
              </h4>
              <ul className="space-y-2 text-xs text-outline">
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-tertiary text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Khán giả vui lòng xuất trình giấy tờ xác thực độ tuổi khi soát vé.</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-tertiary text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Vé đã đặt được hủy trước ít nhất 60 phút theo chính sách rạp.</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-tertiary text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Ghế đặt được giữ tối đa 10 phút để hoàn tất thanh toán an toàn.</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      {/* 3. SHOWTIMES SCHEDULE SECTION */}
      <section
        id="showtimes-section"
        className="w-full px-4 sm:px-8 lg:px-12 py-10 bg-surface-lowest/80 border-t border-white/5 space-y-8"
      >
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div className="text-xs font-bold text-secondary uppercase tracking-wider">
              Lịch chiếu phim
            </div>
            <h2 className="font-headline text-2xl font-bold text-on-surface mt-1">
              Chọn Suất Chiếu Để Đặt Vé
            </h2>
          </div>

          {/* Date Selector */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {dateList.map((d) => (
              <button
                key={d.id}
                onClick={() => setSelectedDate(d)}
                className={cn(
                  'px-3.5 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer',
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

        {/* Theater Selector */}
        <div className="space-y-3">
          <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
            Chọn cụm rạp:
          </label>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            {cinemas.map((c) => (
              <button
                key={c.cinemaId}
                onClick={() => setSelectedCinema(c)}
                className={cn(
                  'p-3.5 rounded-2xl border text-left transition-all flex flex-col justify-between cursor-pointer',
                  selectedCinema?.cinemaId === c.cinemaId
                    ? 'bg-primary/10 border-primary shadow-md'
                    : 'bg-surface-low border-white/5 hover:bg-surface-container'
                )}
              >
                <div className="flex items-center justify-between">
                  <span className="font-headline text-sm font-bold text-on-surface">
                    {c.cinemaName}
                  </span>
                  <span className="text-[10px] px-2 py-0.5 rounded-full bg-surface-high text-secondary font-bold">
                    {c.status || 'ACTIVE'}
                  </span>
                </div>
                <span className="text-xs text-outline line-clamp-1 mt-1">
                  {c.address}
                </span>
              </button>
            ))}
          </div>
        </div>

        {/* Showtimes List */}
        <div className="space-y-4 pt-2">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-on-surface flex items-center gap-2">
              <span className="size-2 rounded-full bg-secondary" />
              <span>Suất chiếu tại {selectedCinema?.cinemaName || 'Rạp đã chọn'} ({selectedDate.label}):</span>
            </h3>
            {showtimes.length > 0 && (
              <span className="text-xs text-outline">{showtimes.length} suất chiếu khả dụng</span>
            )}
          </div>

          {/* Loading Showtimes Spinner */}
          {loadingShowtimes && (
            <div className="py-12 flex items-center justify-center gap-3">
              <div className="size-6 border-2 border-primary border-t-transparent rounded-full animate-spin" />
              <span className="text-xs text-outline">Đang tải lịch chiếu từ cơ sở dữ liệu...</span>
            </div>
          )}

          {/* Showtimes Error */}
          {!loadingShowtimes && showtimesError && (
            <div className="p-4 rounded-xl bg-surface-low border border-primary/20 text-center space-y-2">
              <p className="text-xs text-primary">{showtimesError}</p>
              <button
                onClick={() => fetchShowtimes()}
                className="px-4 py-1.5 rounded-lg bg-surface-high hover:bg-surface-highest text-xs text-on-surface font-semibold"
              >
                Tải lại
              </button>
            </div>
          )}

          {/* Empty State */}
          {!loadingShowtimes && !showtimesError && showtimes.length === 0 && (
            <div className="py-12 text-center bg-surface-low/40 rounded-2xl border border-white/5 space-y-2">
              <span className="material-symbols-outlined text-[36px] text-outline">schedule</span>
              <p className="text-xs text-outline">
                {!movie.isNowShowing
                  ? `Phim dự kiến khởi chiếu vào ngày ${movie.releaseDate}. Vé và lịch chiếu sẽ được mở bán trước ngày công chiếu.`
                  : 'Không tìm thấy suất chiếu nào cho rạp và ngày đã chọn. Vui lòng chuyển sang ngày hoặc cụm rạp khác.'}
              </p>
            </div>
          )}

          {/* Showtimes Grid */}
          {!loadingShowtimes && !showtimesError && showtimes.length > 0 && (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
              {showtimes.map((st) => (
                <button
                  key={st.showtimeId}
                  onClick={() => handleSelectShowtime(st)}
                  className="p-3.5 rounded-2xl bg-surface-low border border-white/5 hover:border-primary/50 hover:bg-surface-container transition-all flex flex-col items-center justify-center gap-1 group cursor-pointer shadow-md hover:scale-105"
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
      </section>

      {/* Trailer Modal */}
      {showTrailerModal && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md flex items-center justify-center p-4">
          <div className="relative w-full max-w-4xl aspect-video bg-black rounded-2xl overflow-hidden shadow-2xl border border-white/10">
            <button
              onClick={() => setShowTrailerModal(false)}
              className="absolute top-4 right-4 z-10 size-10 rounded-full bg-surface-lowest/80 text-white flex items-center justify-center hover:bg-primary transition-colors cursor-pointer"
            >
              <span className="material-symbols-outlined text-[20px]">close</span>
            </button>
            <iframe
              src={movie.trailerUrl}
              title={`${movie.title} Trailer`}
              className="size-full"
              allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
              allowFullScreen
            />
          </div>
        </div>
      )}
    </MainLayout>
  );
};

export default MovieDetail;
