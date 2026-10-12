import React, { useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { Button } from '../../components/Button';
import { INITIAL_TICKETS, MOVIES, THEATERS } from '../../assets/mockData';

export const Checkout = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Booking state from SeatSelection
  const bookingData = location.state || {
    movieId: MOVIES[0].id,
    movieTitle: MOVIES[0].title,
    moviePoster: MOVIES[0].poster,
    theaterName: THEATERS[0].name,
    theaterAddress: THEATERS[0].address,
    room: 'Phòng Chiếu 1 (IMAX)',
    format: 'IMAX Laser 3D',
    date: 'Hôm nay (09/10)',
    time: '19:30',
    selectedSeats: ['F07', 'F08'],
    totalPrice: 190000,
  };

  // Get current user from localStorage
  const getUser = () => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_user');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  };

  const currentUser = getUser();
  const [isProcessing, setIsProcessing] = useState(false);
  const [successTicket, setSuccessTicket] = useState(null);

  const handleConfirmPayment = () => {
    // If not logged in, prompt to login or auto-mock login for guest
    if (!currentUser) {
      const wantLogin = window.confirm(
        'Bạn chưa đăng nhập. Bạn có muốn chuyển đến trang Đăng Nhập để lưu vé vào tài khoản không?\n(Chọn Cancel để tiếp tục đặt vé với tư cách Khách)'
      );
      if (wantLogin) {
        navigate('/login', { state: { returnUrl: '/checkout', bookingData } });
        return;
      }
    }

    setIsProcessing(true);

    setTimeout(() => {
      setIsProcessing(false);
      const randomCode = 'CT-' + Math.floor(100000 + Math.random() * 900000);
      const newTicket = {
        id: 'ticket-' + Date.now(),
        bookingCode: randomCode,
        movieTitle: bookingData.movieTitle,
        moviePoster: bookingData.moviePoster,
        theaterName: bookingData.theaterName,
        theaterAddress: bookingData.theaterAddress,
        roomName: bookingData.room,
        format: bookingData.format,
        showDate: bookingData.date,
        showTime: bookingData.time,
        seats: bookingData.selectedSeats,
        totalPrice: bookingData.totalPrice,
        paymentMethod: 'Thanh toán mô phỏng (Simulated)',
        bookingDate: 'Vừa xong',
        status: 'confirmed',
        canCancel: true,
        qrValue: `Vibe Cinema_${randomCode}_${bookingData.selectedSeats.join('_')}`,
      };

      // Save to localStorage tickets list
      try {
        const stored = localStorage.getItem('Vibe Cinema_tickets');
        const list = stored ? JSON.parse(stored) : INITIAL_TICKETS;
        const updated = [newTicket, ...list];
        localStorage.setItem('Vibe Cinema_tickets', JSON.stringify(updated));
      } catch {
        // no-op
      }

      setSuccessTicket(newTicket);
    }, 1200);
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-8 max-w-4xl mx-auto space-y-8">
        {/* SUCCESS VIEW (Step 4 Success) */}
        {successTicket ? (
          <div className="py-8 space-y-6 animate-fadeIn">
            <div className="text-center space-y-2">
              <div className="w-16 h-16 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center mx-auto text-3xl">
                <span className="material-symbols-outlined text-[36px]">check_circle</span>
              </div>
              <h1 className="font-['Space_Grotesk'] text-2xl sm:text-3xl font-bold text-[#dfe2f1]">
                Đặt Vé &amp; Thanh Toán Thành Công!
              </h1>
              <p className="text-xs text-[#af8782]">
                Mã đặt vé của bạn là{' '}
                <span className="text-[#ffb95f] font-bold">#{successTicket.bookingCode}</span>.
                Vui lòng xuất trình mã QR tại quầy soát vé rạp.
              </p>
            </div>

            {/* Ticket Card */}
            <div className="rounded-3xl bg-[#171b26] border border-white/10 overflow-hidden shadow-2xl">
              <div className="p-6 bg-gradient-to-r from-[#1c1f2a] to-[#262a35] border-b border-white/5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                  <img
                    src={successTicket.moviePoster}
                    alt={successTicket.movieTitle}
                    className="w-16 aspect-[2/3] object-cover rounded-xl shadow-md shrink-0"
                  />
                  <div>
                    <span className="text-[10px] text-[#ffb95f] font-bold uppercase tracking-wider">
                      {successTicket.format}
                    </span>
                    <h3 className="font-['Space_Grotesk'] text-lg font-bold text-[#dfe2f1]">
                      {successTicket.movieTitle}
                    </h3>
                    <p className="text-xs text-[#af8782]">{successTicket.theaterName}</p>
                  </div>
                </div>

                <div className="text-right">
                  <span className="px-3 py-1 rounded-full text-[10px] font-bold bg-emerald-950 text-emerald-400 border border-emerald-900/40">
                    ĐÃ XÁC NHẬN
                  </span>
                  <div className="font-['Space_Grotesk'] text-xs text-[#dfe2f1] font-bold mt-1">
                    #{successTicket.bookingCode}
                  </div>
                </div>
              </div>

              <div className="p-6 grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs border-b border-white/5">
                <div>
                  <span className="text-[10px] text-[#af8782] uppercase block">Ngày chiếu</span>
                  <span className="font-semibold text-[#dfe2f1]">{successTicket.showDate}</span>
                </div>
                <div>
                  <span className="text-[10px] text-[#af8782] uppercase block">Suất chiếu</span>
                  <span className="font-semibold text-[#dfe2f1]">{successTicket.showTime}</span>
                </div>
                <div>
                  <span className="text-[10px] text-[#af8782] uppercase block">Phòng rạp</span>
                  <span className="font-semibold text-[#ffb95f]">{successTicket.roomName}</span>
                </div>
                <div>
                  <span className="text-[10px] text-[#af8782] uppercase block">Ghế ngồi</span>
                  <span className="font-['Space_Grotesk'] font-bold text-[#e50914] text-base">
                    {successTicket.seats.join(', ')}
                  </span>
                </div>
              </div>

              {/* QR Code Simulation */}
              <div className="p-6 flex flex-col sm:flex-row items-center justify-between gap-6 bg-[#0a0e18]/40">
                <div className="space-y-1 text-center sm:text-left">
                  <div className="text-xs font-semibold text-[#dfe2f1]">
                    Vé Điện Tử Mã QR Một Chiều
                  </div>
                  <p className="text-[11px] text-[#af8782] max-w-sm">
                    Quét trực tiếp tại cổng rạp. Hỗ trợ hủy vé hoàn tiền mô phỏng trước giờ chiếu 120 phút.
                  </p>
                </div>

                <div className="p-3 bg-white rounded-2xl shadow-xl shrink-0 flex flex-col items-center">
                  <div className="w-28 h-28 bg-black flex items-center justify-center p-2 rounded-lg">
                    {/* Stylized QR icon */}
                    <span className="material-symbols-outlined text-white text-[72px]">
                      qr_code_2
                    </span>
                  </div>
                  <span className="text-[9px] font-mono text-black font-bold mt-1">
                    {successTicket.bookingCode}
                  </span>
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex flex-wrap items-center justify-center gap-4 pt-4">
              <Link to="/profile">
                <Button variant="primary" size="lg" icon="receipt_long">
                  Xem Trong Lịch Sử Đặt Vé
                </Button>
              </Link>
              <Link to="/">
                <Button variant="secondary" size="lg" icon="home">
                  Về Trang Chủ
                </Button>
              </Link>
            </div>
          </div>
        ) : (
          /* CHECKOUT CONFIRMATION VIEW (Step 3 Checkout) */
          <div className="space-y-6">
            <div className="pb-4 border-b border-white/5 flex items-center justify-between">
              <div>
                <span className="text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
                  Bước 3: Xác nhận đơn &amp; Thanh toán
                </span>
                <h1 className="font-['Space_Grotesk'] text-2xl lg:text-3xl font-bold text-[#dfe2f1]">
                  Xác Nhận Đặt Vé Xem Phim
                </h1>
              </div>

              <Link
                to={`/showtime/${bookingData.movieId}/seats`}
                state={bookingData}
                className="text-xs text-[#af8782] hover:text-[#dfe2f1] flex items-center gap-1"
              >
                <span className="material-symbols-outlined text-[16px]">edit</span>
                <span>Đổi ghế ngồi</span>
              </Link>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
              {/* Left Column: Booking Info & Payment Method */}
              <div className="lg:col-span-7 space-y-6">
                {/* Movie & Showtime Detail Card */}
                <div className="p-5 rounded-2xl bg-[#171b26] border border-white/5 space-y-4">
                  <div className="flex items-center gap-4">
                    <img
                      src={bookingData.moviePoster}
                      alt={bookingData.movieTitle}
                      className="w-16 aspect-[2/3] object-cover rounded-xl shadow-md shrink-0"
                    />
                    <div>
                      <span className="text-[10px] text-[#ffb95f] font-bold uppercase tracking-wider">
                        {bookingData.format}
                      </span>
                      <h2 className="font-['Space_Grotesk'] text-lg font-bold text-[#dfe2f1]">
                        {bookingData.movieTitle}
                      </h2>
                      <p className="text-xs text-[#af8782] mt-0.5">{bookingData.theaterName}</p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3 text-xs pt-3 border-t border-white/5">
                    <div>
                      <span className="text-[10px] text-[#af8782] uppercase block">Ngày xem</span>
                      <span className="font-semibold text-[#dfe2f1]">{bookingData.date}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-[#af8782] uppercase block">Suất chiếu</span>
                      <span className="font-semibold text-[#dfe2f1]">{bookingData.time}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-[#af8782] uppercase block">Phòng rạp</span>
                      <span className="font-semibold text-[#dfe2f1]">{bookingData.room}</span>
                    </div>
                    <div>
                      <span className="text-[10px] text-[#af8782] uppercase block">Vị trí ghế ({bookingData.selectedSeats.length})</span>
                      <span className="font-['Space_Grotesk'] font-bold text-[#e50914] text-sm">
                        {bookingData.selectedSeats.join(', ')}
                      </span>
                    </div>
                  </div>
                </div>

                {/* Simulated Payment Method Card */}
                <div className="p-5 rounded-2xl bg-[#171b26] border border-white/5 space-y-3">
                  <h3 className="font-['Space_Grotesk'] text-sm font-bold text-[#dfe2f1] uppercase tracking-wider">
                    Phương Thức Thanh Toán
                  </h3>
                  <div className="p-4 rounded-xl bg-[#0f131d] border border-[#e50914]/40 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-xl bg-[#e50914]/20 text-[#e50914] flex items-center justify-center font-bold">
                        <span className="material-symbols-outlined text-[24px]">verified</span>
                      </div>
                      <div>
                        <div className="text-xs font-bold text-[#dfe2f1]">
                          Thanh Toán Mô Phỏng (Simulated Checkout)
                        </div>
                        <div className="text-[11px] text-[#af8782]">
                          Không trừ tiền thực tế, vé được kích hoạt và xuất mã ngay tức thì.
                        </div>
                      </div>
                    </div>
                    <span className="w-4 h-4 rounded-full border-2 border-[#e50914] flex items-center justify-center">
                      <span className="w-2 h-2 rounded-full bg-[#e50914]" />
                    </span>
                  </div>
                </div>
              </div>

              {/* Right Column: Order Bill Summary */}
              <div className="lg:col-span-5 space-y-6">
                <div className="p-6 rounded-2xl bg-[#171b26] border border-white/5 space-y-4">
                  <h3 className="font-['Space_Grotesk'] text-base font-bold text-[#dfe2f1]">
                    Chi Tiết Giá Vé
                  </h3>

                  <div className="space-y-2 text-xs">
                    <div className="flex justify-between text-[#af8782]">
                      <span>Đơn giá vé ({bookingData.selectedSeats.length} ghế × 95.000đ):</span>
                      <span className="text-[#dfe2f1] font-semibold">
                        {(bookingData.selectedSeats.length * 95000).toLocaleString('vi-VN')}đ
                      </span>
                    </div>
                    <div className="flex justify-between text-[#af8782]">
                      <span>Phí dịch vụ &amp; tiện ích:</span>
                      <span className="text-emerald-400 font-semibold">0đ (Miễn phí)</span>
                    </div>

                    <div className="pt-3 border-t border-white/5 flex justify-between items-baseline">
                      <span className="font-bold text-sm text-[#dfe2f1]">Tổng thanh toán:</span>
                      <span className="font-['Space_Grotesk'] text-2xl font-bold text-[#e50914]">
                        {bookingData.totalPrice.toLocaleString('vi-VN')}đ
                      </span>
                    </div>
                  </div>

                  {/* Customer Info Reminder */}
                  <div className="p-3.5 rounded-xl bg-[#0f131d] text-xs space-y-1">
                    <div className="text-[#af8782]">Người nhận vé:</div>
                    <div className="font-semibold text-[#dfe2f1]">
                      {currentUser ? `${currentUser.name} (${currentUser.email})` : 'Khách vãng lai'}
                    </div>
                  </div>

                  <Button
                    variant="primary"
                    size="lg"
                    className="w-full"
                    disabled={isProcessing}
                    onClick={handleConfirmPayment}
                    icon={isProcessing ? 'hourglass_top' : 'check'}
                  >
                    {isProcessing ? 'Đang Xử Lý Đặt Vé...' : 'Xác Nhận Đặt Vé Ngay'}
                  </Button>

                  <div className="text-[11px] text-center text-[#af8782] flex items-center justify-center gap-1.5">
                    <span className="material-symbols-outlined text-[16px] text-[#ffb95f]">info</span>
                    <span>Hỗ trợ hủy vé trước giờ khởi chiếu 120 phút.</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};
export default Checkout;

