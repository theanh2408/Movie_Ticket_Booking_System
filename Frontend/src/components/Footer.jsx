import React from 'react';
import { Link } from 'react-router-dom';
import { CineLogo } from './CineLogo';
import { cn } from '../lib/cn';

export const Footer = ({ className = '' }) => {
  return (
    <footer className={cn('w-full bg-surface-lowest mt-16 pt-12 pb-8 border-t border-white/5', className)}>
      <div className="w-full px-4 sm:px-8 lg:px-12 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8 pb-10">
        {/* Brand & Intro */}
        <div className="space-y-4">
          <Link to="/" className="inline-block">
            <CineLogo size="md" />
          </Link>
          <p className="text-outline text-xs leading-relaxed">
            Hệ thống rạp chiếu phim Vibe Cinema hiện đại với chuẩn âm thanh vòm sống động, ghế ngồi thoải mái và trải nghiệm đặt vé nhanh chóng, tiện lợi.
          </p>
          <div className="space-y-2 pt-1 text-xs text-outline">
            <div className="flex items-center gap-2">
              <span className="material-symbols-outlined text-primary-light text-[18px]">call</span>
              <span>Hotline hỗ trợ: 1900 8888 (8:00 - 23:00)</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="material-symbols-outlined text-primary-light text-[18px]">mail</span>
              <span>cskh@Vibe Cinema.vn</span>
            </div>
          </div>
        </div>

        {/* Policy & Refund */}
        <div className="space-y-4">
          <h4 className="font-headline text-sm font-bold text-on-surface tracking-wide uppercase">
            Chính sách vé &amp; Hoàn hủy
          </h4>
          <div className="p-3.5 rounded-xl bg-surface-low border border-white/5 space-y-1.5">
            <div className="flex items-center gap-1.5 text-secondary">
              <span className="material-symbols-outlined text-[18px]">verified</span>
              <span className="font-semibold text-xs">Hủy vé linh hoạt trước 120 phút</span>
            </div>
            <p className="text-outline text-[11px] leading-relaxed">
              Khách hàng có thể hủy vé trước giờ khởi chiếu tối thiểu 120 phút trong mục Lịch sử đặt vé. Ghế sẽ được hoàn lại hệ thống ngay lập tức.
            </p>
          </div>
          <ul className="space-y-1.5 text-xs text-outline">
            <li className="hover:text-on-surface transition-colors cursor-pointer">
              • Quy chế bảo mật thông tin tài khoản
            </li>
            <li className="hover:text-on-surface transition-colors cursor-pointer">
              • Điều khoản sử dụng hệ thống Vibe Cinema
            </li>
            <li className="hover:text-on-surface transition-colors cursor-pointer">
              • Quy định phân loại độ tuổi khán giả (P, T13, C16, C18)
            </li>
          </ul>
        </div>

        {/* Payment Partners & Security */}
        <div className="space-y-4">
          <h4 className="font-headline text-sm font-bold text-on-surface tracking-wide uppercase">
            Cổng thanh toán
          </h4>
          <p className="text-outline text-xs">
            Hỗ trợ đa dạng phương thức thanh toán an toàn, bảo mật chuẩn PCI-DSS:
          </p>
          <div className="space-y-2">
            <div className="grid grid-cols-2 gap-2">
              <div className="px-3 py-2 rounded-xl bg-surface-low border border-white/5 flex items-center gap-2">
                <span className="material-symbols-outlined text-[18px] text-blue-400">qr_code_scanner</span>
                <span className="text-xs font-semibold text-zinc-300">VNPAY-QR</span>
              </div>
              <div className="px-3 py-2 rounded-xl bg-surface-low border border-white/5 flex items-center gap-2">
                <span className="material-symbols-outlined text-[18px] text-pink-400">account_balance_wallet</span>
                <span className="text-xs font-semibold text-zinc-300">Ví MoMo</span>
              </div>
              <div className="px-3 py-2 rounded-xl bg-surface-low border border-white/5 flex items-center gap-2">
                <span className="material-symbols-outlined text-[18px] text-purple-400">credit_card</span>
                <span className="text-xs font-semibold text-zinc-300">Visa / Master</span>
              </div>
              <div className="px-3 py-2 rounded-xl bg-surface-low border border-white/5 flex items-center gap-2">
                <span className="material-symbols-outlined text-[18px] text-emerald-400">account_balance</span>
                <span className="text-xs font-semibold text-zinc-300">ATM Napas</span>
              </div>
            </div>
            <div className="flex items-center gap-1.5 text-[11px] text-secondary pt-0.5">
              <span className="material-symbols-outlined text-[16px]">verified_user</span>
              <span>Bảo mật giao dịch trực tuyến 256-bit</span>
            </div>
          </div>
        </div>

        {/* System & Support */}
        <div className="space-y-4">
          <h4 className="font-headline text-sm font-bold text-on-surface tracking-wide uppercase">
            Hệ thống rạp đối tác
          </h4>
          <p className="text-outline text-xs leading-relaxed">
            Hỗ trợ mạng lưới rạp chiếu tại TP. Hồ Chí Minh với công nghệ màn chiếu IMAX, Dolby Atmos và 4DX chân thực.
          </p>
          <div className="p-3 rounded-xl bg-surface-low border border-white/5 flex items-center gap-3">
            <span className="material-symbols-outlined text-secondary text-[24px]">theater_comedy</span>
            <div>
              <div className="text-xs font-semibold text-on-surface">Vibe Cinema Cinema System</div>
              <div className="text-[10px] text-outline">Công nghệ trải nghiệm phim số 1</div>
            </div>
          </div>
        </div>
      </div>

      <div className="w-full px-4 sm:px-8 lg:px-12 pt-6 border-t border-white/5 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-outline">
        <p>© 2026 Vibe Cinema System. Tất cả các quyền được bảo lưu.</p>
        <p className="text-[11px] text-outline/70">
          Dự án Hệ thống Đặt vé Xem phim Trực tuyến (React + Tailwind CSS)
        </p>
      </div>
    </footer>
  );
};
