import React from 'react';
import { MainLayout } from '../../layouts/MainLayout';
import { MovieCard } from '../../components/MovieCard';
import { useMovies } from '../../hooks/useMovies';

export const NowShowing = () => {
  const { items: nowShowingMovies, loading, error, refetch } = useMovies({
    status: 'NOW_SHOWING',
  });

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-8 bg-surface min-h-screen">
        <div className="max-w-[1155px] mx-auto">
          {/* Header */}
          <div className="mb-10 flex flex-col gap-4">
            <h1 className="font-headline text-3xl md:text-4xl font-black text-on-surface uppercase tracking-tight">
              Phim <span className="text-primary">Đang Chiếu</span>
            </h1>
            <p className="text-outline text-sm md:text-base max-w-2xl">
              Danh sách các bộ phim bom tấn hiện đang được trình chiếu tại hệ thống rạp Vibe Cinema.
              Đặt vé trực tuyến ngay hôm nay để nhận được vị trí ngồi tốt nhất.
            </p>
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

          {/* Error Message */}
          {!loading && error && (
            <div className="py-20 text-center bg-surface-low/50 rounded-2xl border border-primary/20 space-y-3">
              <span className="material-symbols-outlined text-[48px] text-primary">error</span>
              <h2 className="text-lg font-bold text-on-surface">Không thể tải danh sách phim</h2>
              <p className="text-outline text-xs max-w-md mx-auto">{error}</p>
              <button
                onClick={() => refetch()}
                className="px-5 py-2.5 rounded-xl bg-primary hover:bg-primary-dark text-xs font-bold text-white transition-colors cursor-pointer"
              >
                Thử lại
              </button>
            </div>
          )}

          {/* Movies Grid */}
          {!loading && !error && nowShowingMovies.length > 0 && (
            <div className="grid grid-cols-[repeat(auto-fill,270px)] justify-center gap-[25px]">
              {nowShowingMovies.map((movie) => (
                <MovieCard key={movie.movieId || movie.id} movie={movie} />
              ))}
            </div>
          )}

          {/* Empty State */}
          {!loading && !error && nowShowingMovies.length === 0 && (
            <div className="py-20 text-center">
              <div className="size-24 mx-auto mb-4 opacity-20">
                <span className="material-symbols-outlined text-[96px]">movie</span>
              </div>
              <h2 className="text-xl font-bold text-on-surface mb-2">Chưa có phim nào đang chiếu</h2>
              <p className="text-outline">Vui lòng quay lại sau để cập nhật những bộ phim mới nhất.</p>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
};

export default NowShowing;
