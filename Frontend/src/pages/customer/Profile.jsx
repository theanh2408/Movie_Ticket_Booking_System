import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { Button } from '../../components/Button';
import { INITIAL_TICKETS, MOCK_USER } from '../../assets/mockData';

export const Profile = () => {
  const navigate = useNavigate();

  // Load user
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_user');
      return stored ? JSON.parse(stored) : MOCK_USER;
    } catch {
      return MOCK_USER;
    }
  });

  // Load tickets
  const [tickets, setTickets] = useState(() => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_tickets');
      return stored ? JSON.parse(stored) : INITIAL_TICKETS;
    } catch {
      return INITIAL_TICKETS;
    }
  });

  const [activeQrModalTicket, setActiveQrModalTicket] = useState(null);
  const [toastMessage, setToastMessage] = useState('');

  const showToast = (msg) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleCancelTicket = (ticketId) => {
    const targetTicket = tickets.find((t) => t.id === ticketId);
    if (!targetTicket) return;

    const confirmed = window.confirm(
      `Bạn có chắc chắn muốn hủy vé #${targetTicket.bookingCode}?\nGhế sẽ được hoàn lại hệ thống và hoàn tiền 100% mô phỏng.`
    );

    if (confirmed) {
      const updated = tickets.map((t) => {
        if (t.id === ticketId) {
          return { ...t, status: 'cancelled', canCancel: false };
        }
        return t;
      });
      setTickets(updated);
      try {
        localStorage.setItem('Vibe Cinema_tickets', JSON.stringify(updated));
      } catch {
        // no-op
      }
      showToast(`Đã hủy vé #${targetTicket.bookingCode} thành công!`);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('Vibe Cinema_user');
    window.dispatchEvent(new Event('authChange'));
    navigate('/');
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-10 space-y-8">
        {/* Toast */}
        {toastMessage && (
          <div className="fixed top-24 right-6 z-50 bg-[#1c1f2a] border border-emerald-500/50 px-4 py-3 rounded-xl shadow-2xl text-xs text-emerald-300 flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">check_circle</span>
            <span>{toastMessage}</span>
          </div>
        )}

        {/* 1. USER PROFILE HEADER */}
        <div className="p-6 rounded-3xl bg-[#171b26] border border-white/5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-6">
          <div className="flex items-center gap-4">
            <div className="w-16 h-16 rounded-full bg-gradient-to-br from-[#e50914] to-[#93000a] text-white flex items-center justify-center font-['Space_Grotesk'] text-2xl font-bold ring-2 ring-white/10 shrink-0">
              {user.name ? user.name.charAt(0).toUpperCase() : 'U'}
            </div>
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <h1 className="font-['Space_Grotesk'] text-xl sm:text-2xl font-bold text-[#dfe2f1]">
                  {user.name}
                </h1>
                <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-[#ee9800]/20 text-[#ffb95f]">
                  Thành viên Vibe Cinema
                </span>
              </div>
              <div className="flex flex-wrap items-center gap-4 text-xs text-[#af8782]">
                <span className="flex items-center gap-1">
                  <span className="material-symbols-outlined text-[16px]">mail</span>
                  {user.email}
                </span>
                <span className="flex items-center gap-1">
                  <span className="material-symbols-outlined text-[16px]">phone</span>
                  {user.phone}
                </span>
                {user.dob && (
                  <span className="flex items-center gap-1">
                    <span className="material-symbols-outlined text-[16px]">cake</span>
                    {user.dob}
                  </span>
                )}
              </div>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <Button variant="danger" size="sm" onClick={handleLogout} icon="logout">
              Đăng Xuất
            </Button>
          </div>
        </div>

        {/* 2. TICKETS HISTORY SECTION */}
        <div className="space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-white/5">
            <div>
              <div className="text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
                Quản Lý Vé &amp; Mã QR Vào Rạp
              </div>
              <h2 className="font-['Space_Grotesk'] text-2xl font-bold text-[#dfe2f1] mt-1">
                Lịch Sử Đặt Vé Của Bạn
              </h2>
            </div>

            <div className="flex items-center gap-2 text-xs text-[#af8782]">
              <span className="material-symbols-outlined text-[#7bd0ff] text-[18px]">verified</span>
              <span>Hỗ trợ hủy vé trước giờ chiếu 120 phút</span>
            </div>
          </div>

          {/* Ticket items */}
          {tickets.length === 0 ? (
            <div className="py-20 text-center bg-[#171b26]/50 rounded-3xl border border-white/5 space-y-4">
              <div className="w-16 h-16 rounded-2xl bg-[#262a35] text-[#af8782] flex items-center justify-center mx-auto text-3xl">
                <span className="material-symbols-outlined text-[36px]">confirmation_number</span>
              </div>
              <div className="space-y-1">
                <h3 className="font-['Space_Grotesk'] text-lg font-bold text-[#dfe2f1]">
                  Bạn chưa có vé xem phim nào
                </h3>
                <p className="text-xs text-[#af8782]">
                  Hãy chọn ngay một bộ phim bom tấn đang chiếu để trải nghiệm rạp Vibe Cinema!
                </p>
              </div>
              <Link to="/">
                <Button variant="primary" size="md" icon="movie">
                  Khám Phá Phim Đang Chiếu
                </Button>
              </Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {tickets.map((ticket) => (
                <div
                  key={ticket.id}
                  className={`rounded-3xl bg-[#171b26] border overflow-hidden shadow-xl transition-all ${
                    ticket.status === 'cancelled'
                      ? 'border-white/5 opacity-60'
                      : 'border-white/10 hover:border-white/20'
                  }`}
                >
                  {/* Header banner */}
                  <div className="p-4 sm:p-5 bg-gradient-to-r from-[#1c1f2a] to-[#262a35] border-b border-white/5 flex items-start justify-between gap-4">
                    <div className="flex items-center gap-3">
                      <img
                        src={ticket.moviePoster}
                        alt={ticket.movieTitle}
                        className="w-14 sm:w-16 aspect-[2/3] object-cover rounded-xl shadow-md shrink-0"
                      />
                      <div>
                        <span className="text-[10px] text-[#ffb95f] font-bold uppercase tracking-wider">
                          {ticket.format}
                        </span>
                        <h3 className="font-['Space_Grotesk'] text-base sm:text-lg font-bold text-[#dfe2f1] line-clamp-1">
                          {ticket.movieTitle}
                        </h3>
                        <p className="text-xs text-[#af8782] mt-0.5 line-clamp-1">{ticket.theaterName}</p>
                      </div>
                    </div>

                    <div className="text-right shrink-0">
                      <span
                        className={`inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider ${
                          ticket.status === 'confirmed'
                            ? 'bg-[#ee9800]/20 text-[#ffb95f] border border-[#ee9800]/40'
                            : ticket.status === 'cancelled'
                            ? 'bg-red-950 text-red-400'
                            : 'bg-gray-800 text-gray-400'
                        }`}
                      >
                        {ticket.status === 'confirmed'
                          ? 'ĐÃ XÁC NHẬN'
                          : ticket.status === 'cancelled'
                          ? 'ĐÃ HỦY VÉ'
                          : 'ĐÃ SỬ DỤNG'}
                      </span>
                      <div className="font-['Space_Grotesk'] text-xs text-[#dfe2f1] mt-1 font-bold">
                        #{ticket.bookingCode}
                      </div>
                    </div>
                  </div>

                  {/* Body Details */}
                  <div className="p-5 space-y-4">
                    <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
                      <div>
                        <span className="text-[10px] text-[#af8782] uppercase block">Suất Chiếu</span>
                        <span className="font-semibold text-[#dfe2f1]">{ticket.showTime}</span>
                      </div>
                      <div>
                        <span className="text-[10px] text-[#af8782] uppercase block">Ngày Chiếu</span>
                        <span className="font-semibold text-[#dfe2f1]">{ticket.showDate}</span>
                      </div>
                      <div>
                        <span className="text-[10px] text-[#af8782] uppercase block">Phòng Rạp</span>
                        <span className="font-semibold text-[#ffb95f]">{ticket.roomName}</span>
                      </div>
                      <div>
                        <span className="text-[10px] text-[#af8782] uppercase block">Ghế Ngồi</span>
                        <span className="font-['Space_Grotesk'] font-bold text-[#e50914] text-sm">
                          {ticket.seats.join(', ')}
                        </span>
                      </div>
                    </div>

                    <div className="text-xs text-[#af8782] flex items-center justify-between">
                      <span>Tổng thanh toán:</span>
                      <span className="font-['Space_Grotesk'] font-bold text-[#ffb95f] text-sm">
                        {ticket.totalPrice.toLocaleString('vi-VN')}đ ({ticket.paymentMethod})
                      </span>
                    </div>

                    {/* QR Code and Actions */}
                    <div className="pt-3 border-t border-white/5 flex items-center justify-between gap-3">
                      <button
                        onClick={() => setActiveQrModalTicket(ticket)}
                        className="flex-1 py-2.5 rounded-xl bg-[#262a35] hover:bg-[#353944] text-[#dfe2f1] text-xs font-semibold flex items-center justify-center gap-1.5 transition-colors cursor-pointer"
                      >
                        <span className="material-symbols-outlined text-[18px] text-[#7bd0ff]">
                          qr_code_2
                        </span>
                        <span>Mở Mã QR Vào Rạp</span>
                      </button>

                      {ticket.status === 'confirmed' && ticket.canCancel !== false && (
                        <button
                          onClick={() => handleCancelTicket(ticket.id)}
                          className="px-4 py-2.5 rounded-xl hover:bg-red-950/40 text-red-400 hover:text-red-300 border border-red-900/30 text-xs font-semibold transition-colors cursor-pointer"
                        >
                          Hủy Vé
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* 3. MODAL QR VÀO RẠP */}
        {activeQrModalTicket && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
            <div className="relative w-full max-w-sm bg-[#0f131d] border border-white/10 rounded-3xl p-6 shadow-2xl text-center space-y-4">
              <button
                onClick={() => setActiveQrModalTicket(null)}
                className="absolute top-4 right-4 w-8 h-8 rounded-lg bg-[#262a35] hover:bg-[#353944] text-[#dfe2f1] flex items-center justify-center cursor-pointer"
              >
                <span className="material-symbols-outlined text-[18px]">close</span>
              </button>

              <div className="space-y-1 pt-2">
                <span className="text-[10px] text-[#ffb95f] font-bold uppercase tracking-wider">
                  Vé Điện Tử Quét Tại Cửa
                </span>
                <h3 className="font-['Space_Grotesk'] text-lg font-bold text-[#dfe2f1]">
                  {activeQrModalTicket.movieTitle}
                </h3>
                <p className="text-xs text-[#af8782]">
                  Ghế: {activeQrModalTicket.seats.join(', ')} • {activeQrModalTicket.showTime}
                </p>
              </div>

              {/* QR Image Box */}
              <div className="p-4 bg-white rounded-2xl mx-auto inline-block shadow-lg">
                <div className="w-44 h-44 bg-black rounded-xl flex items-center justify-center text-white">
                  <span className="material-symbols-outlined text-[100px]">qr_code_2</span>
                </div>
                <div className="mt-2 text-black font-mono font-bold text-xs">
                  {activeQrModalTicket.bookingCode}
                </div>
              </div>

              <div className="text-[11px] text-[#af8782]">
                Vui lòng tăng độ sáng màn hình điện thoại khi đưa qua máy quét tại rạp Vibe Cinema.
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
export default Profile;

