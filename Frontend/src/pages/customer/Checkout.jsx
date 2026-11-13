import { useState, useEffect } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import { useAuth } from '../../context/AuthContext';

export default function Checkout() {
  const location = useLocation();
  const navigate = useNavigate();
  const { user } = useAuth();

  const bookingData = location.state;
  const [promoCode, setPromoCode] = useState('');
  const [promoDiscount, setPromoDiscount] = useState(0);
  const [promoMessage, setPromoMessage] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('MOCK_QR');
  const [submitting, setSubmitting] = useState(false);
  const [paymentSuccess, setPaymentSuccess] = useState(null);
  const [errorMessage, setErrorMessage] = useState('');

  // Countdown timer
  const [timeLeft, setTimeLeft] = useState(() => {
    return bookingData?.attempt?.remainingSeconds || 600;
  });

  useEffect(() => {
    if (!bookingData) {
      navigate('/');
      return;
    }

    const timer = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          alert('Đã hết thời gian giữ ghế! Vui lòng chọn lại ghế.');
          navigate('/');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [bookingData, navigate]);

  if (!bookingData) {
    return null;
  }

  const { attempt, showtime, selectedSeats, totalPrice } = bookingData;
  const finalAmount = Math.max(0, totalPrice - promoDiscount);

  const formatTime = (secs) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const handleApplyPromo = async () => {
    if (!promoCode.trim()) return;
    try {
      setPromoMessage('');
      setErrorMessage('');

      // Cố gắng gọi API khuyến mãi nếu user có customerId
      if (user?.customerId) {
        const res = await axiosClient.post(`/customers/${user.customerId}/bookings/${attempt.paymentAttemptId}/promotion`, {
          code: promoCode.trim(),
        });
        if (res.success && res.data) {
          setPromoDiscount(res.data.discountAmount || 0);
          setPromoMessage('Áp dụng mã giảm giá thành công!');
          return;
        }
      }

      // Giả lập trực tiếp nếu API báo khác
      if (promoCode.toUpperCase() === 'CHAOBANMOI') {
        const discount = Math.min(totalPrice * 0.2, 50000);
        setPromoDiscount(discount);
        setPromoMessage('Đã áp dụng mã CHAOBANMOI (-20%)');
      } else if (promoCode.toUpperCase() === 'GIAM30K') {
        setPromoDiscount(30000);
        setPromoMessage('Đã áp dụng mã GIAM30K (-30.000đ)');
      } else {
        setPromoMessage('Mã khuyến mãi không hợp lệ hoặc đã hết hạn.');
      }
    } catch (err) {
      const msg = err.response?.data?.message || 'Không thể áp dụng mã khuyến mãi.';
      setPromoMessage(msg);
    }
  };

  const handleCompletePayment = async () => {
    try {
      setSubmitting(true);
      setErrorMessage('');

      const res = await axiosClient.post(`/payments/mock/${attempt.paymentAttemptId}/complete`, {
        result: 'SUCCESS',
      });

      if (res.success && res.data) {
        setPaymentSuccess(res.data);
      }
    } catch (err) {
      const msg = err.response?.data?.message || 'Thanh toán thất bại. Vui lòng thử lại!';
      setErrorMessage(msg);
    } finally {
      setSubmitting(false);
    }
  };

  // Màn hình Thanh toán thành công (Vé xem phim)
  if (paymentSuccess) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-12">
        <div className="bg-slate-900 border border-emerald-500/40 rounded-2xl p-8 text-center shadow-2xl relative overflow-hidden">
          <div className="w-16 h-16 bg-emerald-500/20 text-emerald-400 rounded-full flex items-center justify-center text-3xl mx-auto mb-4 border border-emerald-500/40">
            ✓
          </div>

          <span className="text-xs font-bold text-emerald-400 uppercase tracking-widest">
            Thanh Toán Thành Công
          </span>
          <h1 className="text-2xl sm:text-3xl font-black text-white mt-1 mb-2">
            Vé Xem Phim Của Bạn Đã Sẵn Sàng!
          </h1>
          <p className="text-slate-400 text-sm mb-6">
            Mã đơn đặt vé: <span className="font-mono text-white font-bold">#{paymentSuccess.paymentAttemptId}</span>
          </p>

          {/* Ticket Card */}
          <div className="bg-slate-950 border border-slate-800 rounded-xl p-6 text-left space-y-4 mb-6 relative">
            <div className="border-b border-slate-800 pb-3">
              <h2 className="text-xl font-bold text-white">{showtime?.movieTitle}</h2>
              <p className="text-xs text-red-400 font-medium mt-0.5">
                {showtime?.cinemaName} • {showtime?.roomName}
              </p>
            </div>

            <div className="grid grid-cols-2 gap-4 text-xs text-slate-300">
              <div>
                <span className="text-slate-500 block">Thời gian:</span>
                <span className="font-semibold text-white">
                  {showtime?.startTime ? new Date(showtime.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}{' '}
                  - {showtime?.startTime ? new Date(showtime.startTime).toLocaleDateString('vi-VN') : ''}
                </span>
              </div>
              <div>
                <span className="text-slate-500 block">Ghế ngồi:</span>
                <span className="font-semibold text-amber-400 text-sm">
                  {selectedSeats.map((s) => s.seatNumber).join(', ')}
                </span>
              </div>
              <div>
                <span className="text-slate-500 block">Tổng tiền thanh toán:</span>
                <span className="font-semibold text-white">
                  {paymentSuccess.amount ? paymentSuccess.amount.toLocaleString() : finalAmount.toLocaleString()} đ
                </span>
              </div>
              <div>
                <span className="text-slate-500 block">Trạng thái:</span>
                <span className="font-semibold text-emerald-400">ĐÃ XÁC NHẬN (PAID)</span>
              </div>
            </div>

            {/* QR Token & Barcode */}
            <div className="pt-4 border-t border-slate-800/80 flex flex-col items-center justify-center gap-2">
              <div className="bg-white p-3 rounded-lg shadow-md text-slate-950 font-mono text-center">
                <div className="text-xs text-slate-500 mb-1">MÃ QR CHECK-IN TẠI QUẦY</div>
                <div className="font-bold text-sm tracking-wider break-all max-w-[280px]">
                  {paymentSuccess.qrToken || `BOOKING-${paymentSuccess.paymentAttemptId}`}
                </div>
              </div>
              <p className="text-[11px] text-slate-500 text-center">
                Vui lòng cung cấp mã này cho nhân viên tại rạp để in vé cứng.
              </p>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row gap-3 justify-center">
            <Link
              to="/profile"
              className="bg-red-600 hover:bg-red-500 text-white font-bold px-6 py-2.5 rounded-xl transition text-sm shadow-md"
            >
              Xem Lịch Sử Vé Đã Đặt
            </Link>
            <Link
              to="/"
              className="bg-slate-800 hover:bg-slate-700 text-slate-300 font-medium px-6 py-2.5 rounded-xl transition text-sm border border-slate-700"
            >
              Về Trang Chủ
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Top Countdown bar */}
      <div className="bg-amber-500/10 border border-amber-500/30 rounded-xl p-4 mb-8 flex items-center justify-between">
        <div className="flex items-center gap-2 text-amber-300 text-sm font-medium">
          <span>⏳</span>
          <span>Thời gian giữ ghế còn lại:</span>
        </div>
        <div className="text-2xl font-mono font-bold text-amber-400">
          {formatTime(timeLeft)}
        </div>
      </div>

      {errorMessage && (
        <div className="mb-6 bg-red-500/20 border border-red-500/40 text-red-300 px-4 py-3 rounded-lg text-sm">
          ⚠️ {errorMessage}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        {/* Left: Payment Method & Promotions (2 Cols) */}
        <div className="md:col-span-2 space-y-6">
          {/* Promotion Card */}
          <div className="bg-slate-800/80 border border-slate-700 rounded-xl p-5">
            <h3 className="font-bold text-white mb-3 flex items-center gap-2">
              <span>🎁</span> Khuyến Mãi / Giảm Giá
            </h3>
            <div className="flex gap-2">
              <input
                type="text"
                placeholder="Nhập mã (VD: CHAOBANMOI, GIAM30K)"
                value={promoCode}
                onChange={(e) => setPromoCode(e.target.value)}
                className="flex-1 bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500 uppercase font-mono"
              />
              <button
                onClick={handleApplyPromo}
                className="bg-slate-700 hover:bg-slate-600 text-white px-4 py-2 rounded-lg text-sm font-semibold transition"
              >
                Áp Dụng
              </button>
            </div>
            {promoMessage && (
              <p className={`text-xs mt-2 ${promoDiscount > 0 ? 'text-emerald-400' : 'text-amber-400'}`}>
                {promoMessage}
              </p>
            )}
          </div>

          {/* Payment Method Selector */}
          <div className="bg-slate-800/80 border border-slate-700 rounded-xl p-5">
            <h3 className="font-bold text-white mb-3 flex items-center gap-2">
              <span>💳</span> Phương Thức Thanh Toán (Mô Phỏng)
            </h3>
            <div className="space-y-3">
              {[
                { id: 'MOCK_QR', name: 'Quét mã QR / Ví Điện Tử (Mô phỏng)', desc: 'Thanh toán tức thì không cần thẻ' },
                { id: 'MOCK_ATM', name: 'Thẻ ATM Nội Địa / Internet Banking (Mô phỏng)', desc: 'Chấp nhận tất cả ngân hàng' },
                { id: 'MOCK_CARD', name: 'Thẻ Quốc Tế Visa / MasterCard (Mô phỏng)', desc: 'Miễn phí tiện ích' },
              ].map((m) => (
                <label
                  key={m.id}
                  onClick={() => setPaymentMethod(m.id)}
                  className={`flex items-start gap-3 p-3.5 rounded-lg border cursor-pointer transition ${
                    paymentMethod === m.id
                      ? 'bg-red-950/30 border-red-500/80 ring-1 ring-red-500/50'
                      : 'bg-slate-900/60 border-slate-700/80 hover:bg-slate-900'
                  }`}
                >
                  <input
                    type="radio"
                    name="payment"
                    checked={paymentMethod === m.id}
                    onChange={() => setPaymentMethod(m.id)}
                    className="mt-1 accent-red-600"
                  />
                  <div>
                    <div className="text-sm font-semibold text-white">{m.name}</div>
                    <div className="text-xs text-slate-400 mt-0.5">{m.desc}</div>
                  </div>
                </label>
              ))}
            </div>
          </div>
        </div>

        {/* Right: Booking Summary Order Box (1 Col) */}
        <div className="bg-slate-950 border border-slate-800 rounded-xl p-5 h-fit space-y-4">
          <h3 className="font-bold text-white border-b border-slate-800 pb-3">Tóm Tắt Đơn Vé</h3>

          <div>
            <h4 className="font-bold text-base text-white">{showtime?.movieTitle}</h4>
            <p className="text-xs text-slate-400 mt-0.5">
              {showtime?.cinemaName} • {showtime?.roomName}
            </p>
          </div>

          <div className="text-xs text-slate-300 space-y-2 border-y border-slate-800/80 py-3">
            <div className="flex justify-between">
              <span className="text-slate-400">Suất chiếu:</span>
              <span className="font-medium text-white">
                {showtime?.startTime ? new Date(showtime.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Ghế ngồi:</span>
              <span className="font-bold text-amber-400">
                {selectedSeats.map((s) => s.seatNumber).join(', ')}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Tạm tính:</span>
              <span>{totalPrice.toLocaleString()} đ</span>
            </div>
            {promoDiscount > 0 && (
              <div className="flex justify-between text-emerald-400">
                <span>Giảm giá:</span>
                <span>-{promoDiscount.toLocaleString()} đ</span>
              </div>
            )}
          </div>

          <div className="flex justify-between items-baseline pt-1">
            <span className="text-sm font-bold text-slate-300">Tổng thanh toán:</span>
            <span className="text-2xl font-black text-amber-400">
              {finalAmount.toLocaleString()} <span className="text-sm font-normal">đ</span>
            </span>
          </div>

          <button
            disabled={submitting}
            onClick={handleCompletePayment}
            className="w-full bg-red-600 hover:bg-red-500 disabled:bg-slate-800 text-white font-bold py-3.5 rounded-xl shadow-lg shadow-red-600/30 transition text-sm"
          >
            {submitting ? 'Đang xử lý...' : 'Xác Nhận Thanh Toán Mô Phỏng'}
          </button>
        </div>
      </div>
    </div>
  );
}
