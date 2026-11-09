import React, { useState, useMemo, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { MovieCard } from '../../components/MovieCard';
import { useMovies } from '../../hooks/useMovies';
import cinemaApi from '../../api/cinemaApi';
import { cn } from '../../lib/cn';

export const Home = () => {
  const [statusTab, setStatusTab] = useState('now-showing'); // 'now-showing' | 'coming-soon'
  const [selectedGenreId, setSelectedGenreId] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState('hot');
  const [cinemas, setCinemas] = useState([]);

  // Sử dụng custom hook useMovies kết nối với Backend .NET
  const {
    items: movies,
    genres,
    loading,
    error,
    refetch,
  } = useMovies({
    status: statusTab === 'now-showing' ? 'NOW_SHOWING' : 'COMING_SOON',
    search: searchQuery,
    genreId: selectedGenreId,
  });

  // Tải danh sách rạp từ Backend cho khu vực Highlight
  useEffect(() => {
    const fetchCinemas = async () => {
      try {
        const res = await cinemaApi.getCinemas();
        if (res && res.data) {
          setCinemas(res.data);
        }
      } catch (err) {
        console.error('Lỗi khi tải danh sách rạp:', err);
      }
    };
    fetchCinemas();
  }, []);

  // Lấy phim nổi bật cho Hero Banner
  const heroMovie = movies.length > 0 ? movies[0] : null;

  // Lọc và sắp xếp phía client nếu cần thêm tiêu chí bổ sung
  const sortedMovies = useMemo(() => {
    return [...movies].sort((a, b) => {
      if (sortBy === 'rating') return (b.rating || 0) - (a.rating || 0);
      if (sortBy === 'duration') return (b.duration || 0) - (a.duration || 0);
      if (sortBy === 'title') return a.title.localeCompare(b.title);
      return 0;
    });
  }, [movies, sortBy]);

  return (
    <MainLayout>
      {/* 1. HERO SECTION BANNER */}
      {heroMovie && (
        <section className="relative w-full -mt-20 overflow-hidden bg-surface-lowest">
          <div
            style={{
              '--hero-bg': `url('${heroMovie.backdrop || heroMovie.poster}')`,
            }}
            className="absolute inset-0 w-full h-[620px] lg:h-[720px] bg-[image:var(--hero-bg)] bg-cover bg-center opacity-40 mix-blend-luminosity scale-105 transform duration-1000 transition-all hover:scale-100"
          />
          <div className="absolute inset-0 bg-gradient-to-t from-surface via-surface/60 to-surface/90 pointer-events-none" />
          <div className="absolute inset-0 bg-gradient-to-r from-surface via-surface/85 to-transparent pointer-events-none" />
          <div className="absolute top-0 right-1/4 size-96 bg-primary/15 rounded-full blur-3xl pointer-events-none" />

          <div className="relative w-full px-4 sm:px-8 lg:px-12 pt-36 pb-12 flex flex-col justify-between min-h-[620px] lg:min-h-[720px]">
            <div className="max-w-3xl space-y-4">
              <div className="flex flex-wrap items-center gap-2">
                <span className="px-2.5 py-0.5 rounded bg-primary-dark text-[#ffdad6] font-headline text-xs font-bold tracking-wider">
                  {heroMovie.ageRating}
                </span>
                <span className="px-2.5 py-0.5 rounded bg-surface-high text-secondary text-xs font-semibold tracking-wide uppercase flex items-center gap-1">
                  <span className="material-symbols-outlined text-[14px]">star</span>
                  {heroMovie.rating}/10 ({heroMovie.voteCount})
                </span>
                <span className="text-tertiary-light text-xs tracking-wider uppercase font-semibold flex items-center gap-1">
                  <span className="size-2 rounded-full bg-sky-400 animate-pulse" />
                  {heroMovie.status === 'NOW_SHOWING' ? 'ĐANG CHIẾU TOÀN HỆ THỐNG' : 'SẮP KHỞI CHIẾU'}
                </span>
              </div>

              <h1 className="font-headline text-3xl sm:text-4xl lg:text-5xl text-on-surface font-bold tracking-tight leading-tight uppercase drop-shadow-md">
                {heroMovie.title}
              </h1>

              <p className="text-on-surface-variant text-sm lg:text-base max-w-2xl line-clamp-3 leading-relaxed">
                {heroMovie.synopsis || heroMovie.description}
              </p>

              <div className="flex flex-wrap items-center gap-4 sm:gap-6 text-on-surface-variant text-xs sm:text-sm py-1">
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-primary-light text-[18px]">schedule</span>
                  <span>{heroMovie.duration} Phút</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-secondary text-[18px]">movie_filter</span>
                  <span>{heroMovie.genre}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-tertiary text-[18px]">translate</span>
                  <span>{heroMovie.language}</span>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3 pt-2">
                {heroMovie.status === 'NOW_SHOWING' ? (
                  <Link
                    to={`/movie/${heroMovie.movieId}/showtime`}
                    className="px-6 py-3.5 rounded-xl bg-primary text-white font-headline text-base font-bold flex items-center gap-2 shadow-glow-primary-lg hover:scale-105 active:scale-95 transition-all"
                  >
                    <span className="material-symbols-outlined text-[22px]">confirmation_number</span>
                    ĐẶT VÉ NGAY
                  </Link>
                ) : (
                  <Link
                    to={`/movie/${heroMovie.movieId}`}
                    className="px-6 py-3.5 rounded-xl bg-primary text-white font-headline text-base font-bold flex items-center gap-2 shadow-glow-primary-lg hover:scale-105 active:scale-95 transition-all"
                  >
                    <span className="material-symbols-outlined text-[22px]">info</span>
                    XEM THÔNG TIN
                  </Link>
                )}
                <Link
                  to={`/movie/${heroMovie.movieId}`}
                  className="px-6 py-3.5 rounded-xl bg-surface-high/80 backdrop-blur-md text-on-surface text-base font-medium flex items-center gap-2 hover:bg-surface-highest transition-all shadow-md"
                >
                  <span className="material-symbols-outlined text-secondary text-[22px]">info</span>
                  Chi Tiết Phim
                </Link>
              </div>
            </div>

            {/* Quick Box Office Highlight */}
            <div className="mt-8 pt-4 bg-surface-container/80 backdrop-blur-xl rounded-2xl p-4 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 shadow-xl border border-white/5">
              <div className="flex items-center gap-3">
                <div className="size-10 rounded-xl bg-primary/20 flex items-center justify-center text-primary shrink-0">
                  <span className="material-symbols-outlined text-[24px]">local_fire_department</span>
                </div>
                <div>
                  <div className="text-xs sm:text-sm font-semibold text-on-surface flex items-center gap-2">
                    Xu Hướng Đặt Vé Tuần Này
                    <span className="text-[10px] bg-secondary-container text-on-secondary-container px-2 py-0.5 rounded-full font-bold">
                      Hệ Thống Rạp Hiện Đại
                    </span>
                  </div>
                  <div className="text-xs text-outline">
                    Dữ liệu suất chiếu và giá vé cập nhật trực tiếp theo thời gian thực
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2 overflow-x-auto pb-1 md:pb-0">
                {cinemas.map((c) => (
                  <span
                    key={c.cinemaId}
                    className="px-3 py-1.5 rounded-lg bg-surface-high text-on-surface text-xs whitespace-nowrap"
                  >
                    {c.cinemaName}: <span className="text-secondary font-bold">Đang mở cửa</span>
                  </span>
                ))}
              </div>
            </div>
          </div>
        </section>
      )}

      {/* 2. EXPERIENCE HIGHLIGHTS */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-12 bg-surface-lowest/80 border-t border-white/5">
        <div className="max-w-6xl mx-auto space-y-8">
          <div className="text-center space-y-2">
            <div className="text-xs font-bold text-secondary uppercase tracking-wider">
              Quy trình tiện lợi
            </div>
            <h2 className="font-headline text-2xl lg:text-3xl font-bold text-on-surface">
              Trải Nghiệm Đặt Vé 4 Bước Nhanh Chóng
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {[
              {
                step: '01',
                title: 'Chọn Phim Yêu Thích',
                desc: 'Khám phá các bom tấn điện ảnh đang chiếu và xem trailer chất lượng cao.',
                icon: 'movie',
              },
              {
                step: '02',
                title: 'Chọn Suất & Rạp Chiếu',
                desc: 'Hệ thống rạp phủ sóng rộng khắp với lịch chiếu cập nhật liên tục.',
                icon: 'schedule',
              },
              {
                step: '03',
                title: 'Chọn Ghế Trực Quan',
                desc: 'Sơ đồ phòng chiếu sắc nét, hiển thị trạng thái ghế thực tế tức thì.',
                icon: 'event_seat',
              },
              {
                step: '04',
                title: 'Thanh Toán & Nhận Vé',
                desc: 'Thanh toán an toàn đa phương thức, nhận ngay vé điện tử QR vào rạp.',
                icon: 'qr_code_2',
              },
            ].map((item, idx) => (
              <div
                key={idx}
                className="p-6 rounded-2xl bg-surface-low border border-white/5 hover:border-primary/40 transition-colors space-y-3 relative group"
              >
                <div className="flex items-center justify-between">
                  <div className="size-10 rounded-xl bg-primary/15 text-primary flex items-center justify-center font-bold">
                    <span className="material-symbols-outlined text-[20px]">{item.icon}</span>
                  </div>
                  <span className="font-headline text-2xl font-bold text-white/10 group-hover:text-secondary/30 transition-colors">
                    {item.step}
                  </span>
                </div>
                <h3 className="font-headline text-base font-bold text-on-surface">
                  {item.title}
                </h3>
                <p className="text-xs text-outline leading-relaxed">
                  {item.desc}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* 3. FILTER & SEARCH BAR */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-5 bg-surface sticky top-20 z-40 border-y border-white/5 shadow-[0_12px_24px_rgba(0,0,0,0.6)] backdrop-blur-md">
        <div className="max-w-[1155px] mx-auto flex flex-col gap-4">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
            {/* Status Tabs */}
            <div className="inline-flex p-1.5 rounded-2xl bg-surface-lowest shrink-0 self-start border border-white/5">
              <button
                onClick={() => setStatusTab('now-showing')}
                className={cn(
                  'px-4 sm:px-5 py-2 rounded-xl font-headline text-xs sm:text-sm font-bold flex items-center gap-2 transition-all cursor-pointer',
                  statusTab === 'now-showing'
                    ? 'bg-primary text-white shadow-glow-primary'
                    : 'text-on-surface-variant hover:text-on-surface'
                )}
                type="button"
              >
                <span className="material-symbols-outlined text-[18px]">theaters</span>
                <span>Phim Đang Chiếu</span>
              </button>
              <button
                onClick={() => setStatusTab('coming-soon')}
                className={cn(
                  'px-4 sm:px-5 py-2 rounded-xl font-headline text-xs sm:text-sm font-semibold flex items-center gap-2 transition-all cursor-pointer',
                  statusTab === 'coming-soon'
                    ? 'bg-primary text-white shadow-glow-primary'
                    : 'text-on-surface-variant hover:text-on-surface'
                )}
                type="button"
              >
                <span className="material-symbols-outlined text-[18px]">upcoming</span>
                <span>Phim Sắp Chiếu</span>
              </button>
            </div>

            {/* Search Bar */}
            <div className="flex-1 max-w-lg relative">
              <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-outline pointer-events-none text-[18px]">
                search
              </span>
              <input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full bg-surface-low text-on-surface placeholder:text-outline text-xs sm:text-sm pl-10 pr-9 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:bg-surface-container focus:ring-1 focus:ring-primary transition-all"
                placeholder="Tìm theo tên phim..."
                type="text"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-outline hover:text-on-surface cursor-pointer"
                  type="button"
                >
                  <span className="material-symbols-outlined text-[16px]">clear</span>
                </button>
              )}
            </div>

            {/* Sorter */}
            <div className="flex items-center gap-2 shrink-0">
              <span className="text-outline text-xs hidden sm:inline">Sắp xếp:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className="px-3 py-2 rounded-xl bg-surface-low text-on-surface text-xs font-semibold border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary cursor-pointer"
              >
                <option value="hot">Mới nhất</option>
                <option value="rating">Đánh giá cao nhất</option>
                <option value="duration">Thời lượng dài nhất</option>
                <option value="title">Tên phim A-Z</option>
              </select>
            </div>
          </div>

          {/* Genre Pills */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            <button
              onClick={() => setSelectedGenreId(null)}
              className={cn(
                'shrink-0 px-3.5 py-1.5 rounded-full text-xs font-semibold transition-all cursor-pointer whitespace-nowrap',
                selectedGenreId === null
                  ? 'bg-primary text-white shadow-sm'
                  : 'bg-surface-low hover:bg-surface-container text-outline hover:text-on-surface'
              )}
              type="button"
            >
              Tất cả thể loại
            </button>
            {genres.map((genre) => {
              const isActive = selectedGenreId === genre.genreId;
              return (
                <button
                  key={genre.genreId}
                  onClick={() => setSelectedGenreId(genre.genreId)}
                  className={cn(
                    'shrink-0 px-3.5 py-1.5 rounded-full text-xs font-semibold transition-all cursor-pointer whitespace-nowrap',
                    isActive
                      ? 'bg-primary text-white shadow-sm'
                      : 'bg-surface-low hover:bg-surface-container text-outline hover:text-on-surface'
                  )}
                  type="button"
                >
                  {genre.genreName || genre.name}
                </button>
              );
            })}
          </div>
        </div>
      </section>

      {/* 4. MOVIE GRID */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-10">
        <div className="max-w-[1155px] mx-auto space-y-8">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div className="space-y-1">
              <div className="text-secondary text-xs font-bold uppercase tracking-widest flex items-center gap-2">
                <span className="size-2 rounded-full bg-primary animate-pulse" />
                <span>Vibe Cinema Tuyển Chọn</span>
              </div>
              <h2 className="font-headline text-2xl lg:text-3xl font-bold text-on-surface">
                {statusTab === 'now-showing' ? 'Phim Đang Công Chiếu' : 'Phim Sắp Khởi Chiếu'}
              </h2>
            </div>
          </div>

          {/* Loading Skeleton */}
          {loading && (
            <div className="grid grid-cols-[repeat(auto-fill,270px)] justify-center gap-[25px]">
              {Array.from({ length: 4 }).map((_, idx) => (
                <div
                  key={idx}
                  className="w-[270px] h-[480px] rounded-2xl bg-surface-low border border-white/5 animate-pulse flex flex-col p-4 space-y-3"
                >
                  <div className="w-full h-[320px] bg-white/5 rounded-xl" />
                  <div className="h-4 bg-white/10 rounded w-3/4" />
                  <div className="h-3 bg-white/5 rounded w-1/2" />
                  <div className="h-8 bg-white/5 rounded-xl mt-auto" />
                </div>
              ))}
            </div>
          )}

          {/* Error Message State */}
          {!loading && error && (
            <div className="py-16 text-center bg-surface-low/50 rounded-2xl border border-primary/20 space-y-3">
              <span className="material-symbols-outlined text-[48px] text-primary">error</span>
              <h3 className="text-base font-semibold text-on-surface">Không thể tải dữ liệu phim</h3>
              <p className="text-xs text-outline max-w-md mx-auto">{error}</p>
              <button
                onClick={() => refetch()}
                className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-dark text-xs font-bold text-white transition-colors cursor-pointer"
              >
                Thử lại
              </button>
            </div>
          )}

          {/* Empty State */}
          {!loading && !error && sortedMovies.length === 0 && (
            <div className="py-16 text-center bg-surface-low/50 rounded-2xl border border-white/5 space-y-3">
              <span className="material-symbols-outlined text-[48px] text-outline">movie_filter</span>
              <h3 className="text-base font-semibold text-on-surface">Không tìm thấy phim phù hợp</h3>
              <p className="text-xs text-outline">
                Vui lòng thử tìm kiếm với từ khóa khác hoặc chuyển sang thể loại khác.
              </p>
              <button
                onClick={() => {
                  setSelectedGenreId(null);
                  setSearchQuery('');
                }}
                className="px-4 py-2 rounded-xl bg-surface-high hover:bg-surface-highest text-xs font-semibold text-on-surface transition-colors cursor-pointer"
              >
                Xóa bộ lọc
              </button>
            </div>
          )}

          {/* Movie Cards */}
          {!loading && !error && sortedMovies.length > 0 && (
            <div className="grid grid-cols-[repeat(auto-fill,270px)] justify-center gap-[25px]">
              {sortedMovies.map((movie) => (
                <MovieCard key={movie.movieId || movie.id} movie={movie} />
              ))}
            </div>
          )}
        </div>
      </section>
    </MainLayout>
  );
};

export default Home;
