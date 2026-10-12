import React from 'react';
import { MainLayout } from '../../layouts/MainLayout';
import { MovieCard } from '../../components/MovieCard';
import { MOVIES } from '../../assets/mockData';

export const NowShowing = () => {
  const nowShowingMovies = MOVIES.filter(m => m.status === 'now-showing');

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-8 bg-[#0f131d] min-h-screen">
        <div className="max-w-[1400px] mx-auto">
          {/* Header */}
          <div className="mb-10 flex flex-col gap-4">
            <h1 className="font-['Space_Grotesk'] text-3xl md:text-4xl font-black text-[#dfe2f1] uppercase tracking-tight">
              Phim <span className="text-[#e50914]">Đang Chiếu</span>
            </h1>
            <p className="text-[#af8782] text-sm md:text-base max-w-2xl">
              Danh sách các bộ phim bom tấn hiện đang được trình chiếu tại hệ thống rạp Vibe Cinema. 
              Đặt vé trực tuyến ngay hôm nay để nhận được vị trí ngồi tốt nhất.
            </p>
          </div>

          {/* Movies Grid */}
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4 md:gap-6 lg:gap-8">
            {nowShowingMovies.map((movie) => (
              <MovieCard key={movie.id} movie={movie} />
            ))}
          </div>

          {nowShowingMovies.length === 0 && (
            <div className="py-20 text-center">
              <div className="w-24 h-24 mx-auto mb-4 opacity-20">
                <span className="material-symbols-outlined text-[96px]">movie</span>
              </div>
              <h2 className="text-xl font-bold text-[#dfe2f1] mb-2">Chưa có phim nào đang chiếu</h2>
              <p className="text-[#af8782]">Vui lòng quay lại sau để cập nhật những bộ phim mới nhất.</p>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
};
export default NowShowing;

