import React from 'react';
import { Link } from 'react-router-dom';

export const MovieCard = ({ movie }) => {
  const getAgeBadgeStyle = (age) => {
    switch (age) {
      case 'C18':
        return 'bg-[#93000a] text-[#ffdad6]';
      case 'C16':
      case 'T16':
      case 'T13':
        return 'bg-[#ffb95f] text-[#472a00]';
      case 'P':
        return 'bg-[#007ba6] text-[#f4faff]';
      default:
        return 'bg-[#262a35] text-[#dfe2f1]';
    }
  };

  return (
    <div className="group relative flex flex-col rounded-2xl bg-[#171b26] border border-white/5 overflow-hidden hover:-translate-y-1.5 transition-all duration-300 shadow-lg hover:shadow-2xl hover:shadow-[#e50914]/15">
      {/* Poster Container */}
      <Link
        to={`/movie/${movie.id}`}
        className="relative w-full aspect-[2/3] overflow-hidden bg-[#313540] cursor-pointer"
      >
        <img
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
          src={movie.poster}
          alt={movie.title}
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-[#171b26] via-transparent to-transparent opacity-90" />

        {/* Age & Format Badge */}
        <div className="absolute top-3 left-3 flex flex-col gap-1.5">
          <span
            className={`px-2 py-0.5 rounded-md font-['Space_Grotesk'] text-xs font-bold shadow-md ${getAgeBadgeStyle(
              movie.ageRating
            )}`}
          >
            {movie.ageRating}
          </span>
          <span className="px-2 py-0.5 rounded-md bg-[#0a0e18]/85 backdrop-blur-sm text-[#c4e7ff] text-[10px] font-semibold uppercase tracking-wider">
            {movie.format}
          </span>
        </div>

        {/* Rating Badge */}
        <div className="absolute top-3 right-3 px-2 py-1 rounded-lg bg-[#0a0e18]/85 backdrop-blur-md text-[#ffb95f] text-xs font-semibold flex items-center gap-1 shadow-sm">
          <span className="material-symbols-outlined text-[14px]">star</span>
          <span>{movie.rating}</span>
        </div>
      </Link>

      {/* Card Body */}
      <div className="p-4 flex flex-col flex-1 justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-[#af8782] text-xs">
            <span className="truncate max-w-[140px]">{movie.genre}</span>
            <span className="flex items-center gap-1 shrink-0">
              <span className="material-symbols-outlined text-[14px]">schedule</span>
              {movie.duration} phút
            </span>
          </div>

          <Link
            to={`/movie/${movie.id}`}
            className="font-['Space_Grotesk'] text-base font-bold text-[#dfe2f1] group-hover:text-[#ffb4aa] transition-colors line-clamp-1 block"
            title={movie.title}
          >
            {movie.title}
          </Link>

          <p className="text-xs text-[#af8782] line-clamp-2 leading-relaxed">
            {movie.synopsis}
          </p>
        </div>

        {/* Bottom Meta & Action Buttons */}
        <div className="space-y-2.5 pt-1 border-t border-white/5">
          <div className="text-[11px] text-[#af8782] flex items-center justify-between">
            <span>Khởi chiếu: {movie.releaseDate}</span>
            {movie.status === 'now-showing' ? (
              <span className="text-[#7bd0ff] font-bold">
                {movie.showtimesCount} suất chiếu
              </span>
            ) : (
              <span className="text-[#ffb95f] font-bold">Sắp ra mắt</span>
            )}
          </div>

          <div className="grid grid-cols-2 gap-2">
            <Link
              to={`/movie/${movie.id}`}
              className="py-2.5 rounded-xl bg-[#1c1f2a] hover:bg-[#262a35] text-[#dfe2f1] text-center text-xs font-semibold transition-colors flex items-center justify-center"
            >
              Chi tiết
            </Link>
            {movie.status === 'now-showing' ? (
              <Link
                to={`/movie/${movie.id}/showtime`}
                className="py-2.5 rounded-xl bg-[#e50914] text-white text-center text-xs font-bold hover:shadow-[0_0_15px_rgba(229,9,20,0.5)] transition-all flex items-center justify-center gap-1"
              >
                <span className="material-symbols-outlined text-[16px]">airplane_ticket</span>
                Đặt vé
              </Link>
            ) : (
              <Link
                to={`/movie/${movie.id}`}
                className="py-2.5 rounded-xl bg-[#262a35] text-[#af8782] hover:text-[#dfe2f1] text-center text-xs font-semibold transition-colors flex items-center justify-center gap-1"
              >
                Xem chi tiết
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
