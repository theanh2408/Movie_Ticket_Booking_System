import React, { useState, useEffect } from 'react';
import { useParams, useLocation, useNavigate, Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { SeatIcon } from '../../components/SeatIcon';
import { Button } from '../../components/Button';
import { useShowtimeSeats } from '../../hooks/useShowtimeSeats';
import showtimeApi from '../../api/showtimeApi';
import { cn } from '../../lib/cn';

export const SeatSelection = () => {
  const { id } = useParams();
  const location = useLocation();
  const navigate = useNavigate();

  // Custom hook tải dữ liệu ghế từ Backend và quản lý giới hạn chọn ghế
  const {
    seats,
    rows,
    seatsByRow,
    selectedSeats,
    selectedSeatNumbers,
    totalPrice,
    maxSeats,
    warningMessage,
    toggleSeat,
    loading,
    error,
    refetch,
  } = useShowtimeSeats(id);

  // Thông tin suất chiếu từ state điều hướng hoặc tải trực tiếp từ API nếu F5
  const [session, setSession] = useState(location.state || null);
  const [loadingSession, setLoadingSession] = useState(!location.state);

  // Kiểm tra đăng nhập
  useEffect(() => {
    const token =
      localStorage.getItem(import.meta.env?.VITE_AUTH_TOKEN_KEY || 'vibe_cinema_access_token') ||
      localStorage.getItem('Vibe Cinema_user') ||
      localStorage.getItem('vibe_cinema_user');

    if (!token) {
      navigate('/login', {
        state: {
          returnUrl: location.pathname,
          bookingData: session,
          message: 'Vui lòng đăng nhập để tiếp tục chọn ghế và đặt vé.',
        },
        replace: true,
      });
    }
  }, [navigate, location.pathname, session]);

  // Nếu người dùng tải lại trang mà không có location.state -> fetch thông tin suất chiếu từ API
  useEffect(() => {
    if (!session && id) {
      const fetchShowtime = async () => {
        setLoadingSession(true);
        try {
          const res = await showtimeApi.getShowtimeDetail(id);
          if (res && res.data) {
            const st = res.data;
            setSession({
              showtimeId: st.showtimeId,
              movieId: st.movieId,
              movieTitle: st.movieTitle || 'Phim đã chọn',
              moviePoster: st.posterUrl || 'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800',
              theaterId: st.cinemaId,
              theaterName: st.cinemaName || 'Cụm rạp Vibe Cinema',
              theaterAddress: '',
              room: st.roomName || 'Phòng chiếu',
              format: '2D Kỹ thuật số',
              date: st.date || 'Hôm nay',
              time: st.startTime || st.startHour || '00:00',
              price: st.basePrice || 85000,
            });
          }
        } catch (err) {
          console.error('Lỗi khi tải chi tiết suất chiếu:', err);
        } finally {
          setLoadingSession(false);
        }
      };
      fetchShowtime();
    }
  }, [id, session]);

  const handleContinue = () => {
    if (selectedSeats.length === 0) return;

    navigate('/checkout', {
      state: {
        ...(session || {}),
        showtimeId: id,
        selectedSeats: selectedSeatNumbers,
        selectedSeatObjects: selectedSeats,
        seatIds: selectedSeats.map((s) => s.seatId),
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
              to={session?.movieId ? `/movie/${session.movieId}` : '/'}
              className="p-2 rounded-xl bg-surface-low hover:bg-surface-high text-outline hover:text-on-surface transition-colors"
              title="Quay lại chi tiết phim"
            >
              <span className="material-symbols-outlined text-[20px]">arrow_back</span>
            </Link>
            <div>
              <span className="text-xs font-bold text-secondary uppercase tracking-wider">
                Bước 2: Chọn vị trí ghế ngồi (Tối đa {maxSeats} ghế)
              </span>
              <h1 className="font-headline text-xl sm:text-2xl font-bold text-on-surface">
                {session?.movieTitle || 'Chọn ghế xem phim'}
              </h1>
            </div>
          </div>

          {session && (
            <div className="hidden sm:flex items-center gap-2 text-xs text-outline">
              <span className="text-on-surface font-semibold">{session.theaterName}</span>
              <span>•</span>
              <span className="text-secondary">{session.room}</span>
              <span>•</span>
              <span className="text-tertiary">{session.time} ({session.date})</span>
            </div>
          )}
        </div>

        {/* Warning notification */}
        {warningMessage && (
          <div className="p-3.5 rounded-xl bg-primary-dark/30 border border-primary-dark text-xs text-[#ffdad6] flex items-center gap-2 animate-bounce">
            <span className="material-symbols-outlined text-[18px]">warning</span>
            <span>{warningMessage}</span>
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="p-6 rounded-3xl bg-surface-low border border-primary/20 text-center space-y-3">
            <span className="material-symbols-outlined text-[48px] text-primary">error</span>
            <h2 className="text-base font-bold text-on-surface">Không thể tải sơ đồ ghế</h2>
            <p className="text-xs text-outline">{error}</p>
            <button
              onClick={() => refetch()}
              className="px-5 py-2.5 rounded-xl bg-primary text-white text-xs font-bold hover:bg-primary-dark transition-all cursor-pointer"
            >
              Tải lại sơ đồ ghế
            </button>
          </div>
        )}

        {/* Loading Spinner */}
        {(loading || loadingSession) && (
          <div className="py-24 flex flex-col items-center justify-center space-y-4">
            <div className="size-10 border-4 border-primary border-t-transparent rounded-full animate-spin" />
            <p className="text-xs text-outline">Đang tải trạng thái ghế thực tế từ hệ thống...</p>
          </div>
        )}

        {/* Auditorium Screen & Seats Map */}
        {!loading && !error && (
          <div className="p-6 rounded-3xl bg-surface-low border border-white/5 space-y-8">
            {/* Cinema Screen Curve */}
            <div className="relative flex flex-col items-center pt-2 pb-4">
              <div className="w-4/5 h-3 border-t-4 border-primary rounded-[50%] shadow-[0_6px_25px_rgba(229,9,20,0.8)]" />
              <div className="w-full h-12 mt-1 pointer-events-none bg-[radial-gradient(ellipse_at_top,rgba(229,9,20,0.25)_0%,transparent_70%)]" />
              <span className="text-[11px] font-bold text-outline tracking-widest uppercase -mt-4">
                MÀN HÌNH CHIẾU
              </span>
            </div>

            {/* Empty Seats State */}
            {seats.length === 0 ? (
              <div className="py-12 text-center space-y-2">
                <span className="material-symbols-outlined text-[40px] text-outline">event_seat</span>
                <p className="text-xs text-outline">Chưa có thông tin ghế cho phòng chiếu này.</p>
              </div>
            ) : (
              /* Seat Grid */
              <div className="overflow-x-auto pb-4 flex justify-center">
                <div className="inline-block space-y-2 min-w-[500px]">
                  {rows.map((row) => {
                    const rowSeats = seatsByRow[row] || [];
                    return (
                      <div key={row} className="flex items-center justify-center gap-1.5 sm:gap-2">
                        {/* Row Label Left */}
                        <span className="w-5 text-center font-headline text-xs font-bold text-outline">
                          {row}
                        </span>

                        {/* Seat columns */}
                        <div className="flex items-center gap-1 sm:gap-1.5">
                          {rowSeats.map((seat) => {
                            const isSelected = selectedSeatNumbers.includes(seat.seatNumber);
                            const isAvailable = seat.status?.toUpperCase() === 'AVAILABLE';

                            let visualStatus = 'available';
                            if (isSelected) {
                              visualStatus = 'selected';
                            } else if (!isAvailable) {
                              visualStatus = 'booked';
                            }

                            return (
                              <SeatIcon
                                key={seat.seatId}
                                seatId={seat.seatNumber}
                                status={visualStatus}
                                price={seat.price || session?.price || 85000}
                                onClick={() => toggleSeat(seat)}
                              />
                            );
                          })}
                        </div>

                        {/* Row Label Right */}
                        <span className="w-5 text-center font-headline text-xs font-bold text-outline">
                          {row}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}

            {/* Seat Legend */}
            <div className="flex flex-wrap items-center justify-center gap-6 pt-4 border-t border-white/5 text-xs text-outline">
              <div className="flex items-center gap-2">
                <span className="size-5 rounded-t-md bg-seat-available border border-white/20" />
                <span>Ghế trống ({(session?.price || 85000).toLocaleString('vi-VN')}đ)</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="size-5 rounded-t-md bg-seat-selected text-white shadow-glow-seat" />
                <span className="text-on-surface font-semibold">Ghế đang chọn</span>
              </div>
              <div className="flex items-center gap-2">
                <span className="size-5 rounded-t-md bg-seat-booked" />
                <span>Ghế đã đặt / Đang giữ</span>
              </div>
            </div>
          </div>
        )}

        {/* Bottom Booking Summary Sticky Bar */}
        {!loading && (
          <div className="p-5 rounded-2xl bg-surface-container border border-white/10 shadow-2xl flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
            <div className="flex items-center gap-4">
              {session?.moviePoster && (
                <img
                  src={session.moviePoster}
                  alt={session.movieTitle}
                  className="w-12 aspect-[2/3] object-cover rounded-lg shadow-md hidden sm:block"
                />
              )}
              <div>
                <div className="text-xs text-outline">
                  Ghế đã chọn ({selectedSeats.length}/{maxSeats}):
                </div>
                <div className="font-headline text-lg font-bold text-secondary">
                  {selectedSeatNumbers.length > 0 ? selectedSeatNumbers.join(', ') : 'Chưa chọn ghế'}
                </div>
                {session && (
                  <div className="text-[11px] text-outline mt-0.5">
                    {session.theaterName} • {session.room} • {session.time} ({session.date})
                  </div>
                )}
              </div>
            </div>

            <div className="flex items-center justify-between md:justify-end gap-6 w-full md:w-auto border-t md:border-t-0 pt-3 md:pt-0 border-white/5">
              <div className="text-right">
                <div className="text-xs text-outline">Tổng tạm tính:</div>
                <div className="font-headline text-2xl font-bold text-primary">
                  {totalPrice.toLocaleString('vi-VN')}đ
                </div>
              </div>

              <Button
                variant="primary"
                size="lg"
                disabled={selectedSeats.length === 0}
                onClick={handleContinue}
                icon="arrow_forward"
              >
                Tiếp tục
              </Button>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};

export default SeatSelection;
