import React, { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { MOVIES, THEATERS, DATES } from '../../assets/mockData';

export const MovieDetail = () => {
  const { id } = useParams();
  const navigate = useNavigate();

  // Find movie by id, fallback to first movie if not found
  const movie = MOVIES.find((m) => m.id === id) || MOVIES[0];
  const [selectedTheater, setSelectedTheater] = useState(THEATERS[0]);
  const [selectedDate, setSelectedDate] = useState(DATES[0].label);
  const [showTrailerModal, setShowTrailerModal] = useState(false);

  // Find showtimes for selected theater
  const currentTheaterData = movie.theaters?.find(
    (t) => t.theaterId === selectedTheater.id
  );
  const showtimes = currentTheaterData?.showtimes || [
    { showtimeId: 'st-fallback-1', time: '10:30', room: 'Cinema 1', format: movie.format, price: 95000 },
    { showtimeId: 'st-fallback-2', time: '14:00', room: 'Cinema 2', format: movie.format, price: 95000 },
    { showtimeId: 'st-fallback-3', time: '17:30', room: 'Cinema 1', format: movie.format, price: 95000 },
    { showtimeId: 'st-fallback-4', time: '20:45', room: 'Cinema 3', format: movie.format, price: 95000 },
  ];

  const handleSelectShowtime = (st) => {
    navigate(`/showtime/${st.showtimeId}/seats`, {
      state: {
        movieId: movie.id,
        movieTitle: movie.title,
        moviePoster: movie.poster,
        theaterId: selectedTheater.id,
        theaterName: selectedTheater.name,
        theaterAddress: selectedTheater.address,
        showtimeId: st.showtimeId,
        time: st.time,
        room: st.room,
        format: st.format,
        date: selectedDate,
        price: st.price,
      },
    });
  };

  return (
    <MainLayout>
      {/* 1. BACKDROP & HERO BANNER */}
      <section className="relative w-full -mt-20 overflow-hidden bg-[#0a0e18]">
        <div
          className="absolute inset-0 w-full h-[450px] sm:h-[520px] bg-cover bg-center opacity-40 mix-blend-luminosity scale-105"
          style={{
            backgroundImage: `url('${movie.backdrop || movie.poster}')`,
          }}
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#0f131d] via-[#0f131d]/70 to-[#0f131d]/90" />
        <div className="absolute inset-0 bg-gradient-to-r from-[#0f131d] via-[#0f131d]/85 to-transparent" />

        <div className="relative w-full px-4 sm:px-8 lg:px-12 pt-32 pb-12 flex flex-col md:flex-row gap-8 items-start">
          {/* Poster Card */}
          <div className="w-44 sm:w-56 aspect-[2/3] rounded-2xl overflow-hidden shadow-2xl border-2 border-white/10 shrink-0 bg-[#262a35] relative group">
            <img
              src={movie.poster}
              alt={movie.title}
              className="w-full h-full object-cover"
            />
            {movie.trailerUrl && (
              <button
                onClick={() => setShowTrailerModal(true)}
                className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 flex flex-col items-center justify-center gap-2 transition-opacity cursor-pointer"
              >
                <span className="material-symbols-outlined text-[#ffb95f] text-[48px]">
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
              <span className="px-2.5 py-0.5 rounded bg-[#93000a] text-[#ffdad6] font-['Space_Grotesk'] text-xs font-bold">
                {movie.ageRating}
              </span>
              <span className="px-2.5 py-0.5 rounded bg-[#262a35] text-[#ffb95f] text-xs font-semibold flex items-center gap-1">
                <span className="material-symbols-outlined text-[14px]">star</span>
                {movie.rating}/10 ({movie.voteCount})
              </span>
              <span className="px-2.5 py-0.5 rounded bg-[#262a35] text-[#c4e7ff] text-xs font-semibold uppercase">
                {movie.format}
              </span>
              <span className="text-[#af8782] text-xs">
                {movie.status === 'now-showing' ? 'Đang công chiếu' : 'Sắp ra mắt'}
              </span>
            </div>

            <h1 className="font-['Space_Grotesk'] text-2xl sm:text-3xl lg:text-4xl font-bold text-[#dfe2f1]">
              {movie.title}
            </h1>
            {movie.originalTitle && (
              <p className="text-sm text-[#af8782] italic -mt-2">{movie.originalTitle}</p>
            )}

            <p className="text-xs text-[#af8782]">{movie.ageRatingLabel}</p>

            <div className="flex flex-wrap items-center gap-4 sm:gap-6 text-xs text-[#e9bcb6] py-1 border-y border-white/5">
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[#ffb4aa] text-[16px]">schedule</span>
                <span>{movie.duration} phút</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[#ffb95f] text-[16px]">movie</span>
                <span>{movie.genre}</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[#7bd0ff] text-[16px]">translate</span>
                <span>{movie.language}</span>
              </div>
              <div className="flex items-center gap-1">
                <span className="material-symbols-outlined text-[#c4e7ff] text-[16px]">calendar_month</span>
                <span>Khởi chiếu: {movie.releaseDate}</span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-3 pt-2">
              {movie.status === 'now-showing' ? (
                <button
                  onClick={() => {
                    const showtimesSection = document.getElementById('showtimes-section');
                    if (showtimesSection) {
                      showtimesSection.scrollIntoView({ behavior: 'smooth' });
                    }
                  }}
                  className="px-6 py-3 rounded-xl bg-[#e50914] text-white font-['Space_Grotesk'] text-sm font-bold flex items-center gap-2 shadow-[0_4px_15px_rgba(229,9,20,0.4)] hover:brightness-110 cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[18px]">airplane_ticket</span>
                  MUA VÉ NGAY
                </button>
              ) : (
                <span className="px-4 py-2.5 rounded-xl bg-[#262a35] text-[#ffb95f] text-xs font-semibold">
                  Phim chưa mở bán vé sớm
                </span>
              )}

              {movie.trailerUrl && (
                <button
                  onClick={() => setShowTrailerModal(true)}
                  className="px-5 py-3 rounded-xl bg-[#262a35] hover:bg-[#353944] text-[#dfe2f1] text-sm font-medium flex items-center gap-2 transition-all cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[#ffb95f] text-[18px]">play_circle</span>
                  Xem Trailer ({movie.trailerDuration || '2:30'})
                </button>
              )}
            </div>
          </div>
        </div>
      </section>

      {/* 2. SYNOPSIS & CAST CREW */}
      <section className="w-full px-4 sm:px-8 lg:px-12 py-10">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8">
          <div className="lg:col-span-8 space-y-6">
            <div className="space-y-3">
              <h3 className="font-['Space_Grotesk'] text-base sm:text-lg font-bold text-[#dfe2f1] uppercase tracking-wider flex items-center gap-2">
                <span className="w-1.5 h-4 bg-[#e50914] rounded-full" />
                Nội Dung Phim
              </h3>
              <p className="text-sm text-[#af8782] leading-relaxed">
                {movie.synopsis}
              </p>
            </div>

            {/* Cast & Director */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-5 rounded-2xl bg-[#171b26] border border-white/5 text-xs">
              <div>
                <span className="text-[#af8782] uppercase text-[10px] font-bold block mb-1">
                  Đạo diễn
                </span>
                <span className="text-[#dfe2f1] font-semibold text-sm">{movie.director || 'Đang cập nhật'}</span>
              </div>
              <div>
                <span className="text-[#af8782] uppercase text-[10px] font-bold block mb-1">
                  Diễn viên chính
                </span>
                <span className="text-[#dfe2f1] font-semibold text-sm">
                  {movie.cast && movie.cast.length > 0 ? movie.cast.join(', ') : 'Đang cập nhật'}
                </span>
              </div>
            </div>
          </div>

          <div className="lg:col-span-4 space-y-4">
            <div className="p-5 rounded-2xl bg-[#171b26] border border-white/5 space-y-3">
              <h4 className="font-['Space_Grotesk'] text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
                Quy định độ tuổi & Tiêu chuẩn rạp
              </h4>
              <ul className="space-y-2 text-xs text-[#af8782]">
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-[#7bd0ff] text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Khán giả cần xuất trình CCCD đúng độ tuổi tại cửa soát vé.</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-[#7bd0ff] text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Vé có thể hủy trước 120 phút bắt đầu suất chiếu trong trang cá nhân.</span>
                </li>
                <li className="flex items-start gap-2">
                  <span className="material-symbols-outlined text-[#7bd0ff] text-[16px] shrink-0 mt-0.5">verified</span>
                  <span>Màn hình đạt tiêu chuẩn hình ảnh sắc nét và âm thanh kỹ thuật số.</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      {/* 3. SHOWTIMES SCHEDULE SECTION */}
      <section id="showtimes-section" className="w-full px-4 sm:px-8 lg:px-12 py-10 bg-[#0a0e18]/80 border-t border-white/5 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <div className="text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
              Lịch chiếu phim
            </div>
            <h2 className="font-['Space_Grotesk'] text-2xl font-bold text-[#dfe2f1] mt-1">
              Chọn Suất Chiếu Để Đặt Vé
            </h2>
          </div>

          {/* Date Selector */}
          <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
            {DATES.slice(0, 5).map((d) => (
              <button
                key={d.id}
                onClick={() => setSelectedDate(d.label)}
                className={`px-3.5 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer ${
                  selectedDate === d.label
                    ? 'bg-[#e50914] text-white shadow-md'
                    : 'bg-[#171b26] text-[#af8782] hover:text-[#dfe2f1] hover:bg-[#1c1f2a]'
                }`}
              >
                {d.label}
              </button>
            ))}
          </div>
        </div>

        {/* Theater tabs */}
        <div className="flex items-center gap-2 overflow-x-auto pb-2 scrollbar-none">
          {THEATERS.map((t) => (
            <button
              key={t.id}
              onClick={() => setSelectedTheater(t)}
              className={`px-4 py-2.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-all cursor-pointer flex items-center gap-2 ${
                selectedTheater.id === t.id
                  ? 'bg-[#262a35] text-[#dfe2f1] border border-white/20 shadow-md'
                  : 'bg-[#171b26] text-[#af8782] hover:bg-[#1c1f2a] hover:text-[#dfe2f1]'
              }`}
            >
              <span className="material-symbols-outlined text-[16px] text-[#ffb95f]">location_on</span>
              <span>{t.name}</span>
            </button>
          ))}
        </div>

        {/* Showtimes Grid for this Theater */}
        <div className="p-6 rounded-2xl bg-[#171b26] border border-white/5 space-y-5">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-4 border-b border-white/5">
            <div>
              <h3 className="font-['Space_Grotesk'] text-base font-bold text-[#dfe2f1]">
                {selectedTheater.name}
              </h3>
              <p className="text-xs text-[#af8782] mt-0.5">{selectedTheater.address}</p>
            </div>
            <div className="text-xs text-[#ffb95f] font-semibold">
              {selectedDate}
            </div>
          </div>

          {movie.status === 'coming-soon' ? (
            <div className="py-12 text-center text-xs text-[#af8782]">
              Phim sắp ra mắt, lịch chiếu chính thức sẽ được mở bán trước ngày khởi chiếu 3 ngày.
            </div>
          ) : (
            <div>
              <label className="text-xs font-semibold text-[#af8782] uppercase tracking-wider block mb-3">
                Các khung giờ chiếu khả dụng (Nhấp vào giờ chiếu để chọn ghế):
              </label>
              <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
                {showtimes.map((st) => (
                  <button
                    key={st.showtimeId}
                    onClick={() => handleSelectShowtime(st)}
                    className="p-3.5 rounded-xl bg-[#0f131d] hover:bg-[#e50914]/20 border border-white/5 hover:border-[#e50914] text-center transition-all cursor-pointer group shadow-sm"
                  >
                    <div className="font-['Space_Grotesk'] text-lg font-bold text-[#dfe2f1] group-hover:text-[#ffb4aa]">
                      {st.time}
                    </div>
                    <div className="text-[10px] text-[#ffb95f] font-semibold mt-0.5 truncate">
                      {st.format}
                    </div>
                    <div className="text-[10px] text-[#af8782] mt-0.5">{st.room}</div>
                    <div className="text-[11px] text-[#7bd0ff] font-bold mt-1">
                      {st.price.toLocaleString('vi-VN')}đ
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>
      </section>

      {/* 4. TRAILER MODAL */}
      {showTrailerModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
          <div className="relative w-full max-w-3xl bg-[#0f131d] border border-white/10 rounded-3xl overflow-hidden shadow-2xl">
            <div className="p-4 bg-[#171b26] flex items-center justify-between border-b border-white/5">
              <span className="font-['Space_Grotesk'] text-sm font-bold text-[#dfe2f1]">
                Trailer - {movie.title}
              </span>
              <button
                onClick={() => setShowTrailerModal(false)}
                className="w-8 h-8 rounded-lg bg-[#262a35] hover:bg-[#353944] text-[#dfe2f1] flex items-center justify-center cursor-pointer"
              >
                <span className="material-symbols-outlined text-[18px]">close</span>
              </button>
            </div>
            <div className="aspect-video w-full bg-black">
              <iframe
                src={movie.trailerUrl}
                title={movie.title}
                className="w-full h-full"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
          </div>
        </div>
      )}
    </MainLayout>
  );
};
export default MovieDetail;
