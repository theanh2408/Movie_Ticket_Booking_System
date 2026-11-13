import { useState, useEffect } from 'react';
import axiosClient from '../../api/axiosClient';

export default function ManageShowtimes() {
  const [showtimes, setShowtimes] = useState([]);
  const [movies, setMovies] = useState([]);
  const [rooms, setRooms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [alertMsg, setAlertMsg] = useState('');

  const [formData, setFormData] = useState({
    movieId: '',
    roomId: '',
    date: new Date().toISOString().split('T')[0],
    startHour: '10:00',
    basePrice: 90000,
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [stRes, movRes, roomRes] = await Promise.all([
        axiosClient.get('/admin/showtimes'),
        axiosClient.get('/admin/movies'),
        axiosClient.get('/admin/rooms'),
      ]);

      if (stRes.success && stRes.data) setShowtimes(stRes.data);
      if (movRes.success && movRes.data) {
        setMovies(movRes.data);
        if (movRes.data.length > 0) setFormData((prev) => ({ ...prev, movieId: movRes.data[0].movieId }));
      }
      if (roomRes.success && roomRes.data) {
        setRooms(roomRes.data);
        if (roomRes.data.length > 0) setFormData((prev) => ({ ...prev, roomId: roomRes.data[0].roomId }));
      }
    } catch (err) {
      console.error('Lỗi tải dữ liệu suất chiếu:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateShowtime = async (e) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      setAlertMsg('');

      const payload = {
        movieId: parseInt(formData.movieId),
        roomId: parseInt(formData.roomId),
        date: formData.date,
        startHour: `${formData.startHour}:00`,
        basePrice: parseFloat(formData.basePrice),
      };

      const res = await axiosClient.post('/admin/showtimes', payload);
      if (res.success) {
        setAlertMsg('Tạo suất chiếu và sinh sơ đồ ghế thành công!');
        setShowModal(false);
        fetchData();
      }
    } catch (err) {
      alert(err.response?.data?.message || err.message || 'Lỗi khi tạo suất chiếu.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleCancelShowtime = async (id) => {
    if (!confirm('Bạn có chắc muốn hủy suất chiếu này?')) return;
    try {
      const res = await axiosClient.delete(`/admin/showtimes/${id}`);
      if (res.success) {
        setAlertMsg('Đã hủy suất chiếu thành công.');
        fetchData();
      }
    } catch (err) {
      alert(err.response?.data?.message || 'Lỗi khi hủy suất chiếu.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-white">Quản Lý Suất Chiếu</h1>
          <p className="text-slate-400 text-xs mt-1">Lập lịch chiếu phim theo phòng và tự động khởi tạo sơ đồ ghế</p>
        </div>

        <button
          onClick={() => setShowModal(true)}
          className="bg-red-600 hover:bg-red-500 text-white font-bold px-4 py-2.5 rounded-xl shadow-lg shadow-red-600/30 transition text-sm flex items-center gap-2 self-start sm:self-auto"
        >
          <span>➕</span> Tạo Suất Chiếu Mới
        </button>
      </div>

      {alertMsg && (
        <div className="bg-emerald-500/20 border border-emerald-500/40 text-emerald-300 px-4 py-2.5 rounded-lg text-xs">
          ✓ {alertMsg}
        </div>
      )}

      {/* Showtimes Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-300">
            <thead className="bg-slate-950 text-xs uppercase text-slate-400 border-b border-slate-800 font-semibold tracking-wider">
              <tr>
                <th className="px-5 py-4">Phim</th>
                <th className="px-4 py-4">Rạp & Phòng Chiếu</th>
                <th className="px-4 py-4">Ngày chiếu</th>
                <th className="px-4 py-4">Giờ chiếu</th>
                <th className="px-4 py-4">Giá vé cơ bản</th>
                <th className="px-4 py-4">Trạng thái</th>
                <th className="px-5 py-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/80">
              {loading ? (
                <tr>
                  <td colSpan="7" className="text-center py-12 text-slate-500">
                    Đang tải lịch chiếu...
                  </td>
                </tr>
              ) : showtimes.length === 0 ? (
                <tr>
                  <td colSpan="7" className="text-center py-12 text-slate-500">
                    Chưa có suất chiếu nào được tạo.
                  </td>
                </tr>
              ) : (
                showtimes.map((st) => (
                  <tr key={st.showtimeId} className="hover:bg-slate-800/40 transition">
                    <td className="px-5 py-3.5 font-bold text-white text-sm">
                      {st.movieTitle || `Phim #${st.movieId}`}
                    </td>
                    <td className="px-4 py-3.5 text-xs">
                      <div className="text-amber-400 font-medium">{st.cinemaName || 'CGV'}</div>
                      <div className="text-slate-500">{st.roomName || 'Phòng 01'}</div>
                    </td>
                    <td className="px-4 py-3.5 text-xs text-slate-300">
                      {st.date || 'Hôm nay'}
                    </td>
                    <td className="px-4 py-3.5 font-mono font-bold text-white text-sm">
                      {st.startHour || '10:00'}
                    </td>
                    <td className="px-4 py-3.5 font-semibold text-amber-400 text-xs">
                      {st.basePrice ? `${st.basePrice.toLocaleString()} đ` : '85.000 đ'}
                    </td>
                    <td className="px-4 py-3.5">
                      <span
                        className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                          st.status === 'ACTIVE'
                            ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                            : 'bg-rose-500/20 text-rose-400 border border-rose-500/30'
                        }`}
                      >
                        {st.status === 'ACTIVE' ? 'Hoạt động' : 'Đã hủy'}
                      </span>
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      {st.status === 'ACTIVE' && (
                        <button
                          onClick={() => handleCancelShowtime(st.showtimeId)}
                          className="text-xs bg-rose-950/60 hover:bg-rose-900 text-rose-300 px-2.5 py-1 rounded transition border border-rose-800/40"
                        >
                          Hủy Suất
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Tạo Suất Chiếu */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-md p-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
              <h2 className="text-lg font-bold text-white">Tạo Suất Chiếu Mới</h2>
              <button onClick={() => setShowModal(false)} className="text-slate-400 hover:text-white font-bold">
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateShowtime} className="space-y-4 text-xs">
              <div>
                <label className="block text-slate-300 font-semibold mb-1">Chọn Phim Chiếu *</label>
                <select
                  required
                  value={formData.movieId}
                  onChange={(e) => setFormData({ ...formData, movieId: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                >
                  {movies.map((m) => (
                    <option key={m.movieId} value={m.movieId}>
                      {m.title} ({m.duration}p)
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Chọn Phòng Chiếu (Thuộc Rạp) *</label>
                <select
                  required
                  value={formData.roomId}
                  onChange={(e) => setFormData({ ...formData, roomId: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                >
                  {rooms.map((r) => (
                    <option key={r.roomId} value={r.roomId}>
                      {r.cinemaName} - {r.roomName} ({r.totalSeats} ghế)
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Ngày chiếu *</label>
                  <input
                    type="date"
                    required
                    value={formData.date}
                    onChange={(e) => setFormData({ ...formData, date: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  />
                </div>
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Giờ bắt đầu *</label>
                  <input
                    type="time"
                    required
                    value={formData.startHour}
                    onChange={(e) => setFormData({ ...formData, startHour: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Giá vé cơ bản (VNĐ) *</label>
                <input
                  type="number"
                  required
                  min="20000"
                  step="5000"
                  value={formData.basePrice}
                  onChange={(e) => setFormData({ ...formData, basePrice: parseInt(e.target.value) || 0 })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                />
              </div>

              <div className="flex justify-end gap-2 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 bg-slate-800 text-slate-300 rounded-lg hover:bg-slate-700 transition"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 bg-red-600 hover:bg-red-500 text-white font-bold rounded-lg shadow-md transition"
                >
                  {submitting ? 'Đang tạo...' : 'Khởi Tạo Suất'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
