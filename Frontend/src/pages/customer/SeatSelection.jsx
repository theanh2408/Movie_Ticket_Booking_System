import React, { useState, useMemo } from 'react';
import { useLocation, useNavigate, Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { SeatIcon } from '../../components/SeatIcon';
import { Button } from '../../components/Button';
import { MOVIES, THEATERS } from '../../assets/mockData';

const ROWS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J'];
const COLS_PER_ROW = 12;
const SEAT_PRICE = 95000;

export const SeatSelection = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Booking session data passed from previous step
  const session = location.state || {
    movieId: MOVIES[0].id,
    movieTitle: MOVIES[0].title,
    moviePoster: MOVIES[0].poster,
    theaterId: THEATERS[0].id,
    theaterName: THEATERS[0].name,
    theaterAddress: THEATERS[0].address,
    showtimeId: 'st-drpn-r1',
    time: '19:30',
    room: 'Phòng Chiếu 1 (IMAX)',
    format: 'IMAX Laser 3D',
    date: 'Hôm nay (09/10)',
    price: SEAT_PRICE,
  };

  const [selectedSeatIds, setSelectedSeatIds] = useState([]);
  const [errorMessage, setErrorMessage] = useState('');

  // Fixed simulated booked seats
  const bookedSeatIds = useMemo(() => {
    return new Set(['C04', 'C05', 'D06', 'D07', 'E04', 'E05', 'E08', 'F05', 'F06', 'G09', 'G10']);
  }, []);

  const handleToggleSeat = (seatId) => {
    setErrorMessage('');
    if (bookedSeatIds.has(seatId)) return;

    if (selectedSeatIds.includes(seatId)) {
      setSelectedSeatIds(selectedSeatIds.filter((id) => id !== seatId));
    } else {
      if (selectedSeatIds.length >= 8) {
        setErrorMessage('Bạn chỉ có thể chọn tối đa 8 ghế trong một lượt đặt.');
        return;
      }
      setSelectedSeatIds([...selectedSeatIds, seatId]);
    }
  };

  const totalPrice = selectedSeatIds.length * SEAT_PRICE;

  const handleContinue = () => {
    if (selectedSeatIds.length === 0) {
      setErrorMessage('Vui lòng chọn ít nhất 1 ghế trước khi tiếp tục.');
      return;
    }

    // Navigate to checkout with payload
    navigate('/checkout', {
      state: {
        ...session,
        selectedSeats: selectedSeatIds,
        totalPrice,
      },
    });
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-8 space-y-6">
        {/* Progress Navigation Breadcrumb */}
        <div className="flex items-center justify-between pb-4 border-b border-white/5">
          <div className="flex items-center gap-3">
            <Link
              to={`/movie/${session.movieId}`}
              className="p-2 rounded-xl bg-[#171b26] hover:bg-[#262a35] text-[#af8782] hover:text-[#dfe2f1] transition-colors"
              title="Quay lại chi tiết phim"
            >
              <span className="material-symbols-outlined text-[20px]">arrow_back</span>
            </Link>
            <div>
              <span className="text-xs font-bold text-[#ffb95f] uppercase tracking-wider">
                Bước 2: Chọn vị trí ghế ngồi
              </span>
              <h1 className="font-['Space_Grotesk'] text-xl sm:text-2xl font-bold text-[#dfe2f1]">
                {session.movieTitle}
              </h1>
            </div>
          </div>

          <div className="hidden sm:flex items-center gap-2 text-xs text-[#af8782]">
            <span className="text-[#dfe2f1] font-semibold">{session.theaterName}</span>
            <span>•</span>
            <span className="text-[#ffb95f]">{session.room}</span>
            <span>•</span>
            <span className="text-[#7bd0ff]">{session.time}</span>
          </div>
        </div>

        {/* Warning notification */}
        {errorMessage && (
          <div className="p-3.5 rounded-xl bg-[#93000a]/30 border border-[#93000a] text-xs text-[#ffdad6] flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">error</span>
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Auditorium Screen & Seats Map */}
        <div className="p-6 rounded-3xl bg-[#171b26] border border-white/5 space-y-8">
          {/* Cinema Screen Curve */}
          <div className="relative flex flex-col items-center pt-2 pb-4">
            <div className="w-4/5 h-3 border-t-4 border-[#e50914] rounded-[50%] shadow-[0_6px_25px_rgba(229,9,20,0.8)]" />
            <div
              className="w-full h-12 mt-1 pointer-events-none"
              style={{
                background:
                  'radial-gradient(ellipse at top, rgba(229, 9, 20, 0.25) 0%, transparent 70%)',
              }}
            />
            <span className="text-[11px] font-bold text-[#af8782] tracking-widest uppercase -mt-4">
              MÀN HÌNH CHIẾU
            </span>
          </div>

          {/* Seat Grid */}
          <div className="overflow-x-auto pb-4 flex justify-center">
            <div className="inline-block space-y-2 min-w-[580px]">
              {ROWS.map((row) => (
                <div key={row} className="flex items-center justify-center gap-1.5 sm:gap-2">
                  {/* Row Label Left */}
                  <span className="w-5 text-center font-['Space_Grotesk'] text-xs font-bold text-[#af8782]">
                    {row}
                  </span>

                  {/* Seat columns */}
                  <div className="flex items-center gap-1 sm:gap-1.5">
                    {Array.from({ length: COLS_PER_ROW }).map((_, colIdx) => {
                      const colNum = colIdx + 1;
                      const seatId = `${row}${colNum < 10 ? '0' + colNum : colNum}`;
                      const isBooked = bookedSeatIds.has(seatId);
                      const isSelected = selectedSeatIds.includes(seatId);

                      let status = 'available';
                      if (isBooked) status = 'booked';
                      else if (isSelected) status = 'selected';

                      return (
                        <div
                          key={seatId}
                          className={colNum === 3 || colNum === 9 ? 'mr-3' : ''}
                        >
                          <SeatIcon
                            seatId={seatId}
                            status={status}
                            price={SEAT_PRICE}
                            onClick={() => handleToggleSeat(seatId)}
                          />
                        </div>
                      );
                    })}
                  </div>

                  {/* Row Label Right */}
                  <span className="w-5 text-center font-['Space_Grotesk'] text-xs font-bold text-[#af8782]">
                    {row}
                  </span>
                </div>
              ))}
            </div>
          </div>

          {/* Seat Legend */}
          <div className="flex flex-wrap items-center justify-center gap-6 pt-4 border-t border-white/5 text-xs text-[#af8782]">
            <div className="flex items-center gap-2">
              <span className="w-5 h-5 rounded-t-md bg-[#1f2937] border border-white/20" />
              <span>Ghế trống ({SEAT_PRICE.toLocaleString('vi-VN')}đ)</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-5 h-5 rounded-t-md bg-[#e50914] text-white shadow-[0_0_8px_rgba(229,9,20,0.6)]" />
              <span className="text-[#dfe2f1] font-semibold">Ghế đang chọn</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="w-5 h-5 rounded-t-md bg-[#111827] text-white/20 opacity-40 flex items-center justify-center text-[10px]">
                ✕
              </span>
              <span>Ghế đã được đặt</span>
            </div>
          </div>
        </div>

        {/* Bottom Booking Summary Sticky Bar */}
        <div className="p-5 rounded-2xl bg-[#1c1f2a] border border-white/10 shadow-2xl flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div className="flex items-center gap-4">
            <img
              src={session.moviePoster}
              alt={session.movieTitle}
              className="w-12 aspect-[2/3] object-cover rounded-lg shadow-md hidden sm:block"
            />
            <div>
              <div className="text-xs text-[#af8782]">
                Ghế đã chọn ({selectedSeatIds.length}/8):
              </div>
              <div className="font-['Space_Grotesk'] text-lg font-bold text-[#ffb95f]">
                {selectedSeatIds.length > 0 ? selectedSeatIds.join(', ') : 'Chưa chọn ghế'}
              </div>
              <div className="text-[11px] text-[#af8782] mt-0.5">
                {session.theaterName} • {session.room} • {session.time} ({session.date})
              </div>
            </div>
          </div>

          <div className="flex items-center justify-between md:justify-end gap-6 w-full md:w-auto border-t md:border-t-0 pt-3 md:pt-0 border-white/5">
            <div className="text-right">
              <div className="text-xs text-[#af8782]">Tổng tạm tính:</div>
              <div className="font-['Space_Grotesk'] text-2xl font-bold text-[#e50914]">
                {totalPrice.toLocaleString('vi-VN')}đ
              </div>
            </div>

            <Button
              variant="primary"
              size="lg"
              disabled={selectedSeatIds.length === 0}
              onClick={handleContinue}
              icon="arrow_forward"
            >
              Tiếp tục
            </Button>
          </div>
        </div>
      </div>
    </MainLayout>
  );
};
export default SeatSelection;
