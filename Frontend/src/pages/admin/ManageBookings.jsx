import { useState } from 'react';
import axiosClient from '../../api/axiosClient';

export default function ManageBookings() {
  const [tokenInput, setTokenInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [bookingResult, setBookingResult] = useState(null);
  const [printLoading, setPrintLoading] = useState(false);
  const [printSuccess, setPrintSuccess] = useState(null);
  const [errorMsg, setErrorMsg] = useState('');

  const handleScanQr = async (e) => {
    e.preventDefault();
    if (!tokenInput.trim()) return;

    try {
      setLoading(true);
      setErrorMsg('');
      setBookingResult(null);
      setPrintSuccess(null);

      const res = await axiosClient.get(`/ticket-checking/qr/${encodeURIComponent(tokenInput.trim())}`);
      if (res.success && res.data) {
        setBookingResult(res.data);
      }
    } catch (err) {
      setErrorMsg(err.response?.data?.message || 'Không tìm thấy QR Token hoặc mã vé không hợp lệ.');
    } finally {
      setLoading(false);
    }
  };

  const handleConfirmPrint = async () => {
    if (!tokenInput.trim()) return;
    try {
      setPrintLoading(true);
      setErrorMsg('');

      const res = await axiosClient.post(`/ticket-checking/qr/${encodeURIComponent(tokenInput.trim())}/print`);
      if (res.success && res.data) {
        setPrintSuccess(res.data);
        // Refresh lại dữ liệu
        setBookingResult((prev) => (prev ? { ...prev, qrStatus: 'Revoked', status: 'PRINTED' } : null));
      }
    } catch (err) {
      setErrorMsg(err.response?.data?.message || 'Không thể xác nhận in vé.');
    } finally {
      setPrintLoading(false);
    }
  };

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-black text-white">Soát Vé & Quét Mã QR Tại Quầy</h1>
        <p className="text-slate-400 text-xs mt-1">
          Nhân viên nhập hoặc quét mã QR Token từ đơn đặt vé của khách để kiểm tra và in vé cứng
        </p>
      </div>

      {/* QR Input Card */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl">
        <form onSubmit={handleScanQr} className="flex flex-col sm:flex-row gap-3">
          <input
            type="text"
            required
            placeholder="Nhập mã QR Token (VD: BOOKING-f2c9...)"
            value={tokenInput}
            onChange={(e) => setTokenInput(e.target.value)}
            className="flex-1 bg-slate-950 border border-slate-700 rounded-xl px-4 py-3 text-sm text-white placeholder-slate-500 font-mono focus:outline-none focus:border-red-500"
          />
          <button
            type="submit"
            disabled={loading}
            className="bg-red-600 hover:bg-red-500 disabled:bg-slate-800 text-white font-bold px-6 py-3 rounded-xl transition text-sm flex items-center justify-center gap-2 shadow-lg shadow-red-600/30"
          >
            {loading ? 'Đang kiểm tra...' : '🔍 Kiểm Tra Vé'}
          </button>
        </form>

        {errorMsg && (
          <div className="mt-4 bg-red-500/20 border border-red-500/40 text-red-300 px-4 py-3 rounded-xl text-xs">
            ⚠️ {errorMsg}
          </div>
        )}

        {printSuccess && (
          <div className="mt-4 bg-emerald-500/20 border border-emerald-500/40 text-emerald-300 px-4 py-3 rounded-xl text-xs font-semibold">
            ✓ In vé thành công! Mã QR Token đã được chuyển sang trạng thái hết hiệu lực (Revoked).
          </div>
        )}
      </div>

      {/* QR Scan Result Box */}
      {bookingResult && (
        <div className="bg-slate-900 border border-slate-700 rounded-2xl p-6 shadow-2xl space-y-6">
          <div className="flex items-center justify-between border-b border-slate-800 pb-4">
            <div>
              <span className="text-xs text-slate-500 block">Đơn vé</span>
              <span className="text-xl font-black text-white">#{bookingResult.bookingId}</span>
            </div>
            <div className="text-right">
              <span className="text-xs text-slate-500 block">Trạng thái QR</span>
              <span
                className={`px-3 py-1 rounded-full text-xs font-bold ${
                  bookingResult.qrStatus === 'Active'
                    ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                    : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                }`}
              >
                {bookingResult.qrStatus}
              </span>
            </div>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-xs">
            <div>
              <span className="text-slate-500 block">Khách hàng:</span>
              <span className="font-semibold text-white text-sm">{bookingResult.customerName || 'Khách Vãng Lai'}</span>
            </div>
            <div>
              <span className="text-slate-500 block">Số điện thoại:</span>
              <span className="font-semibold text-white">{bookingResult.phone || 'N/A'}</span>
            </div>
            <div>
              <span className="text-slate-500 block">Suất chiếu:</span>
              <span className="font-semibold text-amber-400">{bookingResult.showtime || '10:00 Hôm nay'}</span>
            </div>
            <div>
              <span className="text-slate-500 block">Phòng chiếu:</span>
              <span className="font-semibold text-white">{bookingResult.room || 'Phòng 01'}</span>
            </div>
            <div>
              <span className="text-slate-500 block">Ghế ngồi:</span>
              <span className="font-bold text-amber-400 text-sm">
                {Array.isArray(bookingResult.seats) ? bookingResult.seats.join(', ') : bookingResult.seats || 'A1, A2'}
              </span>
            </div>
            <div>
              <span className="text-slate-500 block">Tổng tiền:</span>
              <span className="font-bold text-white text-sm">
                {bookingResult.totalAmount ? bookingResult.totalAmount.toLocaleString() : 0} đ
              </span>
            </div>
          </div>

          {/* Action button */}
          <div className="pt-4 border-t border-slate-800 flex justify-end">
            <button
              onClick={handleConfirmPrint}
              disabled={bookingResult.qrStatus !== 'Active' || printLoading}
              className="bg-emerald-600 hover:bg-emerald-500 disabled:bg-slate-800 disabled:text-slate-600 text-white font-bold px-8 py-3 rounded-xl transition shadow-lg shadow-emerald-600/30 text-sm flex items-center gap-2"
            >
              {printLoading ? 'Đang in...' : '🖨️ Xác Nhận & In Vé Cho Khách'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
