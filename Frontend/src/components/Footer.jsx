export default function Footer() {
  return (
    <footer className="bg-slate-950 border-t border-slate-800/80 py-10 mt-20 text-slate-400 text-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex flex-col md:flex-row items-center justify-between gap-6">
          <div className="flex items-center gap-2">
            <span className="text-2xl">🎬</span>
            <span className="font-extrabold text-white text-lg tracking-wider">
              CINEMA<span className="text-red-500">PLUS</span>
            </span>
            <span className="text-xs text-slate-500 ml-2">Hệ Thống Đặt Vé Xem Phim Trực Tuyến</span>
          </div>
          <div className="flex gap-6 text-xs text-slate-400">
            <span>Đồ án môn học: Phát triển Ứng dụng Web</span>
            <span>Phiên bản MVP 1.0</span>
          </div>
        </div>
        <div className="mt-8 pt-6 border-t border-slate-900 text-center text-xs text-slate-600">
          © {new Date().getFullYear()} CinemaPlus. Dự án hoàn thiện phục vụ báo cáo môn học.
        </div>
      </div>
    </footer>
  );
}
