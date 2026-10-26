import React, { useState } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { Button } from '../../components/Button';
import { INITIAL_TICKETS } from '../../assets/mockData';

export const Checkout = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Booking state from SeatSelection or default matching mockup
  const rawState = location.state;
  const bookingData = {
    movieId: rawState?.movieId || 'chien-binh-vu-tru',
    movieTitle: rawState?.movieTitle || 'Chiến Binh Vũ Trụ: Tận Thế Kỷ',
    moviePoster:
      rawState?.moviePoster ||
      'https://images.unsplash.com/photo-1534447677768-be436bb09401?w=800&auto=format&fit=crop&q=80',
    theaterName:
      rawState?.theaterShortName ||
      (rawState?.theaterName?.includes('Royal Mall') ? 'Royal Mall' : rawState?.theaterName) ||
      'Royal Mall',
    theaterAddress: rawState?.theaterAddress || 'Tầng 5, TTTM Royal Mall, 182 Lê Thánh Tôn, Bến Nghé, Quận 1',
    room: rawState?.room ? rawState.room.replace(/\s*\(.*?\)\s*/g, '') : 'Phòng Chiếu 1',
    format: rawState?.format || 'IMAX 3D Laser',
    date: rawState?.date || 'Hôm nay (07/10)',
    time: rawState?.time || '19:30',
    selectedSeats:
      rawState?.selectedSeats && rawState.selectedSeats.length > 0
        ? rawState.selectedSeats
        : ['F07', 'F08', 'G07', 'G08'],
    pricePerSeat: rawState?.price || 95000,
  };

  // Payment method selection: 'vnpay' | 'momo'
  const [paymentMethod, setPaymentMethod] = useState('vnpay');

  // Promo code state
  const [promoCode, setPromoCode] = useState('CINETICKET');
  const [appliedPromo, setAppliedPromo] = useState(null);
  const [promoMessage, setPromoMessage] = useState('');

  // Current user
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

  const selectedCount = bookingData.selectedSeats.length;
  const rawTotal = selectedCount * bookingData.pricePerSeat;
  const discountAmount = appliedPromo ? 20000 : 0;
  const finalTotal = Math.max(0, rawTotal - discountAmount);

  const handleApplyPromo = () => {
    if (!promoCode.trim()) {
      setPromoMessage('Vui lòng nhập mã ưu đãi.');
      return;
    }
    if (promoCode.trim().toUpperCase() === 'CINETICKET') {
      setAppliedPromo('CINETICKET');
      setPromoMessage('Đã áp dụng mã CINETICKET: Giảm 20.000đ!');
    } else {
      setPromoMessage(`Mã "${promoCode}" không hợp lệ hoặc đã hết hạn.`);
    }
  };

  const handleClose = () => {
    if (window.history.length > 2) {
      navigate(-1);
    } else {
      navigate('/');
    }
  };

  const handleConfirmPayment = () => {
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
      const paymentMethodName =
        paymentMethod === 'vnpay' ? 'VNPAY-QR' : 'Ví Điện Tử MoMo / ShopeePay';

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
        totalPrice: finalTotal,
        paymentMethod: paymentMethodName,
        bookingDate: 'Vừa xong',
        status: 'confirmed',
        canCancel: true,
        qrValue: `VibeCinema_${randomCode}_${bookingData.selectedSeats.join('_')}`,
      };

      try {
        const stored = localStorage.getItem('Vibe Cinema_tickets');
        const list = stored ? JSON.parse(stored) : INITIAL_TICKETS;
        const updated = [newTicket, ...list];
        localStorage.setItem('Vibe Cinema_tickets', JSON.stringify(updated));

        // Sync with transaction history
        const txStored = localStorage.getItem('Vibe Cinema_transactions');
        const txList = txStored ? JSON.parse(txStored) : [];
        const now = new Date();
        const timeStr = `${String(now.getDate()).padStart(2, '0')}/${String(now.getMonth() + 1).padStart(2, '0')}/${now.getFullYear()} ${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
        const newTx = {
          id: 'tx-' + Date.now(),
          transactionTime: timeStr,
          paymentMethod: paymentMethodName,
          invoiceCode: 'HD-' + randomCode.replace('CT-', ''),
          theaterInfo: `${bookingData.theaterName} - ${bookingData.room}`,
          theaterName: bookingData.theaterName,
          roomName: bookingData.room,
          movieTitle: bookingData.movieTitle,
          seats: bookingData.selectedSeats,
          ticketCount: bookingData.selectedSeats.length,
          totalAmount: finalTotal,
          status: 'Thanh toán thành công',
        };
        localStorage.setItem('Vibe Cinema_transactions', JSON.stringify([newTx, ...txList]));
        window.dispatchEvent(new Event('storage'));
      } catch {
        // no-op
      }

      setSuccessTicket(newTicket);
    }, 1200);
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-6 lg:px-8 py-6 max-w-6xl mx-auto">
        {successTicket ? (
          /* SUCCESS VIEW (Step 4 Success) */
          <div className="py-8 space-y-6 max-w-2xl mx-auto">
            <div className="text-center space-y-2">
              <div className="size-16 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center mx-auto text-3xl">
                <span className="material-symbols-outlined text-[36px]">check_circle</span>
              </div>
              <h1 className="font-headline text-2xl sm:text-3xl font-bold text-white">
                Đặt Vé &amp; Thanh Toán Thành Công!
              </h1>
              <p className="text-xs text-zinc-400">
                Phương thức: <span className="text-white font-medium">{successTicket.paymentMethod}</span>.
                Mã đặt vé là{' '}
                <span className="text-amber-400 font-bold">#{successTicket.bookingCode}</span>.
                Vui lòng xuất trình mã QR tại rạp để vào xem phim.
              </p>
            </div>

            {/* Ticket Card */}
            <div className="rounded-3xl bg-[#11141e] border border-white/10 overflow-hidden shadow-2xl">
              <div className="p-6 bg-gradient-to-r from-[#171b26] to-[#1e2330] border-b border-white/5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                  <img
                    src={successTicket.moviePoster}
                    alt={successTicket.movieTitle}
                    className="w-16 aspect-[2/3] object-cover rounded-xl shadow-md shrink-0"
                  />
                  <div>
                    <span className="text-[10px] text-amber-400 font-bold uppercase tracking-wider">
                      {successTicket.format}
                    </span>
                    <h3 className="font-headline text-lg font-bold text-white">
                      {successTicket.movieTitle}
                    </h3>
                    <p className="text-xs text-zinc-400">{successTicket.theaterName}</p>
                  </div>
                </div>

                <div className="text-right">
                  <span className="px-3 py-1 rounded-full text-[10px] font-bold bg-emerald-950 text-emerald-400 border border-emerald-900/40">
                    ĐÃ XÁC NHẬN
                  </span>
                  <div className="font-headline text-xs text-white font-bold mt-1">
                    #{successTicket.bookingCode}
                  </div>
                </div>
              </div>

              <div className="p-6 grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs border-b border-white/5">
                <div>
                  <span className="text-[10px] text-zinc-400 uppercase block">Ngày chiếu</span>
                  <span className="font-semibold text-white">{successTicket.showDate}</span>
                </div>
                <div>
                  <span className="text-[10px] text-zinc-400 uppercase block">Suất chiếu</span>
                  <span className="font-semibold text-white">{successTicket.showTime}</span>
                </div>
                <div>
                  <span className="text-[10px] text-zinc-400 uppercase block">Phòng rạp</span>
                  <span className="font-semibold text-amber-400">{successTicket.roomName}</span>
                </div>
                <div>
                  <span className="text-[10px] text-zinc-400 uppercase block">Ghế ngồi</span>
                  <span className="font-headline font-bold text-[#e50914] text-base">
                    {successTicket.seats.join(', ')}
                  </span>
                </div>
              </div>

              {/* QR Code Ticket */}
              <div className="p-6 flex flex-col sm:flex-row items-center justify-between gap-6 bg-[#0a0d14]">
                <div className="space-y-1 text-center sm:text-left">
                  <div className="text-xs font-semibold text-white">
                    Vé Điện Tử Mã QR Một Chiều
                  </div>
                  <p className="text-[11px] text-zinc-400 max-w-sm">
                    Quét trực tiếp tại cổng rạp. Hỗ trợ hoàn hủy vé trước giờ chiếu 120 phút.
                  </p>
                </div>

                <div className="p-3 bg-white rounded-2xl shadow-xl shrink-0 flex flex-col items-center">
                  <div className="size-28 bg-black flex items-center justify-center p-2 rounded-lg">
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
              <Link to="/my-tickets">
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
          /* STEP 3 CHECKOUT VIEW */
          <div className="space-y-8">
            {/* Top Bar matching image */}
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4">
              {/* Left: Movie Header Info */}
              <div className="flex items-center gap-3 sm:gap-4">
                <div className="w-10 h-10 rounded-xl bg-red-600/10 border border-red-600/20 flex items-center justify-center shrink-0">
                  <span className="material-symbols-outlined text-[#e50914] text-2xl">
                    confirmation_number
                  </span>
                </div>
                <div>
                  <h1 className="font-headline text-lg sm:text-xl font-bold text-white tracking-wide">
                    {bookingData.movieTitle}
                  </h1>
                  <div className="flex items-center gap-2 text-xs sm:text-sm text-zinc-400 mt-0.5">
                    <span>{bookingData.theaterName}</span>
                    <span className="text-zinc-600">•</span>
                    <span className="text-amber-400 font-semibold">{bookingData.format}</span>
                    <span className="text-zinc-600">•</span>
                    <span>{bookingData.time}</span>
                  </div>
                </div>
              </div>

              {/* Right: Step Indicator & Close Button */}
              <div className="flex items-center gap-3 sm:gap-5 self-end md:self-center">
                <div className="flex items-center text-xs sm:text-sm font-medium text-zinc-400">
                  <Link
                    to={`/movie/${bookingData.movieId}`}
                    className="hover:text-white transition-colors"
                  >
                    1. Suất chiếu
                  </Link>
                  <span className="mx-2 text-zinc-600">/</span>
                  <Link
                    to={`/showtime/${bookingData.movieId}/seats`}
                    state={rawState || bookingData}
                    className="hover:text-white transition-colors"
                  >
                    2. Chọn ghế
                  </Link>
                  <span className="mx-2 text-zinc-600">/</span>
                  <span className="bg-[#e50914] text-white font-bold px-3.5 py-1 rounded-full text-xs sm:text-sm inline-flex items-center shadow-md">
                    3. Thanh toán
                  </span>
                </div>

                <button
                  type="button"
                  onClick={handleClose}
                  className="w-9 h-9 sm:w-10 sm:h-10 rounded-xl bg-[#1c202c] hover:bg-[#262c3e] text-zinc-400 hover:text-white flex items-center justify-center transition-colors cursor-pointer"
                  title="Đóng / Quay lại"
                >
                  <span className="material-symbols-outlined text-xl">close</span>
                </button>
              </div>
            </div>

            {/* Main 2-column Grid */}
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
              {/* Left Column: Payment Methods & Promo Code */}
              <div className="lg:col-span-7 space-y-6">
                <div>
                  <h2 className="text-lg sm:text-xl font-bold text-white mb-4">
                    Phương Thức Thanh Toán
                  </h2>

                  <div className="space-y-3.5">
                    {/* Method 1: VNPAY-QR */}
                    <div
                      onClick={() => setPaymentMethod('vnpay')}
                      className={`p-4 sm:p-5 rounded-2xl flex items-center justify-between cursor-pointer transition-all border ${
                        paymentMethod === 'vnpay'
                          ? 'border-red-600 bg-[#161217]'
                          : 'border-zinc-800/90 hover:border-zinc-700 bg-[#121620]'
                      }`}
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-[#0c1824] border border-cyan-500/40 flex items-center justify-center shrink-0 text-cyan-400 shadow-inner">
                          <span className="material-symbols-outlined text-[24px]">
                            account_balance_wallet
                          </span>
                        </div>
                        <div>
                          <div className="font-bold text-white text-base">VNPAY-QR</div>
                          <div className="text-xs text-zinc-400 mt-0.5">
                            Quét mã QR từ ứng dụng ngân hàng
                          </div>
                        </div>
                      </div>

                      {/* Radio Indicator */}
                      <div className="shrink-0 pl-3">
                        {paymentMethod === 'vnpay' ? (
                          <div className="w-5 h-5 rounded-full border-2 border-red-600 flex items-center justify-center">
                            <div className="w-2.5 h-2.5 rounded-full bg-red-600" />
                          </div>
                        ) : (
                          <div className="w-5 h-5 rounded-full border-2 border-red-950/70" />
                        )}
                      </div>
                    </div>

                    {/* Method 2: Ví Điện Tử MoMo / ShopeePay */}
                    <div
                      onClick={() => setPaymentMethod('momo')}
                      className={`p-4 sm:p-5 rounded-2xl flex items-center justify-between cursor-pointer transition-all border ${
                        paymentMethod === 'momo'
                          ? 'border-red-600 bg-[#161217]'
                          : 'border-zinc-800/90 hover:border-zinc-700 bg-[#121620]'
                      }`}
                    >
                      <div className="flex items-center gap-4">
                        <div className="w-12 h-12 rounded-xl bg-[#1e1912] border border-amber-500/40 flex items-center justify-center shrink-0 text-amber-400 shadow-inner">
                          <span className="material-symbols-outlined text-[24px]">
                            payments
                          </span>
                        </div>
                        <div>
                          <div className="font-bold text-white text-base">
                            Ví Điện Tử MoMo / ShopeePay
                          </div>
                          <div className="text-xs text-zinc-400 mt-0.5">
                            Xác nhận tức thì qua ứng dụng ví
                          </div>
                        </div>
                      </div>

                      {/* Radio Indicator */}
                      <div className="shrink-0 pl-3">
                        {paymentMethod === 'momo' ? (
                          <div className="w-5 h-5 rounded-full border-2 border-red-600 flex items-center justify-center">
                            <div className="w-2.5 h-2.5 rounded-full bg-red-600" />
                          </div>
                        ) : (
                          <div className="w-5 h-5 rounded-full border-2 border-red-950/70" />
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Promo / Voucher Code Row */}
                <div className="space-y-2 pt-1">
                  <div className="flex gap-3">
                    <input
                      type="text"
                      value={promoCode}
                      onChange={(e) => setPromoCode(e.target.value)}
                      placeholder="CINETICKET"
                      className="flex-1 bg-[#121620] border border-zinc-800 focus:border-zinc-600 rounded-xl px-4 py-3 text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                    />
                    <button
                      type="button"
                      onClick={handleApplyPromo}
                      className="bg-[#202534] hover:bg-[#2b3246] text-white font-medium px-6 py-3 rounded-xl text-sm transition-colors cursor-pointer shrink-0"
                    >
                      Áp dụng
                    </button>
                  </div>
                  {promoMessage && (
                    <div
                      className={`text-xs ${
                        appliedPromo ? 'text-emerald-400' : 'text-zinc-400'
                      }`}
                    >
                      {promoMessage}
                    </div>
                  )}
                </div>
              </div>

              {/* Right Column: Order Bill Summary */}
              <div className="lg:col-span-5">
                <div className="rounded-2xl bg-[#11141e] border border-zinc-800/80 p-6 sm:p-7 shadow-2xl flex flex-col justify-between">
                  <div>
                    <h2 className="font-headline text-lg sm:text-xl font-bold text-white mb-6">
                      Tóm Tắt Đơn Vé
                    </h2>

                    <div className="space-y-4 text-sm">
                      {/* Row: Phim */}
                      <div className="flex justify-between items-start gap-4">
                        <span className="text-zinc-400 shrink-0">Phim:</span>
                        <span className="font-bold text-white text-right">
                          {bookingData.movieTitle}
                        </span>
                      </div>

                      {/* Row: Rạp */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Rạp:</span>
                        <span className="font-medium text-white text-right">
                          {bookingData.theaterName}
                        </span>
                      </div>

                      {/* Row: Suất chiếu */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Suất chiếu:</span>
                        <span className="font-bold text-amber-400 text-right">
                          {bookingData.time} • {bookingData.date}
                        </span>
                      </div>

                      {/* Row: Phòng chiếu */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Phòng chiếu:</span>
                        <span className="font-medium text-white text-right">
                          {bookingData.room}
                        </span>
                      </div>

                      {/* Row: Ghế đã chọn */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Ghế đã chọn:</span>
                        <span className="font-bold text-white text-right">
                          {bookingData.selectedSeats.join(', ')} ({selectedCount} ghế)
                        </span>
                      </div>

                      {/* Row: Đơn giá */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Đơn giá:</span>
                        <span className="font-medium text-white text-right">
                          {bookingData.pricePerSeat.toLocaleString('vi-VN')}đ / ghế
                        </span>
                      </div>

                      {/* Row: Tổng tiền vé */}
                      <div className="flex justify-between items-center gap-4">
                        <span className="text-zinc-400">Tổng tiền vé:</span>
                        <span className="font-bold text-white text-right">
                          {rawTotal.toLocaleString('vi-VN')}đ
                        </span>
                      </div>

                      {appliedPromo && (
                        <div className="flex justify-between items-center gap-4">
                          <span className="text-emerald-400">Ưu đãi ({appliedPromo}):</span>
                          <span className="font-bold text-emerald-400 text-right">
                            -{discountAmount.toLocaleString('vi-VN')}đ
                          </span>
                        </div>
                      )}
                    </div>
                  </div>

                  {/* Summary Total & Confirm Action */}
                  <div className="pt-6 mt-6 border-t border-zinc-800/80 space-y-4">
                    <div className="flex justify-between items-baseline">
                      <span className="text-base font-bold text-white">Tổng thanh toán:</span>
                      <span className="font-headline text-2xl sm:text-3xl font-extrabold text-amber-400">
                        {finalTotal.toLocaleString('vi-VN')}đ
                      </span>
                    </div>

                    <button
                      type="button"
                      disabled={isProcessing}
                      onClick={handleConfirmPayment}
                      className="w-full bg-[#e50914] hover:bg-[#f40612] disabled:opacity-50 text-white font-bold py-4 px-6 rounded-2xl flex items-center justify-center gap-2 text-base transition-all duration-200 shadow-lg shadow-red-900/30 active:scale-[0.99] cursor-pointer"
                    >
                      <span className="material-symbols-outlined text-[20px]">
                        {isProcessing ? 'hourglass_top' : 'lock'}
                      </span>
                      <span>
                        {isProcessing ? 'Đang Xử Lý Đặt Vé...' : 'Xác Nhận & Thanh Toán'}
                      </span>
                    </button>

                    <p className="text-xs text-zinc-400 text-center flex items-center justify-center gap-1.5 pt-1">
                      <span>Bảo mật chuẩn PCI-DSS 256-bit</span>
                      <span>•</span>
                      <span>Hỗ trợ hoàn hủy trước 120 phút</span>
                    </p>
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
