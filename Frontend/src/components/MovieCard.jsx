import React from 'react';
import { Link } from 'react-router-dom';
import { cn } from '../lib/cn';

const ageBadgeVariants = {
  C18: 'bg-primary-dark text-[#ffdad6]',
  C16: 'bg-secondary text-[#472a00]',
  T16: 'bg-secondary text-[#472a00]',
  T13: 'bg-secondary text-[#472a00]',
  P: 'bg-tertiary-container text-[#f4faff]',
};

export const MovieCard = ({ movie, className = '' }) => {
  const badgeClass = ageBadgeVariants[movie.ageRating] || 'bg-surface-high text-on-surface';

  return (
    <div
      className={cn(
        'group relative flex flex-col w-[270px] max-w-full mx-auto rounded-2xl bg-surface-low border border-white/5 overflow-hidden hover:-translate-y-1.5 transition-all duration-300 shadow-lg hover:shadow-2xl hover:shadow-primary/15',
        className
      )}
    >
      {/* Poster Container */}
      <Link
        to={`/movie/${movie.id}`}
        className="relative w-full h-[405px] overflow-hidden bg-surface-container-highest cursor-pointer block"
      >
        <img
          className="size-full object-cover group-hover:scale-105 transition-transform duration-500"
          src={movie.poster}
          alt={movie.title}
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-surface-low via-transparent to-transparent opacity-90" />

        {/* Age Badge */}
        {movie.ageRating && (
          <div className="absolute top-3 left-3">
            <span
              className={cn(
                'w-[40px] h-[26px] flex items-center justify-center rounded-md font-headline text-xs font-bold shadow-md leading-none',
                badgeClass
              )}
            >
              {movie.ageRating}
            </span>
          </div>
        )}

        {/* Rating Badge */}
        <div className="absolute top-3 right-3 px-2 py-1 rounded-lg bg-surface-lowest/85 backdrop-blur-md text-secondary text-xs font-semibold flex items-center gap-1 shadow-sm">
          <span className="material-symbols-outlined text-[14px]">star</span>
          <span>{movie.rating}</span>
        </div>
      </Link>

      {/* Card Body */}
      <div className="p-4 flex flex-col flex-1 justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-outline text-xs">
            <span className="truncate max-w-[140px]">{movie.genre}</span>
            <span className="flex items-center gap-1 shrink-0">
              <span className="material-symbols-outlined text-[14px]">schedule</span>
              {movie.duration} phút
            </span>
          </div>

          <Link
            to={`/movie/${movie.id}`}
            className="font-headline text-base font-bold text-on-surface group-hover:text-primary-light transition-colors line-clamp-1 block"
            title={movie.title}
          >
            {movie.title}
          </Link>

          <p className="text-xs text-outline line-clamp-2 leading-relaxed">
            {movie.synopsis}
          </p>
        </div>

        {/* Bottom Meta & Action Buttons */}
        <div className="space-y-2.5 pt-1 border-t border-white/5">
          

          <div className="grid grid-cols-2 gap-2">
            <Link
              to={`/movie/${movie.id}`}
              className="py-2.5 rounded-xl bg-surface-container hover:bg-surface-high text-on-surface text-center text-xs font-semibold transition-colors flex items-center justify-center"
            >
              Chi tiết
            </Link>
            {movie.status === 'now-showing' ? (
              <Link
                to={`/movie/${movie.id}/showtime`}
                className="py-2.5 rounded-xl bg-primary text-white text-center text-xs font-bold hover:shadow-glow-primary transition-all flex items-center justify-center gap-1"
              >
                <span className="material-symbols-outlined text-[16px]">airplane_ticket</span>
                Đặt vé
              </Link>
            ) : (
              <Link
                to={`/movie/${movie.id}`}
                className="py-2.5 rounded-xl bg-surface-high text-outline hover:text-on-surface text-center text-xs font-semibold transition-colors flex items-center justify-center gap-1"
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
