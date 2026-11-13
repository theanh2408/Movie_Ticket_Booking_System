import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import { useAuth } from '../../context/AuthContext';

export default function Profile() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const [bookings, setBookings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState('bookings'); // 'bookings' or 'info'
  const [cancelMessage, setCancelMessage] = useState('');

  useEffect(() => {
    if (!user) {
      navigate('/login');
      return;
    }
    fetchBookingHistory();
  }, [user]);

  const fetchBookingHistory = async () => {
    if (!user?.customerId) {
      setLoading(false);
      return;
    }
    try {
      setLoading(true);
      const res = await axiosClient.get(`/customers/${user.customerId}/bookings/history`);
      if (res.success && res.data) {
        setBookings(res.data);
      }
    } catch (err) {
      console.error('Lỗi lấy lịch sử đặt vé:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCancelBooking = async (bookingId) => {
    if (!confirm('Bạn có chắc chắn muốn hủy đơn đặt vé này không?')) return;
    try {
      setCancelMessage('');
      const res = await axiosClient.post(`/customers/${user.customerId}/bookings/${bookingId}/cancel`);
      if (res.success) {
        setCancelMessage('Hủy đơn đặt vé thành công!');
        fetchBookingHistory();
      }
    } catch (err) {
      const msg = err.response?.data?.message || 'Không thể hủy vé (Chỉ được hủy trong 30 phút sau khi thanh toán).';
      setCancelMessage(msg);
    }
  };

  return (
    <div className="max-w-5xl mx-auto px-4 py-10">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
        <div>
          <h1 className="text-3xl font-black text-white">Tài Khoản Của Tôi</h1>
          <p className="text-slate-400 text-sm mt-1">Quản lý thông tin cá nhân và lịch sử đặt vé xem phim</p>
        </div>

        <div className="flex gap-2 bg-slate-800 p-1 rounded-xl self-start sm:self-auto border border-slate-700">
          <button
            onClick={() => setActiveTab('bookings')}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition ${
              activeTab === 'bookings'
                ? 'bg-red-600 text-white shadow'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            🎟️ Lịch Sử Vé Đã Mua
          </button>
          <button
            onClick={() => setActiveTab('info')}
            className={`px-4 py-2 rounded-lg text-sm font-semibold transition ${
              activeTab === 'info'
                ? 'bg-red-600 text-white shadow'
                : 'text-slate-400 hover:text-white'
            }`}
          >
            👤 Thông Tin Cá Nhân
          </button>
        </div>
      </div>

      {cancelMessage && (
        <div className="mb-6 bg-amber-500/20 border border-amber-500/40 text-amber-300 px-4 py-3 rounded-lg text-sm">
          ℹ️ {cancelMessage}
        </div>
      )}

      {activeTab === 'bookings' ? (
        <div className="space-y-6">
          {loading ? (
            <div className="py-20 text-center text-slate-400">
              <div className="animate-spin text-3xl mb-2">⌛</div>
              <p>Đang tải lịch sử vé...</p>
            </div>
          ) : bookings.length === 0 ? (
            <div className="text-center py-16 bg-slate-800/40 border border-dashed border-slate-700 rounded-2xl">
              <p className="text-slate-400 text-lg mb-2">Bạn chưa có đơn đặt vé nào.</p>
              <button
                onClick={() => navigate('/')}
                className="bg-red-600 hover:bg-red-500 text-white text-xs font-semibold px-4 py-2 rounded-lg transition"
              >
                Khám Phá Phim Ngay
              </button>
            </div>
          ) : (
            bookings.map((b) => (
              <div
                key={b.bookingId}
                className="bg-slate-900 border border-slate-800 hover:border-slate-700 rounded-2xl p-6 transition shadow-lg"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-800/80 pb-4 mb-4 gap-2">
                  <div>
                    <span className="text-xs text-slate-500 block">Mã đơn vé:</span>
                    <span className="font-mono font-bold text-white text-base">#{b.bookingId}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-xs text-slate-400">
                      Ngày đặt: {b.createdDate ? new Date(b.createdDate).toLocaleDateString('vi-VN') : ''}
                    </span>
                    <span
                      className={`px-2.5 py-0.5 rounded-full text-xs font-bold ${
                        b.status === 'PAID'
                          ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                          : b.status === 'CANCELLED'
                          ? 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                          : 'bg-amber-500/20 text-amber-400 border border-amber-500/30'
                      }`}
                    >
                      {b.status}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  <div className="md:col-span-2 space-y-2">
                    <h3 className="font-bold text-lg text-white">
                      {b.tickets?.[0]?.movieTitle || 'Vé Xem Phim'}
                    </h3>
                    <div className="text-xs text-slate-300 space-y-1">
                      <p>
                        <span className="text-slate-500">Số lượng vé:</span> {b.tickets?.length || 0} vé
                      </p>
                      <p>
                        <span className="text-slate-500">Tổng tiền:</span>{' '}
                        <span className="font-bold text-amber-400 text-sm">
                          {b.totalAmount ? b.totalAmount.toLocaleString() : 0} đ
                        </span>
                      </p>
                    </div>

                    {b.status === 'PAID' && (
                      <div className="pt-2">
                        <button
                          onClick={() => handleCancelBooking(b.bookingId)}
                          className="text-xs text-rose-400 hover:text-rose-300 underline"
                        >
                          Hủy đơn vé này
                        </button>
                      </div>
                    )}
                  </div>

                  {/* QR Token box */}
                  <div className="bg-slate-950 p-4 rounded-xl border border-slate-800 text-center flex flex-col items-center justify-center">
                    <div className="text-xs text-slate-400 mb-1">MÃ QR CHECK-IN</div>
                    <div className="font-mono text-xs font-bold text-amber-400 break-all bg-slate-900 px-3 py-2 rounded border border-slate-700 w-full">
                      {b.qrToken || 'CHƯA CÓ'}
                    </div>
                    <div className="text-[10px] text-slate-500 mt-2">Đưa mã này cho nhân viên soát vé</div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      ) : (
        /* Tab thông tin cá nhân */
        <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 sm:p-8 max-w-xl mx-auto space-y-4">
          <h2 className="text-xl font-bold text-white border-b border-slate-800 pb-3">Thông Tin Tài Khoản</h2>
          <div className="space-y-3 text-sm">
            <div>
              <span className="text-slate-500 block text-xs">Họ và tên:</span>
              <span className="text-white font-medium">{user?.fullName}</span>
            </div>
            <div>
              <span className="text-slate-500 block text-xs">Email:</span>
              <span className="text-white font-medium">{user?.email}</span>
            </div>
            <div>
              <span className="text-slate-500 block text-xs">Số điện thoại:</span>
              <span className="text-white font-medium">{user?.phone || 'Chưa cập nhật'}</span>
            </div>
            <div>
              <span className="text-slate-500 block text-xs">Vai trò hệ thống:</span>
              <span className="inline-block mt-0.5 px-2 py-0.5 rounded text-xs font-bold bg-slate-800 text-slate-300 border border-slate-700">
                {user?.role}
              </span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
