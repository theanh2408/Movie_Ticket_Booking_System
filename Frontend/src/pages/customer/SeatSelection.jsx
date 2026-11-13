import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import axiosClient from '../../api/axiosClient';
import { useAuth } from '../../context/AuthContext';

export default function SeatSelection() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [showtime, setShowtime] = useState(null);
  const [seats, setSeats] = useState([]);
  const [selectedSeatIds, setSelectedSeatIds] = useState([]);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    fetchShowtimeAndSeats();
  }, [id]);

  const fetchShowtimeAndSeats = async () => {
    try {
      setLoading(true);
      const [stRes, seatsRes] = await Promise.all([
        axiosClient.get(`/showtimes/${id}`),
        axiosClient.get(`/showtimes/${id}/seats`),
      ]);

      if (stRes.success && stRes.data) {
        setShowtime(stRes.data);
      }
      if (seatsRes.success && seatsRes.data?.seats) {
        setSeats(seatsRes.data.seats);
      }
    } catch (err) {
      console.error('Lỗi tải dữ liệu suất chiếu/ghế:', err);
      setErrorMessage('Không thể tải thông tin ghế của suất chiếu.');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleSeat = (seat) => {
    if (seat.status !== 'AVAILABLE') return;

    if (selectedSeatIds.includes(seat.seatId)) {
      setSelectedSeatIds(selectedSeatIds.filter((sid) => sid !== seat.seatId));
    } else {
      if (selectedSeatIds.length >= 8) {
        alert('Bạn chỉ có thể chọn tối đa 8 ghế trong một lần đặt!');
        return;
      }
      setSelectedSeatIds([...selectedSeatIds, seat.seatId]);
    }
  };

  const selectedSeats = seats.filter((s) => selectedSeatIds.includes(s.seatId));
  const totalPrice = selectedSeats.reduce((sum, s) => sum + (s.price || showtime?.basePrice || 0), 0);

  const handleProceed = async () => {
    if (!user) {
      alert('Vui lòng đăng nhập để tiến hành đặt vé!');
      navigate('/login', { state: { from: `/showtime/${id}/seats` } });
      return;
    }

    if (selectedSeatIds.length === 0) {
      alert('Vui lòng chọn ít nhất một ghế!');
      return;
    }

    try {
      setSubmitting(true);
      setErrorMessage('');

      // Gọi API giữ ghế (create payment attempt)
      const res = await axiosClient.post('/payments/mock', {
        showtimeId: parseInt(id),
        seatIds: selectedSeatIds,
      });

      if (res.success && res.data) {
        // Lưu tạm thông tin đơn và chuyển hướng đến trang thanh toán
        const attempt = res.data;
        navigate('/checkout', {
          state: {
            attempt,
            showtime,
            selectedSeats,
            totalPrice,
          },
        });
      }
    } catch (err) {
      const msg = err.response?.data?.message || 'Không thể giữ ghế. Vui lòng thử lại!';
      setErrorMessage(msg);
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-5xl mx-auto px-4 py-20 text-center text-slate-400">
        <div className="animate-spin text-4xl mb-4">⌛</div>
        <p>Đang tải sơ đồ phòng chiếu...</p>
      </div>
    );
  }

  // Nhóm ghế theo Hàng (Row A, B, C...)
  const seatsByRow = seats.reduce((acc, seat) => {
    const row = seat.seatNumber ? seat.seatNumber[0] : 'A';
    if (!acc[row]) acc[row] = [];
    acc[row].push(seat);
    return acc;
  }, {});

  return (
    <div className="max-w-5xl mx-auto px-4 py-8">
      {/* Showtime header info */}
      <div className="bg-slate-800/80 border border-slate-700 rounded-xl p-5 mb-8 flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <span className="text-xs font-semibold text-red-400 uppercase tracking-wide">
            {showtime?.cinemaName} • {showtime?.roomName}
          </span>
          <h1 className="text-2xl font-bold text-white mt-1">{showtime?.movieTitle}</h1>
          <div className="text-sm text-slate-300 mt-1 flex items-center gap-3">
            <span>
              📅 {showtime?.startTime ? new Date(showtime.startTime).toLocaleDateString('vi-VN') : ''}
            </span>
            <span>
              ⏰ {showtime?.startTime ? new Date(showtime.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}
            </span>
          </div>
        </div>

        <Link
          to={`/movie/${showtime?.movieId}`}
          className="text-xs text-slate-400 hover:text-white border border-slate-700 px-3 py-1.5 rounded-lg self-start md:self-auto transition"
        >
          ← Đổi suất chiếu
        </Link>
      </div>

      {errorMessage && (
        <div className="mb-6 bg-red-500/20 border border-red-500/40 text-red-300 px-4 py-3 rounded-lg text-sm">
          ⚠️ {errorMessage}
        </div>
      )}

      {/* Screen Visualization */}
      <div className="mb-12">
        <div className="w-3/4 mx-auto h-3 bg-gradient-to-b from-red-500 to-transparent rounded-t-full opacity-60 shadow-[0_-10px_20px_rgba(239,68,68,0.4)]" />
        <p className="text-center text-xs text-slate-400 mt-2 uppercase tracking-widest font-semibold">
          Màn Hình Chiếu
        </p>
      </div>

      {/* Seat Matrix */}
      <div className="bg-slate-950/60 border border-slate-800 rounded-2xl p-6 sm:p-10 mb-8 overflow-x-auto">
        <div className="min-w-[420px] max-w-xl mx-auto space-y-3">
          {Object.entries(seatsByRow).map(([rowLetter, rowSeats]) => (
            <div key={rowLetter} className="flex items-center justify-center gap-2">
              <span className="w-6 text-center font-bold text-sm text-slate-400">{rowLetter}</span>

              <div className="flex gap-2">
                {rowSeats.map((seat) => {
                  const isSelected = selectedSeatIds.includes(seat.seatId);
                  const isBooked = seat.status === 'BOOKED';
                  const isHeld = seat.status === 'HELD';

                  let seatStyle = 'bg-slate-800 hover:bg-slate-700 text-slate-300 border-slate-700';

                  if (isSelected) {
                    seatStyle = 'bg-amber-500 text-slate-950 font-extrabold border-amber-300 ring-2 ring-amber-400 scale-105';
                  } else if (isBooked) {
                    seatStyle = 'bg-rose-950/50 text-slate-600 border-rose-900/40 cursor-not-allowed opacity-50';
                  } else if (isHeld) {
                    seatStyle = 'bg-orange-950/50 text-orange-400/60 border-orange-900/40 cursor-not-allowed opacity-60';
                  }

                  return (
                    <button
                      key={seat.seatId}
                      disabled={isBooked || isHeld}
                      onClick={() => handleToggleSeat(seat)}
                      className={`w-9 h-9 sm:w-10 sm:h-10 rounded-lg text-xs font-semibold flex items-center justify-center border transition ${seatStyle}`}
                      title={`Ghế ${seat.seatNumber} - ${(seat.price || showtime?.basePrice || 0).toLocaleString()} đ`}
                    >
                      {seat.seatNumber}
                    </button>
                  );
                })}
              </div>

              <span className="w-6 text-center font-bold text-sm text-slate-400">{rowLetter}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Legend */}
      <div className="flex flex-wrap items-center justify-center gap-6 text-xs text-slate-400 mb-8">
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 rounded bg-slate-800 border border-slate-700" />
          <span>Ghế Trống</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 rounded bg-amber-500 border border-amber-300" />
          <span className="text-amber-400 font-medium">Đang Chọn</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 rounded bg-orange-950/50 border border-orange-900/40" />
          <span>Đang Giữ Chỗ</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 rounded bg-rose-950/50 border border-rose-900/40 opacity-50" />
          <span>Đã Đặt</span>
        </div>
      </div>

      {/* Bottom Floating Bar */}
      <div className="sticky bottom-4 z-40 bg-slate-900/95 backdrop-blur border border-slate-700 rounded-2xl p-4 sm:p-6 shadow-2xl flex flex-col sm:flex-row items-center justify-between gap-4">
        <div>
          <div className="text-xs text-slate-400">
            Ghế đã chọn ({selectedSeats.length}):{' '}
            <span className="font-bold text-white">
              {selectedSeats.map((s) => s.seatNumber).join(', ') || 'Chưa chọn'}
            </span>
          </div>
          <div className="text-xl sm:text-2xl font-black text-amber-400 mt-0.5">
            {totalPrice.toLocaleString()} <span className="text-sm font-normal">đ</span>
          </div>
        </div>

        <button
          disabled={selectedSeatIds.length === 0 || submitting}
          onClick={handleProceed}
          className="w-full sm:w-auto bg-red-600 hover:bg-red-500 disabled:bg-slate-800 disabled:text-slate-600 text-white font-bold px-8 py-3 rounded-xl shadow-lg shadow-red-600/30 transition flex items-center justify-center gap-2"
        >
          {submitting ? 'Đang giữ ghế...' : 'Tiếp Tục Thanh Toán →'}
        </button>
      </div>
    </div>
  );
}
