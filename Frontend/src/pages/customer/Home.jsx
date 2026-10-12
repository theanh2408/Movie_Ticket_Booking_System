import React, { useState, useMemo } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { MovieCard } from '../../components/MovieCard';
import { MOVIES, GENRES, THEATERS } from '../../assets/mockData';

export const Home = () => {
  const navigate = useNavigate();
  const [statusTab, setStatusTab] = useState('now-showing'); // 'now-showing' | 'coming-soon'
  const [selectedGenre, setSelectedGenre] = useState('Tất cả thể loại');
  const [selectedTheaterId, setSelectedTheaterId] = useState('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState('hot');

  // Featured Hero movie (top 1 now showing)
  const heroMovie = MOVIES[0];

  const filteredMovies = useMemo(() => {
    return MOVIES.filter((m) => {
      // Status filter
      if (statusTab === 'now-showing' && m.status !== 'now-showing') return false;
      if (statusTab === 'coming-soon' && m.status !== 'coming-soon') return false;

      // Genre filter
      if (selectedGenre !== 'Tất cả thể loại') {
        const matchesGenre = m.genres.some((g) =>
          g.toLowerCase().includes(selectedGenre.toLowerCase())
        );
        if (!matchesGenre) return false;
      }

      // Search query
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase().trim();
        const matchesTitle = m.title.toLowerCase().includes(q);
        const matchesDirector = m.director?.toLowerCase().includes(q);
        const matchesCast = m.cast?.some((c) => c.toLowerCase().includes(q));
        if (!matchesTitle && !matchesDirector && !matchesCast) return false;
      }

      return true;
    }).sort((a, b) => {
      if (sortBy === 'rating') return b.rating - a.rating;
      if (sortBy === 'duration') return b.duration - a.duration;
      if (sortBy === 'title') return a.title.localeCompare(b.title);
      return 0; // default hot/release
    });
  }, [statusTab, selectedGenre, searchQuery, sortBy]);

  const nowShowingCount = MOVIES.filter((m) => m.status === 'now-showing').length;
  const comingSoonCount = MOVIES.filter((m) => m.status === 'coming-soon').length;

  return (
    <MainLayout>
      {/* 1. HERO SECTION BANNER */}
      {heroMovie && (
        <section className="relative w-full -mt-20 overflow-hidden bg-[#0a0e18]">
          <div
            className="absolute inset-0 w-full h-[620px] lg:h-[720px] bg-cover bg-center opacity-40 mix-blend-luminosity scale-105 transform duration-1000 transition-all hover:scale-100"
            style={{
              backgroundImage: `url('${heroMovie.backdrop || heroMovie.poster}')`,
            }}
          />
          <div className="absolute inset-0 bg-gradient-to-t from-[#0f131d] via-[#0f131d]/60 to-[#0f131d]/90 pointer-events-none" />
          <div className="absolute inset-0 bg-gradient-to-r from-[#0f131d] via-[#0f131d]/85 to-transparent pointer-events-none" />
          <div className="absolute top-0 right-1/4 w-96 h-96 bg-[#e50914]/15 rounded-full blur-3xl pointer-events-none" />

          <div className="relative w-full px-4 sm:px-8 lg:px-12 pt-36 pb-12 flex flex-col justify-between min-h-[620px] lg:min-h-[720px]">
            <div className="max-w-3xl space-y-4">
              <div className="flex flex-wrap items-center gap-2">
                <span className="px-2.5 py-0.5 rounded bg-[#93000a] text-[#ffdad6] font-['Space_Grotesk'] text-xs font-bold tracking-wider">
                  {heroMovie.ageRating}
                </span>
                <span className="px-2.5 py-0.5 rounded bg-[#262a35] text-[#ffb95f] text-xs font-semibold tracking-wide uppercase flex items-center gap-1">
                  <span className="material-symbols-outlined text-[14px]">star</span>
                  {heroMovie.rating}/10 ({heroMovie.voteCount})
                </span>
                <span className="px-2.5 py-0.5 rounded bg-[#262a35] text-[#e9bcb6] text-xs uppercase font-medium">
                  {heroMovie.format}
                </span>
                <span className="text-[#c4e7ff] text-xs tracking-wider uppercase font-semibold flex items-center gap-1">
                  <span className="w-2 h-2 rounded-full bg-[#38bdf8] animate-pulse"></span>
                  ĐANG CHIẾU TOÀN HỆ THỐNG
                </span>
              </div>

              <h1 className="font-['Space_Grotesk'] text-3xl sm:text-4xl lg:text-5xl text-[#dfe2f1] font-bold tracking-tight leading-tight uppercase drop-shadow-md">
                {heroMovie.title}
              </h1>

              <p className="text-[#e9bcb6] text-sm lg:text-base max-w-2xl line-clamp-3 leading-relaxed">
                {heroMovie.synopsis}
              </p>

              <div className="flex flex-wrap items-center gap-4 sm:gap-6 text-[#e9bcb6] text-xs sm:text-sm py-1">
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-[#ffb4aa] text-[18px]">schedule</span>
                  <span>{heroMovie.duration} Phút</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-[#ffb95f] text-[18px]">movie_filter</span>
                  <span>{heroMovie.genre}</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-[#7bd0ff] text-[18px]">translate</span>
                  <span>{heroMovie.language}</span>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3 pt-2">
                <Link
                  to={`/movie/${heroMovie.id}/showtime`}
                  className="px-6 py-3.5 rounded-xl bg-[#e50914] text-white font-['Space_Grotesk'] text-base font-bold flex items-center gap-2 shadow-[0_8px_25px_rgba(229,9,20,0.45)] hover:scale-105 active:scale-95 transition-all"
                >
                  <span className="material-symbols-outlined text-[22px]">confirmation_number</span>
                  ĐẶT VÉ NGAY
                </Link>
                <Link
                  to={`/movie/${heroMovie.id}`}
                  className="px-6 py-3.5 rounded-xl bg-[#262a35]/80 backdrop-blur-md text-[#dfe2f1] text-base font-medium flex items-center gap-2 hover:bg-[#353944] transition-all shadow-md"
                >
                  <span className="material-symbols-outlined text-[#ffb95f] text-[22px]">info</span>
                  Chi Tiết Phim
                </Link>
              </div>
            </div>

            {/* Quick Box Office Highlight */}
            <div className="mt-8 pt-4 bg-[#1c1f2a]/80 backdrop-blur-xl rounded-2xl p-4 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 shadow-xl border border-white/5">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#e50914]/20 flex items-center justify-center text-[#e50914] shrink-0">
                  <span className="material-symbols-outlined text-[24px]">local_fire_department</span>
                </div>
                <div>
                  <div className="text-xs sm:text-sm font-semibold text-[#dfe2f1] flex items-center gap-2">
                    Xu Hướng Đặt Vé Tuần Này
                    <span className="text-[10px] bg-[#ee9800] text-[#5b3800] px-2 py-0.5 rounded-full font-bold">
                      Top 1 Box Office
                    </span>
                  </div>
                  <div className="text-xs text-[#af8782]">
                    Hơn 45.000 vé đã được giữ chỗ qua hệ thống Vibe Cinema
                  </div>
                </div>
              </div>

              <div className="flex items-center gap-2 overflow-x-auto pb-1 md:pb-0">
                {THEATERS.slice(0, 3).map((t) => (
                  <span
                    key={t.id}
                    className="px-3 py-1.5 rounded-lg bg-[#262a35] text-[#dfe2f1] text-xs whitespace-nowrap"
                  >
                    {t.shortName}: <span className="text-[#ffb95f] font-bold">Mở bán</span>
                  </span>
                ))}
              </div>
            </div>
          </div>
        </section>
      )}

      {/* 2. FILTER & SEARCH BAR */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-5 bg-[#0f131d] sticky top-20 z-40 border-y border-white/5 shadow-[0_12px_24px_rgba(0,0,0,0.6)] backdrop-blur-md">
        <div className="flex flex-col gap-4">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
            {/* Status Tabs */}
            <div className="inline-flex p-1.5 rounded-2xl bg-[#0a0e18] shrink-0 self-start border border-white/5">
              <button
                onClick={() => setStatusTab('now-showing')}
                className={`px-4 sm:px-5 py-2 rounded-xl font-['Space_Grotesk'] text-xs sm:text-sm font-bold flex items-center gap-2 transition-all cursor-pointer ${
                  statusTab === 'now-showing'
                    ? 'bg-[#e50914] text-white shadow-[0_0_15px_rgba(229,9,20,0.45)]'
                    : 'text-[#e9bcb6] hover:text-[#dfe2f1]'
                }`}
                type="button"
              >
                <span className="material-symbols-outlined text-[18px]">theaters</span>
                <span>Phim Đang Chiếu</span>
                <span
                  className={`text-[10px] px-2 py-0.5 rounded-full ${
                    statusTab === 'now-showing'
                      ? 'bg-[#0a0e18] text-white'
                      : 'bg-[#1c1f2a] text-[#af8782]'
                  }`}
                >
                  {nowShowingCount}
                </span>
              </button>
              <button
                onClick={() => setStatusTab('coming-soon')}
                className={`px-4 sm:px-5 py-2 rounded-xl font-['Space_Grotesk'] text-xs sm:text-sm font-semibold flex items-center gap-2 transition-all cursor-pointer ${
                  statusTab === 'coming-soon'
                    ? 'bg-[#e50914] text-white shadow-[0_0_15px_rgba(229,9,20,0.45)]'
                    : 'text-[#e9bcb6] hover:text-[#dfe2f1]'
                }`}
                type="button"
              >
                <span className="material-symbols-outlined text-[18px]">upcoming</span>
                <span>Phim Sắp Chiếu</span>
                <span
                  className={`text-[10px] px-2 py-0.5 rounded-full ${
                    statusTab === 'coming-soon'
                      ? 'bg-[#0a0e18] text-white'
                      : 'bg-[#1c1f2a] text-[#af8782]'
                  }`}
                >
                  {comingSoonCount}
                </span>
              </button>
            </div>

            {/* Search Bar */}
            <div className="flex-1 max-w-lg relative">
              <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-[#af8782] pointer-events-none text-[18px]">
                search
              </span>
              <input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="w-full bg-[#171b26] text-[#dfe2f1] placeholder:text-[#af8782] text-xs sm:text-sm pl-10 pr-9 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:bg-[#1c1f2a] focus:ring-1 focus:ring-[#e50914] transition-all"
                placeholder="Tìm theo tên phim, diễn viên, đạo diễn..."
                type="text"
              />
              {searchQuery && (
                <button
                  onClick={() => setSearchQuery('')}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-[#af8782] hover:text-[#dfe2f1]"
                  type="button"
                >
                  <span className="material-symbols-outlined text-[16px]">clear</span>
                </button>
              )}
            </div>

            {/* Sorter */}
            <div className="flex items-center gap-2 shrink-0">
              <span className="text-[#af8782] text-xs hidden sm:inline">Sắp xếp:</span>
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className="px-3 py-2 rounded-xl bg-[#171b26] text-[#dfe2f1] text-xs font-semibold border border-white/5 focus:outline-none focus:ring-1 focus:ring-[#e50914] cursor-pointer"
              >
                <option value="hot">Độ Hot Nhất</option>
                <option value="rating">Đánh giá cao nhất</option>
                <option value="duration">Thời lượng dài nhất</option>
                <option value="title">Tên phim A-Z</option>
              </select>
            </div>
          </div>

          {/* Genre Pills */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {GENRES.map((genre) => {
              const isActive = selectedGenre === genre;
              return (
                <button
                  key={genre}
                  onClick={() => setSelectedGenre(genre)}
                  className={`shrink-0 px-3.5 py-1.5 rounded-full text-xs font-semibold transition-all cursor-pointer whitespace-nowrap ${
                    isActive
                      ? 'bg-[#262a35] text-[#dfe2f1] shadow-sm border border-white/10'
                      : 'bg-[#171b26] hover:bg-[#1c1f2a] text-[#af8782] hover:text-[#dfe2f1]'
                  }`}
                  type="button"
                >
                  {genre}
                </button>
              );
            })}
          </div>
        </div>
      </section>

      {/* 3. MOVIE GRID */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-10">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
          <div className="space-y-1">
            <div className="text-[#ffb95f] text-xs font-bold uppercase tracking-widest flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-[#e50914] animate-pulse" />
              <span>Vibe Cinema Tuyển Chọn</span>
            </div>
            <h2 className="font-['Space_Grotesk'] text-2xl lg:text-3xl font-bold text-[#dfe2f1]">
              {statusTab === 'now-showing' ? 'Phim Đang Công Chiếu' : 'Phim Sắp Khởi Chiếu'}
            </h2>
          </div>
        </div>

        {filteredMovies.length === 0 ? (
          <div className="py-16 text-center bg-[#171b26]/50 rounded-2xl border border-white/5 space-y-3">
            <span className="material-symbols-outlined text-[48px] text-[#af8782]">movie_filter</span>
            <h3 className="text-base font-semibold text-[#dfe2f1]">Không tìm thấy phim phù hợp</h3>
            <p className="text-xs text-[#af8782]">
              Vui lòng thử tìm kiếm với từ khóa khác hoặc chuyển sang thể loại khác.
            </p>
            <button
              onClick={() => {
                setSelectedGenre('Tất cả thể loại');
                setSearchQuery('');
              }}
              className="px-4 py-2 rounded-xl bg-[#262a35] hover:bg-[#353944] text-xs font-semibold text-[#dfe2f1] transition-colors"
            >
              Xóa bộ lọc
            </button>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredMovies.map((movie) => (
              <MovieCard key={movie.id} movie={movie} />
            ))}
          </div>
        )}
      </section>

      {/* 4. EXPERIENCE HIGHLIGHTS */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-12 bg-[#0a0e18]/80 border-t border-white/5">
        <div className="max-w-6xl mx-auto space-y-8">
          <div className="text-center space-y-2">
            <div className="text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
              Quy trình tiện lợi
            </div>
            <h2 className="font-['Space_Grotesk'] text-2xl lg:text-3xl font-bold text-[#dfe2f1]">
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
                desc: 'Mô phỏng thanh toán 1 chạm, nhận ngay mã vé QR vào rạp tiện lợi.',
                icon: 'qr_code_2',
              },
            ].map((item, idx) => (
              <div
                key={idx}
                className="p-6 rounded-2xl bg-[#171b26] border border-white/5 hover:border-[#e50914]/40 transition-colors space-y-3 relative group"
              >
                <div className="flex items-center justify-between">
                  <div className="w-10 h-10 rounded-xl bg-[#e50914]/15 text-[#e50914] flex items-center justify-center font-bold">
                    <span className="material-symbols-outlined text-[20px]">{item.icon}</span>
                  </div>
                  <span className="font-['Space_Grotesk'] text-2xl font-bold text-white/10 group-hover:text-[#ffb95f]/30 transition-colors">
                    {item.step}
                  </span>
                </div>
                <h3 className="font-['Space_Grotesk'] text-base font-bold text-[#dfe2f1]">
                  {item.title}
                </h3>
                <p className="text-xs text-[#af8782] leading-relaxed">
                  {item.desc}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </MainLayout>
  );
};
export default Home;

