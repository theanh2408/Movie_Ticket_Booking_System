import { useState, useEffect } from 'react';
import axiosClient from '../../api/axiosClient';

export default function ManageRooms() {
  const [rooms, setRooms] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchRooms();
  }, []);

  const fetchRooms = async () => {
    try {
      setLoading(true);
      const res = await axiosClient.get('/admin/rooms');
      if (res.success && res.data) {
        setRooms(res.data);
      }
    } catch (err) {
      console.error('Lỗi tải danh sách phòng chiếu:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6 max-w-5xl">
      <div>
        <h1 className="text-2xl font-black text-white">Quản Lý Phòng Chiếu & Cụm Rạp</h1>
        <p className="text-slate-400 text-xs mt-1">Danh sách phòng chiếu và sơ đồ chỗ ngồi của các cụm rạp</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {loading ? (
          <div className="col-span-full py-16 text-center text-slate-500">
            Đang tải thông tin phòng chiếu...
          </div>
        ) : rooms.length === 0 ? (
          <div className="col-span-full py-16 text-center text-slate-500">
            Chưa có phòng chiếu nào được thiết lập.
          </div>
        ) : (
          rooms.map((r) => (
            <div
              key={r.roomId}
              className="bg-slate-900 border border-slate-800 rounded-2xl p-6 shadow-xl space-y-4 hover:border-slate-700 transition"
            >
              <div className="flex items-center justify-between border-b border-slate-800/80 pb-3">
                <span className="text-xs font-semibold text-amber-400 uppercase tracking-wider">
                  {r.cinemaName || 'CGV Cinema'}
                </span>
                <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                  {r.status}
                </span>
              </div>

              <div>
                <h3 className="text-lg font-bold text-white">{r.roomName}</h3>
                <p className="text-xs text-slate-400 mt-1">Sức chứa tối đa: {r.totalSeats || 40} ghế ngồi tiêu chuẩn</p>
              </div>

              <div className="bg-slate-950 p-4 rounded-xl border border-slate-800/80 text-xs text-slate-400 space-y-1">
                <div className="flex justify-between">
                  <span>Ma trận ghế:</span>
                  <span className="font-semibold text-slate-200">5 Hàng x 8 Cột (A - E)</span>
                </div>
                <div className="flex justify-between">
                  <span>Hệ thống âm thanh:</span>
                  <span className="font-semibold text-slate-200">Dolby Atmos 7.1</span>
                </div>
                <div className="flex justify-between">
                  <span>Màn chiếu:</span>
                  <span className="font-semibold text-slate-200">Curved Silver Screen</span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
