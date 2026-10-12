import React from 'react';
import { Link } from 'react-router-dom';
import { CineLogo } from './CineLogo';

export const Footer = () => {
  return (
    <footer className="w-full bg-[#0a0e18] mt-16 pt-12 pb-8 border-t border-white/5">
      <div className="w-full px-4 sm:px-8 lg:px-12 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 pb-10">
        {/* Brand & Intro */}
        <div className="space-y-4">
          <Link to="/" className="inline-block">
            <CineLogo size="md" />
          </Link>
          <p className="text-[#af8782] text-xs leading-relaxed">
            Hệ thống rạp chiếu phim Vibe Cinema hiện đại với chuẩn âm thanh vòm sống động, ghế ngồi thoải mái và trải nghiệm đặt vé nhanh chóng, tiện lợi.
          </p>
          <div className="space-y-2 pt-1 text-xs text-[#af8782]">
            <div className="flex items-center gap-2">
              <span className="material-symbols-outlined text-[#ffb4aa] text-[18px]">call</span>
              <span>Hotline hỗ trợ: 1900 8888 (8:00 - 23:00)</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="material-symbols-outlined text-[#ffb4aa] text-[18px]">mail</span>
              <span>cskh@Vibe Cinema.vn</span>
            </div>
          </div>
        </div>

        {/* Policy & Refund */}
        <div className="space-y-4">
          <h4 className="font-['Space_Grotesk'] text-sm font-bold text-[#dfe2f1] tracking-wide uppercase">
            Chính sách vé &amp; Hoàn hủy
          </h4>
          <div className="p-3.5 rounded-xl bg-[#171b26] border border-white/5 space-y-1.5">
            <div className="flex items-center gap-1.5 text-[#ffb95f]">
              <span className="material-symbols-outlined text-[18px]">verified</span>
              <span className="font-semibold text-xs">Hủy vé linh hoạt trước 120 phút</span>
            </div>
            <p className="text-[#af8782] text-[11px] leading-relaxed">
              Khách hàng có thể hủy vé trước giờ khởi chiếu tối thiểu 120 phút trong mục Lịch sử đặt vé. Ghế sẽ được hoàn lại hệ thống ngay lập tức.
            </p>
          </div>
          <ul className="space-y-1.5 text-xs text-[#af8782]">
            <li className="hover:text-[#dfe2f1] transition-colors cursor-pointer">
              • Quy chế bảo mật thông tin tài khoản
            </li>
            <li className="hover:text-[#dfe2f1] transition-colors cursor-pointer">
              • Điều khoản sử dụng hệ thống Vibe Cinema
            </li>
            <li className="hover:text-[#dfe2f1] transition-colors cursor-pointer">
              • Quy định phân loại độ tuổi khán giả (P, T13, C16, C18)
            </li>
          </ul>
        </div>

        {/* Payment & Simulation info */}
        <div className="space-y-4">
          <h4 className="font-['Space_Grotesk'] text-sm font-bold text-[#dfe2f1] tracking-wide uppercase">
            Thanh toán mô phỏng
          </h4>
          <p className="text-[#af8782] text-xs">
            Hệ thống trong phiên bản thử nghiệm hỗ trợ thanh toán giả lập tức thì:
          </p>
          <div className="p-3 rounded-xl bg-[#171b26] border border-white/5 space-y-2">
            <div className="flex items-center gap-2 text-xs text-[#7bd0ff]">
              <span className="material-symbols-outlined text-[18px]">bolt</span>
              <span className="font-semibold">Thanh toán không cần thẻ thật</span>
            </div>
            <p className="text-[#af8782] text-[11px] leading-relaxed">
              Chỉ cần bấm "Xác nhận đặt vé", hệ thống sẽ kích hoạt vé điện tử kèm mã vé QR ngay tức khắc.
            </p>
          </div>
        </div>

        {/* System & Support */}
        <div className="space-y-4">
          <h4 className="font-['Space_Grotesk'] text-sm font-bold text-[#dfe2f1] tracking-wide uppercase">
            Hệ thống rạp đối tác
          </h4>
          <p className="text-[#af8782] text-xs leading-relaxed">
            Hỗ trợ mạng lưới rạp chiếu tại TP. Hồ Chí Minh với công nghệ màn chiếu IMAX, Dolby Atmos và 4DX chân thực.
          </p>
          <div className="p-3 rounded-xl bg-[#171b26] border border-white/5 flex items-center gap-3">
            <span className="material-symbols-outlined text-[#ffb95f] text-[24px]">theater_comedy</span>
            <div>
              <div className="text-xs font-semibold text-[#dfe2f1]">Vibe Cinema Cinema System</div>
              <div className="text-[10px] text-[#af8782]">Công nghệ trải nghiệm phim số 1</div>
            </div>
          </div>
        </div>
      </div>

      <div className="w-full px-4 sm:px-8 lg:px-12 pt-6 border-t border-white/5 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-[#af8782]">
        <p>© 2026 Vibe Cinema System. Tất cả các quyền được bảo lưu.</p>
        <p className="text-[11px] text-[#af8782]/70">
          Dự án Hệ thống Đặt vé Xem phim Trực tuyến (React + Tailwind CSS)
        </p>
      </div>
    </footer>
  );
};

