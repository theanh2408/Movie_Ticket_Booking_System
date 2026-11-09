import React from 'react';
import { Link } from 'react-router-dom';
import { cn } from '../lib/cn';

const ageBadgeVariants = {
  C18: 'bg-primary-dark text-[#ffdad6]',
  C16: 'bg-secondary text-[#472a00]',
  C13: 'bg-secondary text-[#472a00]',
  T16: 'bg-secondary text-[#472a00]',
  T13: 'bg-secondary text-[#472a00]',
  P: 'bg-tertiary-container text-[#f4faff]',
};

export const MovieCard = ({ movie, className = '' }) => {
  const movieId = movie.movieId ?? movie.id;
  const poster = movie.posterUrl || movie.poster || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800';
  const duration = movie.durationMinutes ?? movie.duration ?? 120;
  const synopsis = movie.description || movie.synopsis || '';
  const ageRating = movie.ageRating || 'C13';
  const rating = movie.rating || 9.0;

  // Phân loại chính xác tuyệt đối: NOW_SHOWING là đang chiếu, COMING_SOON là sắp chiếu
  const rawStatus = (movie.status || '').trim().toUpperCase();
  const isComingSoon =
    movie.isComingSoon === true ||
    rawStatus === 'COMING_SOON' ||
    rawStatus === 'COMING-SOON' ||
    movie.statusTab === 'coming-soon';

  const isNowShowing =
    !isComingSoon &&
    (movie.isNowShowing === true ||
      rawStatus === 'NOW_SHOWING' ||
      rawStatus === 'NOW-SHOWING' ||
      movie.statusTab === 'now-showing');

  const genreText = Array.isArray(movie.genres) && movie.genres.length > 0
    ? movie.genres.map((g) => (typeof g === 'object' && g !== null ? (g.genreName || g.name || '') : g)).filter(Boolean).join(' • ')
    : (typeof movie.genre === 'string' && movie.genre.trim() ? movie.genre : 'Hành động');

  const badgeClass = ageBadgeVariants[ageRating] || 'bg-surface-high text-on-surface';

  return (
    <div
      className={cn(
        'group relative flex flex-col w-[270px] max-w-full mx-auto rounded-2xl bg-surface-low border border-white/5 overflow-hidden hover:-translate-y-1.5 transition-all duration-300 shadow-lg hover:shadow-2xl hover:shadow-primary/15',
        className
      )}
    >
      {/* Poster Container */}
      <Link
        to={`/movie/${movieId}`}
        className="relative w-full h-[405px] overflow-hidden bg-surface-container-highest cursor-pointer block"
      >
        <img
          className="size-full object-cover group-hover:scale-105 transition-transform duration-500"
          src={poster}
          alt={movie.title}
          loading="lazy"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-surface-low via-transparent to-transparent opacity-90" />

        {/* Top Badges Row */}
        <div className="absolute top-3 inset-x-3 flex items-center justify-between pointer-events-none">
          <div className="flex items-center gap-1.5">
            {/* Age Badge */}
            {ageRating && (
              <span
                className={cn(
                  'w-[40px] h-[26px] flex items-center justify-center rounded-md font-headline text-xs font-bold shadow-md leading-none',
                  badgeClass
                )}
              >
                {ageRating}
              </span>
            )}

            
          </div>

          {/* Rating Badge */}
          <div className="px-2 py-1 rounded-lg bg-surface-lowest/85 backdrop-blur-md text-secondary text-xs font-semibold flex items-center gap-1 shadow-sm">
            <span className="material-symbols-outlined text-[14px]">star</span>
            <span>{rating}</span>
          </div>
        </div>
      </Link>

      {/* Card Body */}
      <div className="p-4 flex flex-col flex-1 justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-outline text-xs">
            <span className="truncate max-w-[140px]">{genreText}</span>
            <span className="flex items-center gap-1 shrink-0">
              <span className="material-symbols-outlined text-[14px]">schedule</span>
              {duration} phút
            </span>
          </div>

          <Link
            to={`/movie/${movieId}`}
            className="font-headline text-base font-bold text-on-surface group-hover:text-primary-light transition-colors line-clamp-1 block"
            title={movie.title}
          >
            {movie.title}
          </Link>

          <p className="text-xs text-outline line-clamp-2 leading-relaxed">
            {synopsis}
          </p>
        </div>

        {/* Bottom Meta & Action Buttons */}
        <div className="space-y-2.5 pt-1 border-t border-white/5">
          <div className="grid grid-cols-2 gap-2">
            <Link
              to={`/movie/${movieId}`}
              className="py-2.5 rounded-xl bg-surface-container hover:bg-surface-high text-on-surface text-center text-xs font-semibold transition-colors flex items-center justify-center"
            >
              Chi tiết
            </Link>
            {isNowShowing ? (
              <Link
                to={`/movie/${movieId}/showtime`}
                className="py-2.5 rounded-xl bg-primary text-white text-center text-xs font-bold hover:shadow-glow-primary transition-all flex items-center justify-center gap-1"
              >
                <span className="material-symbols-outlined text-[16px]">airplane_ticket</span>
                Đặt vé
              </Link>
            ) : (
              <Link
                to={`/movie/${movieId}`}
                className="py-2.5 rounded-xl bg-surface-high text-outline hover:text-on-surface text-center text-xs font-semibold transition-colors flex items-center justify-center gap-1"
              >
                <span className="material-symbols-outlined text-[16px]">info</span>
                Xem chi tiết
              </Link>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default MovieCard;
