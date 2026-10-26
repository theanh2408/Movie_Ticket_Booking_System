import React, { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { cn } from '../../lib/cn';

// ==========================================================================
// 1. HELPER FUNCTIONS: Kiểm tra quy tắc 120 phút & Định dạng thời gian
// ==========================================================================

/**
 * Kiểm tra thời gian từ hiện tại đến giờ bắt đầu chiếu có >= 120 phút không
 * @param {string} showDateTime - Chuỗi ISO hoặc date string của suất chiếu
 * @returns {boolean} true nếu còn >= 120 phút, false nếu < 120 phút
 */
export const isCancelable = (showDateTime) => {
  if (!showDateTime) return false;
  const showTime = new Date(showDateTime).getTime();
  const now = Date.now();
  const diffMinutes = Math.floor((showTime - now) / (1000 * 60));
  return diffMinutes >= 120;
};

/**
 * Tính số phút còn lại từ thời điểm hiện tại đến giờ chiếu
 * @param {string} showDateTime - Chuỗi ISO của suất chiếu
 * @returns {number} Số phút còn lại
 */
export const getMinutesUntilShow = (showDateTime) => {
  if (!showDateTime) return 0;
  const showTime = new Date(showDateTime).getTime();
  const now = Date.now();
  return Math.floor((showTime - now) / (1000 * 60));
};

/**
 * Định dạng số phút thành chuỗi trực quan: "X giờ Y phút" hoặc "Y phút"
 * @param {number} minutes
 * @returns {string}
 */
export const formatRemainingTime = (minutes) => {
  if (minutes <= 0) return 'Đã bắt đầu hoặc qua giờ chiếu';
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  if (hours > 0) {
    return `${hours} giờ ${mins > 0 ? `${mins} phút` : ''}`.trim();
  }
  return `${mins} phút`;
};

/**
 * Lấy thời gian hiện tại định dạng DD/MM/YYYY HH:mm
 */
const getCurrentFormattedTime = () => {
  const now = new Date();
  const d = String(now.getDate()).padStart(2, '0');
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const y = now.getFullYear();
  const h = String(now.getHours()).padStart(2, '0');
  const min = String(now.getMinutes()).padStart(2, '0');
  return `${d}/${m}/${y} ${h}:${min}`;
};

// ==========================================================================
// 2. MOCK DATA TĨNH: Chứa vé > 120 phút, < 120 phút, đã xem và đã hủy
// ==========================================================================
export const getInitialMockTickets = () => {
  const now = Date.now();
  return [
    {
      id: 'ticket-up-01',
      bookingCode: 'CT-982410',
      movieTitle: 'Đất Rừng Phương Nam: Huyền Thoại Trở Lại',
      moviePoster:
        'https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&auto=format&fit=crop&q=80',
      theaterName: 'Vibe Cinema Royal Mall (Quận 1)',
      theaterAddress: 'Tầng 5, TTTM Royal Mall, 182 Lê Thánh Tôn, P. Bến Nghé, Quận 1, TP.HCM',
      roomName: 'Phòng Chiếu 1 (IMAX Laser)',
      format: 'IMAX 3D Laser',
      // Suất chiếu sau 5 giờ (300 phút > 120 phút) -> ĐỦ ĐIỀU KIỆN HỦY VÉ
      showDateTime: new Date(now + 5 * 3600 * 1000).toISOString(),
      showDate: 'Hôm nay (26/10/2026)',
      showTime: '20:15',
      seats: ['F07', 'F08'],
      totalPrice: 220000,
      bookingDate: '26/10/2026 14:30',
      status: 'upcoming', // Phim sắp xem
      paymentMethod: 'Ví MoMo',
      qrValue: 'VIBECINEMA_QR_CT982410_ROYAL_IMAX_F07_F08',
      barcodeValue: '982410772189',
    },
    {
      id: 'ticket-up-02',
      bookingCode: 'CT-831490',
      movieTitle: 'Dune: Biên Niên Sử Cát Vàng',
      moviePoster:
        'https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=800&auto=format&fit=crop&q=80',
      theaterName: 'Vibe Cinema Landmark (Bình Thạnh)',
      theaterAddress:
        'Tầng B1, Vincom Center Landmark 81, 720A Điện Biên Phủ, P.22, Bình Thạnh',
      roomName: 'Phòng Chiếu 3 (Dolby Atmos)',
      format: '2D Dolby Atmos Phụ Đề',
      // Suất chiếu sau 45 phút (45 phút < 120 phút) -> KHÔNG ĐỦ ĐIỀU KIỆN (BỊ CHẶN HỦY)
      showDateTime: new Date(now + 45 * 60 * 1000).toISOString(),
      showDate: 'Hôm nay (26/10/2026)',
      showTime: '01:55',
      seats: ['G12', 'G13'],
      totalPrice: 190000,
      bookingDate: '26/10/2026 10:15',
      status: 'upcoming', // Phim sắp xem
      paymentMethod: 'Thẻ Quốc Tế (Visa/Mastercard)',
      qrValue: 'VIBECINEMA_QR_CT831490_LANDMARK_G12_G13',
      barcodeValue: '831490126743',
    },
    {
      id: 'ticket-wt-01',
      bookingCode: 'CT-762104',
      movieTitle: 'Thám Tử Lừng Danh: Mật Mã Tokyo',
      moviePoster:
        'https://images.unsplash.com/photo-1578632767115-351597cf2477?w=800&auto=format&fit=crop&q=80',
      theaterName: 'Vibe Cinema Saigon Centre (Quận 1)',
      theaterAddress: 'Tầng L5-L6, Saigon Centre, 65 Lê Lợi, P. Bến Nghé, Quận 1, TP.HCM',
      roomName: 'Phòng Chiếu 2',
      format: '2D Lồng Tiếng Việt',
      showDate: '18/10/2026',
      showTime: '18:30',
      seats: ['E05', 'E06'],
      totalPrice: 180000,
      bookingDate: '16/10/2026 09:20',
      status: 'watched', // Phim đã xem
      paymentMethod: 'Ví VNPAY-QR',
      qrValue: 'VIBECINEMA_QR_CT762104_SAIGON_E05_E06',
      barcodeValue: '762104554321',
    },
    {
      id: 'ticket-ca-01',
      bookingCode: 'CT-615290',
      movieTitle: 'Băng Đảng Báo Đen: Khởi Tố',
      moviePoster:
        'https://images.unsplash.com/photo-1478760329108-5c3ed9d495a0?w=800&auto=format&fit=crop&q=80',
      theaterName: 'Vibe Cinema Ba Tháng Hai (Quận 10)',
      theaterAddress: 'Số 3/2, Phường 12, Quận 10, TP.HCM',
      roomName: 'Phòng Chiếu 4DX',
      format: '4DX Extreme',
      showDate: '12/10/2026',
      showTime: '21:30',
      seats: ['H09'],
      totalPrice: 150000,
      bookingDate: '11/10/2026 16:45',
      cancelledDate: '11/10/2026 18:30',
      status: 'cancelled', // Phim đã hủy
      cancelReason: 'Thay đổi lịch trình cá nhân đột xuất.',
      refundStatus: 'Đã hoàn tiền',
      paymentMethod: 'Ví MoMo',
      qrValue: 'VIBECINEMA_QR_CT615290_32_4DX_H09',
      barcodeValue: '615290981240',
    },
  ];
};

// 4 Lý do hủy vé chuẩn theo nghiệp vụ
export const CANCELLATION_REASONS = [
  'Thay đổi lịch trình cá nhân đột xuất.',
  'Đặt nhầm suất chiếu / nhầm rạp / nhầm phim.',
  'Muốn đổi vị trí ghế ngồi khác.',
  'Lý do khác',
];

export const MyTicket = () => {
  // Tab hiện tại: 'upcoming' (Phim sắp xem) hoặc 'watched' (Phim đã xem / Đã hủy)
  const [activeTab, setActiveTab] = useState('upcoming');

  // Danh sách vé (Load từ localStorage hoặc khởi tạo từ mock)
  const [tickets, setTickets] = useState(() => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_tickets');
      if (stored) {
        const parsed = JSON.parse(stored);
        if (Array.isArray(parsed) && parsed.length > 0) {
          // Bảo đảm có showDateTime cho vé sắp chiếu nếu thiếu
          const now = Date.now();
          return parsed.map((t, idx) => {
            if (!t.showDateTime && t.status === 'upcoming') {
              return {
                ...t,
                showDateTime: new Date(now + (idx === 0 ? 5 * 3600 : 45 * 60) * 1000).toISOString(),
              };
            }
            return t;
          });
        }
      }
      return getInitialMockTickets();
    } catch {
      return getInitialMockTickets();
    }
  });

  // State Step 2: Modal chi tiết vé đang chọn
  const [selectedTicket, setSelectedTicket] = useState(null);

  // State hiển thị mã vé (QR Code / Barcode) bên trong Modal chi tiết
  const [showQrCode, setShowQrCode] = useState(false);

  // State thông báo sao chép mã vé
  const [copiedCode, setCopiedCode] = useState(false);

  // State Step 3: Modal chọn lý do hủy vé & xác nhận
  const [showCancelModal, setShowCancelModal] = useState(false);
  const [selectedReason, setSelectedReason] = useState(CANCELLATION_REASONS[0]);
  const [otherReasonText, setOtherReasonText] = useState('');
  const [cancelError, setCancelError] = useState('');

  // Toast thông báo hệ thống
  const [toastData, setToastData] = useState(null);

  const showToast = ({ title, message, actionText, actionLink }) => {
    setToastData({ title, message, actionText, actionLink });
    setTimeout(() => {
      setToastData(null);
    }, 6000);
  };

  // Phân loại danh sách vé theo trạng thái
  const upcomingTickets = useMemo(
    () => tickets.filter((t) => t.status === 'upcoming'),
    [tickets]
  );
  const watchedOrCancelledTickets = useMemo(
    () => tickets.filter((t) => t.status === 'watched' || t.status === 'cancelled'),
    [tickets]
  );

  // Danh sách vé theo tab đang chọn
  const currentTickets = activeTab === 'upcoming' ? upcomingTickets : watchedOrCancelledTickets;

  // Đóng modal khi bấm phím Escape
  useEffect(() => {
    const handleKeyDown = (e) => {
      if (e.key === 'Escape') {
        if (showCancelModal) {
          setShowCancelModal(false);
        } else if (selectedTicket) {
          handleCloseModal();
        }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => {
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [selectedTicket, showCancelModal]);

  // Mở modal chi tiết vé
  const handleOpenTicketDetail = (ticket) => {
    setSelectedTicket(ticket);
    setShowQrCode(false);
    setCopiedCode(false);
    setShowCancelModal(false);
    setSelectedReason(CANCELLATION_REASONS[0]);
    setOtherReasonText('');
    setCancelError('');
  };

  // Đóng modal chi tiết vé
  const handleCloseModal = () => {
    setSelectedTicket(null);
    setShowQrCode(false);
    setCopiedCode(false);
    setShowCancelModal(false);
  };

  // Xử lý nút Lấy mã vé (QR Code / Barcode)
  const handleToggleQrCode = () => {
    setShowQrCode((prev) => !prev);
  };

  // Sao chép mã đặt vé vào clipboard
  const handleCopyBookingCode = (code) => {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(code);
      setCopiedCode(true);
      setTimeout(() => setCopiedCode(false), 2500);
    }
  };

  // ========================================================================
  // BƯỚC 3: Xử lý Xác nhận hủy vé & Hoàn tiền
  // ========================================================================
  const handleConfirmCancelTicket = () => {
    if (!selectedTicket) return;

    // Kiểm tra điều kiện một lần nữa
    const minutesLeft = getMinutesUntilShow(selectedTicket.showDateTime);
    if (minutesLeft < 120) {
      setCancelError(
        'Không thể hủy: Suất chiếu còn dưới 120 phút theo quy định của hệ thống.'
      );
      return;
    }

    if (selectedReason === 'Lý do khác' && !otherReasonText.trim()) {
      setCancelError('Vui lòng nhập lý do cụ thể muốn hủy vé.');
      return;
    }

    const finalReason =
      selectedReason === 'Lý do khác' ? otherReasonText.trim() : selectedReason;
    const nowFormatted = getCurrentFormattedTime();

    // 1. Cập nhật trạng thái vé sang CANCELLED (Đã hủy)
    const updatedTickets = tickets.map((t) => {
      if (t.id === selectedTicket.id || t.bookingCode === selectedTicket.bookingCode) {
        return {
          ...t,
          status: 'cancelled',
          cancelReason: finalReason,
          cancelledAt: nowFormatted,
          refundStatus: 'Đã hoàn tiền',
          canCancel: false,
        };
      }
      return t;
    });

    setTickets(updatedTickets);
    try {
      localStorage.setItem('Vibe Cinema_tickets', JSON.stringify(updatedTickets));
    } catch {
      // no-op
    }

    // 2. Tự động tạo bản ghi hoàn tiền mới trong mục "Lịch sử giao dịch"
    const newRefundTransaction = {
      id: `tx-refund-${Date.now()}`,
      invoiceCode: selectedTicket.bookingCode.replace('CT-', 'HD-'),
      transactionTime: nowFormatted,
      paymentMethod: selectedTicket.paymentMethod || 'Ví MoMo',
      theaterInfo: `${selectedTicket.theaterName} - ${selectedTicket.roomName}`,
      theaterName: selectedTicket.theaterName,
      roomName: selectedTicket.roomName,
      movieTitle: selectedTicket.movieTitle,
      seats: selectedTicket.seats,
      ticketCount: Array.isArray(selectedTicket.seats) ? selectedTicket.seats.length : 1,
      totalAmount: selectedTicket.totalPrice,
      status: 'Đã hoàn tiền',
      refundStatus: 'Đã hoàn tiền',
      cancelReason: finalReason,
    };

    try {
      const storedTx = localStorage.getItem('Vibe Cinema_transactions');
      const txList = storedTx ? JSON.parse(storedTx) : [];
      const updatedTxList = [
        newRefundTransaction,
        ...txList.filter((tx) => tx.invoiceCode !== newRefundTransaction.invoiceCode),
      ];
      localStorage.setItem('Vibe Cinema_transactions', JSON.stringify(updatedTxList));
    } catch {
      // no-op
    }

    // 3. Đóng modals và hiển thị Toast thành công có link sang Lịch sử giao dịch
    setShowCancelModal(false);
    setSelectedTicket(null);

    showToast({
      title: 'Hủy vé & Gửi yêu cầu hoàn tiền thành công!',
      message: `Vé #${selectedTicket.bookingCode} đã được hủy. Số tiền 100% (${selectedTicket.totalPrice.toLocaleString('vi-VN')}₫) đang được hoàn về ${selectedTicket.paymentMethod}.`,
      actionText: 'Xem Lịch sử giao dịch',
      actionLink: '/profile?tab=transactions',
    });

    // 4. Chuyển tab sang mục "Phim đã xem / Đã hủy" để thấy ngay vé đã hủy
    setActiveTab('watched');
  };

  // Tính toán điều kiện 120 phút cho vé đang chọn trong modal
  const selectedMinutesLeft = selectedTicket
    ? getMinutesUntilShow(selectedTicket.showDateTime)
    : 0;
  const isSelectedCancelable = selectedTicket
    ? selectedTicket.status === 'upcoming' && selectedMinutesLeft >= 120
    : false;

  return (
    <MainLayout>
      <div className="w-full min-h-screen bg-surface text-on-surface px-4 py-10 pb-20 box-border">
        <div className="max-w-[1140px] mx-auto flex flex-col gap-8">
          {/* Toast Notification Alert */}
          {toastData && (
            <div className="fixed top-24 right-6 z-50 max-w-md bg-[#11141e] border border-emerald-500/50 p-4 rounded-2xl shadow-2xl backdrop-blur-xl animate-fade-in-down flex flex-col gap-2.5">
              <div className="flex items-start gap-3">
                <span className="material-symbols-outlined text-emerald-400 text-2xl shrink-0">
                  check_circle
                </span>
                <div className="flex-1">
                  <h4 className="text-sm font-bold text-white">{toastData.title}</h4>
                  <p className="text-xs text-zinc-300 mt-0.5 leading-relaxed">
                    {toastData.message}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => setToastData(null)}
                  className="text-zinc-500 hover:text-white"
                >
                  <span className="material-symbols-outlined text-lg">close</span>
                </button>
              </div>

              {toastData.actionLink && (
                <div className="pt-2 border-t border-white/10 flex justify-end">
                  <Link
                    to={toastData.actionLink}
                    className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-500/20 hover:bg-emerald-500/30 text-emerald-300 text-xs font-semibold transition-colors"
                  >
                    <span>{toastData.actionText}</span>
                    <span className="material-symbols-outlined text-sm">arrow_forward</span>
                  </Link>
                </div>
              )}
            </div>
          )}

          {/* ================================================================
              BƯỚC 1: PAGE HEADER & TABS NAVIGATION
              ================================================================ */}
          <header className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-6 p-7 sm:px-8 bg-gradient-to-br from-surface-low to-surface-container border border-white/10 rounded-3xl shadow-card">
            <div className="flex items-center gap-5">
              <div className="size-14 sm:size-15 rounded-2xl bg-primary/15 border border-primary/35 text-primary flex items-center justify-center shrink-0 shadow-glow-primary">
                <span className="material-symbols-outlined text-4xl">confirmation_number</span>
              </div>
              <div className="flex flex-col gap-1">
                <div className="flex items-center gap-3 flex-wrap">
                  <h1 className="font-headline text-2xl sm:text-[26px] font-bold text-white tracking-tight">
                    Vé Của Tôi
                  </h1>
                  <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-secondary/15 text-secondary border border-secondary/35">
                    <span className="material-symbols-outlined text-sm">verified</span>
                    <span>{upcomingTickets.length} vé có hiệu lực</span>
                  </span>
                </div>
                <p className="text-[13px] text-outline">
                  Quản lý vé xem phim, hỗ trợ hủy vé &amp; hoàn tiền trực tuyến trước giờ chiếu 120 phút
                </p>
              </div>
            </div>

            <div className="flex items-center gap-3 flex-wrap">
              <Link
                to="/profile?tab=transactions"
                className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-surface-high text-emerald-400 border border-emerald-500/30 text-[13px] font-semibold cursor-pointer transition-all hover:bg-emerald-500/20 hover:border-emerald-500/50 hover:-translate-y-0.5"
                title="Xem trạng thái hoàn tiền và lịch sử giao dịch"
              >
                <span className="material-symbols-outlined text-[18px]">receipt_long</span>
                <span>Lịch sử hoàn tiền</span>
              </Link>
            </div>
          </header>

          {/* ================================================================
              TABS NAVIGATION: 2 Tabs rõ ràng
              Tab 1: Phim sắp xem | Tab 2: Phim đã xem / Đã hủy
              ================================================================ */}
          <section className="flex flex-col gap-5">
            <nav
              className="flex items-center gap-3 bg-surface-lowest p-1.5 rounded-2xl border border-white/5 w-fit"
              aria-label="Bộ lọc vé theo trạng thái"
            >
              {/* Tab Phim sắp xem */}
              <button
                type="button"
                className={cn(
                  'inline-flex items-center gap-2.5 px-5 py-2.5 rounded-xl text-sm font-semibold transition-all cursor-pointer',
                  activeTab === 'upcoming'
                    ? 'bg-primary text-white shadow-glow-primary'
                    : 'text-outline hover:text-on-surface hover:bg-white/5'
                )}
                onClick={() => setActiveTab('upcoming')}
              >
                <span className="material-symbols-outlined text-xl">upcoming</span>
                <span>Phim sắp xem</span>
                <span
                  className={cn(
                    'inline-flex items-center justify-center min-w-5 h-5 px-1.5 rounded-full text-[11px] font-bold',
                    activeTab === 'upcoming'
                      ? 'bg-black/30 text-white'
                      : 'bg-white/10 text-on-surface'
                  )}
                >
                  {upcomingTickets.length}
                </span>
              </button>

              {/* Tab Phim đã xem / Đã hủy */}
              <button
                type="button"
                className={cn(
                  'inline-flex items-center gap-2.5 px-5 py-2.5 rounded-xl text-sm font-semibold transition-all cursor-pointer',
                  activeTab === 'watched'
                    ? 'bg-primary text-white shadow-glow-primary'
                    : 'text-outline hover:text-on-surface hover:bg-white/5'
                )}
                onClick={() => setActiveTab('watched')}
              >
                <span className="material-symbols-outlined text-xl">history</span>
                <span>Phim đã xem / Đã hủy</span>
                <span
                  className={cn(
                    'inline-flex items-center justify-center min-w-5 h-5 px-1.5 rounded-full text-[11px] font-bold',
                    activeTab === 'watched'
                      ? 'bg-black/30 text-white'
                      : 'bg-white/10 text-on-surface'
                  )}
                >
                  {watchedOrCancelledTickets.length}
                </span>
              </button>
            </nav>

            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-3 border-b border-white/5">
              <h2 className="text-[13px] font-bold text-outline uppercase tracking-wider">
                {activeTab === 'upcoming'
                  ? `Danh sách vé sắp chiếu (${upcomingTickets.length})`
                  : `Lịch sử vé đã xem & đã hủy (${watchedOrCancelledTickets.length})`}
              </h2>
              <div className="flex items-center gap-1.5 text-xs text-outline">
                <span className="material-symbols-outlined text-base text-tertiary">touch_app</span>
                <span>Bấm vào vé để xem chi tiết, lấy mã QR hoặc yêu cầu hủy vé</span>
              </div>
            </div>

            {/* DANH SÁCH TICKET CARDS */}
            {currentTickets.length === 0 ? (
              <div className="flex flex-col items-center justify-center text-center p-16 sm:p-20 bg-surface-low border border-dashed border-white/10 rounded-3xl gap-5">
                <div className="size-18 rounded-3xl bg-surface-high text-outline/60 flex items-center justify-center">
                  <span className="material-symbols-outlined text-4xl">theater_comedy</span>
                </div>
                <div className="flex flex-col gap-1.5 max-w-[380px]">
                  <h3 className="font-headline text-lg font-bold text-white">
                    {activeTab === 'upcoming'
                      ? 'Chưa có vé phim sắp xem'
                      : 'Chưa có lịch sử phim đã xem hoặc đã hủy'}
                  </h3>
                  <p className="text-[13px] text-outline leading-relaxed">
                    {activeTab === 'upcoming'
                      ? 'Bạn chưa có suất chiếu sắp tới nào. Hãy đặt vé ngay để thưởng thức các bom tấn đỉnh cao!'
                      : 'Các suất chiếu sau khi kết thúc hoặc đã hủy hoàn tiền sẽ lưu trữ tự động tại đây.'}
                  </p>
                </div>
                <Link
                  to="/now-showing"
                  className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-primary text-white text-[13px] font-bold shadow-glow-primary hover:bg-primary-hover hover:-translate-y-0.5 transition-all"
                >
                  <span className="material-symbols-outlined text-lg">local_activity</span>
                  <span>Khám Phá Phim Đang Chiếu</span>
                </Link>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {currentTickets.map((ticket) => {
                  const minutesLeft = getMinutesUntilShow(ticket.showDateTime);
                  const canCancelThisTicket = ticket.status === 'upcoming' && minutesLeft >= 120;

                  return (
                    <article
                      key={ticket.id}
                      className={cn(
                        'group relative bg-surface-low border rounded-3xl overflow-hidden shadow-card cursor-pointer flex flex-col transition-all duration-200 hover:-translate-y-1 hover:border-white/20 hover:shadow-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary',
                        ticket.status === 'upcoming'
                          ? 'border-white/10'
                          : ticket.status === 'cancelled'
                          ? 'border-amber-500/30 opacity-90'
                          : 'border-white/5 opacity-80 hover:opacity-100'
                      )}
                      onClick={() => handleOpenTicketDetail(ticket)}
                      tabIndex={0}
                      role="button"
                      aria-label={`Chi tiết vé xem phim ${ticket.movieTitle}`}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          handleOpenTicketDetail(ticket);
                        }
                      }}
                    >
                      {/* Header Card: Tên bộ phim & Tag trạng thái */}
                      <div className="p-5 sm:px-6 bg-gradient-to-b from-surface-container to-surface-low border-b border-white/5 flex items-start justify-between gap-4">
                        <div className="flex flex-col gap-1 flex-1">
                          <span className="text-[11px] font-bold uppercase tracking-wider text-secondary">
                            {ticket.format}
                          </span>
                          <h3 className="font-headline text-lg font-bold text-white line-clamp-2 leading-snug group-hover:text-primary-light transition-colors">
                            {ticket.movieTitle}
                          </h3>
                        </div>

                        {/* Status Badge */}
                        <div className="flex flex-col items-end gap-1.5">
                          <span
                            className={cn(
                              'inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-bold uppercase tracking-wider whitespace-nowrap',
                              ticket.status === 'upcoming'
                                ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                                : ticket.status === 'cancelled'
                                ? 'bg-amber-500/15 text-amber-300 border border-amber-500/30'
                                : 'bg-surface-high text-outline border border-white/5'
                            )}
                          >
                            {ticket.status === 'upcoming'
                              ? 'Sắp chiếu'
                              : ticket.status === 'cancelled'
                              ? 'Đã hủy vé'
                              : 'Đã xem'}
                          </span>

                          {/* Tag điều kiện hủy vé 120 phút trên Card */}
                          {ticket.status === 'upcoming' && (
                            <span
                              className={cn(
                                'text-[10px] font-semibold px-2 py-0.5 rounded-md flex items-center gap-1',
                                canCancelThisTicket
                                  ? 'bg-emerald-500/10 text-emerald-300'
                                  : 'bg-red-500/15 text-red-400'
                              )}
                            >
                              <span className="material-symbols-outlined text-[13px]">
                                {canCancelThisTicket ? 'check_circle' : 'lock_clock'}
                              </span>
                              <span>
                                {canCancelThisTicket
                                  ? 'Có thể hủy (>120p)'
                                  : `Sát giờ chiếu (${formatRemainingTime(minutesLeft)})`}
                              </span>
                            </span>
                          )}
                        </div>
                      </div>

                      {/* Card Body: Poster + Tên rạp, Ngày giờ đặt & Suất chiếu */}
                      <div className="p-5 sm:px-6 flex items-center gap-5">
                        <img
                          src={ticket.moviePoster}
                          alt={ticket.movieTitle}
                          className="w-[72px] h-[104px] rounded-xl object-cover shrink-0 shadow-md border border-white/10"
                          loading="lazy"
                        />

                        <div className="flex flex-col gap-2.5 flex-1 text-[13px]">
                          {/* Tên rạp */}
                          <div className="flex items-start gap-2">
                            <span className="material-symbols-outlined text-lg text-tertiary shrink-0 mt-0.5">
                              location_on
                            </span>
                            <div>
                              <span className="text-[11px] text-outline uppercase tracking-wider block">
                                Rạp chiếu
                              </span>
                              <span className="text-on-surface font-medium line-clamp-1">
                                {ticket.theaterName}
                              </span>
                            </div>
                          </div>

                          {/* Suất chiếu & Ghế */}
                          <div className="flex items-start gap-2">
                            <span className="material-symbols-outlined text-lg text-tertiary shrink-0 mt-0.5">
                              schedule
                            </span>
                            <div>
                              <span className="text-[11px] text-outline uppercase tracking-wider block">
                                Suất chiếu &amp; Ghế
                              </span>
                              <span className="text-on-surface font-medium">
                                {ticket.showTime} • {ticket.showDate} |{' '}
                                <span className="font-headline text-primary-light font-bold">
                                  Ghế: {ticket.seats.join(', ')}
                                </span>
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Đường răng cưa vé xem phim */}
                      <div
                        className="relative w-full h-6 flex items-center justify-center overflow-hidden"
                        aria-hidden="true"
                      >
                        <div className="absolute -left-3 top-1/2 -translate-y-1/2 size-6 rounded-full bg-surface border-r border-white/10 shadow-[inset_0_0_4px_rgba(0,0,0,0.6)] z-10" />
                        <div className="w-[calc(100%-3rem)] border-b border-dashed border-white/15" />
                        <div className="absolute -right-3 top-1/2 -translate-y-1/2 size-6 rounded-full bg-surface border-l border-white/10 shadow-[inset_0_0_4px_rgba(0,0,0,0.6)] z-10" />
                      </div>

                      {/* Card Footer: Xem chi tiết & Thành tiền */}
                      <div className="px-5 sm:px-6 pt-3.5 pb-5 flex items-end justify-between gap-4">
                        <div className="inline-flex items-center gap-1.5 text-xs font-semibold text-tertiary group-hover:text-white transition-colors">
                          <span className="material-symbols-outlined text-lg">visibility</span>
                          <span>
                            {ticket.status === 'upcoming'
                              ? 'Chi tiết & Đổi/Hủy vé'
                              : 'Xem chi tiết vé'}
                          </span>
                        </div>

                        <div className="flex flex-col items-end text-right ml-auto">
                          <span className="text-[11px] text-outline uppercase tracking-wider mb-0.5">
                            Thành tiền
                          </span>
                          <span
                            className={cn(
                              'font-headline text-xl font-bold leading-none',
                              ticket.status === 'upcoming'
                                ? 'text-secondary'
                                : ticket.status === 'cancelled'
                                ? 'text-amber-400'
                                : 'text-outline'
                            )}
                          >
                            {ticket.totalPrice.toLocaleString('vi-VN')}đ
                          </span>
                        </div>
                      </div>
                    </article>
                  );
                })}
              </div>
            )}
          </section>

          {/* ================================================================
              BƯỚC 2: MODAL CHI TIẾT VÉ & KIỂM TRA ĐIỀU KIỆN 120 PHÚT
              ================================================================ */}
          {selectedTicket && (
            <div
              className="fixed inset-0 z-50 bg-[#0a0e18]/90 backdrop-blur-md flex items-center justify-center p-4 animate-fade-in"
              onClick={handleCloseModal}
              role="dialog"
              aria-modal="true"
              aria-labelledby="modal-ticket-title"
            >
              <div
                className="relative w-full max-w-[560px] max-h-[92vh] bg-surface-low border border-white/20 rounded-3xl shadow-modal overflow-y-auto flex flex-col animate-modal-scale"
                onClick={(e) => e.stopPropagation()}
              >
                {/* Nút đóng modal */}
                <button
                  type="button"
                  className="absolute top-4 right-4 size-9 rounded-xl bg-surface-high text-outline border border-white/10 flex items-center justify-center cursor-pointer z-10 transition-colors hover:bg-primary hover:text-white hover:border-primary"
                  onClick={handleCloseModal}
                  aria-label="Đóng chi tiết vé"
                >
                  <span className="material-symbols-outlined text-xl">close</span>
                </button>

                {/* Header Modal: Tên bộ phim nằm to trên cùng */}
                <div className="pt-8 px-7 pb-5 bg-gradient-to-b from-surface-container to-surface-low border-b border-white/5 flex flex-col gap-2">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="font-headline text-xs font-bold text-secondary bg-secondary/15 border border-secondary/35 px-2.5 py-0.5 rounded-full flex items-center gap-1">
                      <span>Mã vé: #{selectedTicket.bookingCode}</span>
                      <button
                        type="button"
                        onClick={() => handleCopyBookingCode(selectedTicket.bookingCode)}
                        className="hover:text-white"
                        title="Sao chép"
                      >
                        <span className="material-symbols-outlined text-xs">content_copy</span>
                      </button>
                    </span>

                    <span
                      className={cn(
                        'inline-flex items-center px-2.5 py-0.5 rounded-full text-[11px] font-bold uppercase tracking-wider',
                        selectedTicket.status === 'upcoming'
                          ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                          : selectedTicket.status === 'cancelled'
                          ? 'bg-amber-500/15 text-amber-300 border border-amber-500/30'
                          : 'bg-surface-high text-outline border border-white/5'
                      )}
                    >
                      {selectedTicket.status === 'upcoming'
                        ? 'Vé có hiệu lực'
                        : selectedTicket.status === 'cancelled'
                        ? 'Đã hủy vé'
                        : 'Đã sử dụng'}
                    </span>
                  </div>

                  <h2
                    id="modal-ticket-title"
                    className="font-headline text-2xl sm:text-[28px] font-bold text-white leading-tight mt-1"
                  >
                    {selectedTicket.movieTitle}
                  </h2>
                  <p className="text-[13px] text-outline">
                    Định dạng: {selectedTicket.format} • Cổng thanh toán: {selectedTicket.paymentMethod}
                  </p>
                </div>

                {/* Body Modal: Chi tiết suất chiếu & Thông tin rạp */}
                <div className="p-6 sm:p-7 flex flex-col gap-5">
                  <div className="grid grid-cols-2 gap-3.5">
                    {/* Ngày giờ suất chiếu */}
                    <div className="col-span-2 flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          calendar_clock
                        </span>
                        <span>Ngày giờ suất chiếu</span>
                      </div>
                      <p className="text-sm font-semibold text-on-surface">
                        {selectedTicket.showTime} — {selectedTicket.showDate}
                      </p>
                    </div>

                    {/* Rạp */}
                    <div className="col-span-2 flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          apartment
                        </span>
                        <span>Rạp chiếu</span>
                      </div>
                      <p className="text-sm font-semibold text-on-surface">
                        {selectedTicket.theaterName}
                      </p>
                      <span className="text-xs text-outline">{selectedTicket.theaterAddress}</span>
                    </div>

                    {/* Phòng chiếu */}
                    <div className="flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          meeting_room
                        </span>
                        <span>Phòng chiếu</span>
                      </div>
                      <p className="text-sm font-semibold text-on-surface">
                        {selectedTicket.roomName}
                      </p>
                    </div>

                    {/* Ghế */}
                    <div className="flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          event_seat
                        </span>
                        <span>Ghế ngồi</span>
                      </div>
                      <p className="font-headline text-lg font-bold text-primary-light tracking-wide">
                        {selectedTicket.seats.join(', ')}
                      </p>
                    </div>

                    {/* Thời gian đặt */}
                    <div className="flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          receipt
                        </span>
                        <span>Thời gian đặt</span>
                      </div>
                      <p className="text-sm font-semibold text-on-surface">
                        {selectedTicket.bookingDate}
                      </p>
                    </div>

                    {/* Thành tiền */}
                    <div className="flex flex-col gap-1 bg-surface-container p-3.5 rounded-xl border border-white/5">
                      <div className="flex items-center gap-1.5 text-[11px] text-outline uppercase tracking-wider">
                        <span className="material-symbols-outlined text-base text-tertiary">
                          payments
                        </span>
                        <span>Thành tiền</span>
                      </div>
                      <p className="font-headline text-lg font-bold text-secondary">
                        {selectedTicket.totalPrice.toLocaleString('vi-VN')}đ
                      </p>
                    </div>
                  </div>

                  {/* KHUNG KIỂM TRA ĐIỀU KIỆN 120 PHÚT & NÚT ĐỔI/HỦY VÉ */}
                  {selectedTicket.status === 'upcoming' && (
                    <div className="rounded-2xl border p-4.5 flex flex-col gap-3 transition-all">
                      {isSelectedCancelable ? (
                        /* ĐỦ ĐIỀU KIỆN (>= 120 PHÚT) */
                        <div className="flex flex-col gap-3 bg-emerald-950/20 border border-emerald-500/30 p-4 rounded-xl">
                          <div className="flex items-start gap-3">
                            <span className="material-symbols-outlined text-emerald-400 text-2xl shrink-0 mt-0.5">
                              verified
                            </span>
                            <div className="space-y-0.5">
                              <h4 className="text-sm font-bold text-white">
                                Đủ điều kiện hủy vé &amp; hoàn tiền 100%
                              </h4>
                              <p className="text-xs text-zinc-300 leading-relaxed">
                                Thời gian còn lại đến giờ chiếu:{' '}
                                <span className="font-bold text-emerald-400">
                                  {formatRemainingTime(selectedMinutesLeft)}
                                </span>{' '}
                                (đạt yêu cầu &ge; 120 phút). Hoàn 100% số tiền vào tài khoản/ví ban đầu.
                              </p>
                            </div>
                          </div>

                          {/* Nút bấm Đổi vé / Hủy vé khả dụng */}
                          <button
                            type="button"
                            onClick={() => setShowCancelModal(true)}
                            className="w-full py-3 px-4 rounded-xl bg-red-600/15 hover:bg-red-600 text-red-400 hover:text-white border border-red-500/40 hover:border-red-600 font-bold text-sm transition-all flex items-center justify-center gap-2 cursor-pointer shadow-sm active:scale-[0.99]"
                          >
                            <span className="material-symbols-outlined text-[20px]">
                              cancel_presentation
                            </span>
                            <span>Đổi vé / Hủy vé</span>
                          </button>
                        </div>
                      ) : (
                        /* KHÔNG ĐỦ ĐIỀU KIỆN (< 120 PHÚT) - HIỂN THỊ CẢNH BÁO RÕ RÀNG */
                        <div className="flex flex-col gap-3 bg-red-950/25 border border-red-500/30 p-4 rounded-xl">
                          <div className="flex items-start gap-3">
                            <span className="material-symbols-outlined text-red-400 text-2xl shrink-0 mt-0.5">
                              lock_clock
                            </span>
                            <div className="space-y-1">
                              <h4 className="text-sm font-bold text-red-300">
                                Không thể hủy vé trực tuyến sát giờ chiếu
                              </h4>
                              {/* Dòng cảnh báo bắt buộc theo nghiệp vụ */}
                              <p className="text-xs text-red-200/90 font-medium leading-relaxed bg-red-900/30 p-2 rounded-lg border border-red-500/20">
                                ⚠️ Chức năng hủy trực tuyến chỉ khả dụng trước giờ chiếu ít nhất 120 phút.
                              </p>
                              <p className="text-[11px] text-zinc-400">
                                Hiện chỉ còn{' '}
                                <span className="text-amber-400 font-bold">
                                  {formatRemainingTime(selectedMinutesLeft)}
                                </span>{' '}
                                trước giờ bắt đầu. Vui lòng liên hệ trực tiếp quầy vé tại rạp để được hỗ trợ.
                              </p>
                            </div>
                          </div>

                          {/* Nút Hủy vé bị Disable */}
                          <button
                            type="button"
                            disabled
                            className="w-full py-3 px-4 rounded-xl bg-zinc-800/80 text-zinc-500 border border-white/5 font-semibold text-sm flex items-center justify-center gap-2 cursor-not-allowed opacity-60"
                            title="Chức năng hủy trực tuyến chỉ khả dụng trước giờ chiếu ít nhất 120 phút."
                          >
                            <span className="material-symbols-outlined text-[18px]">lock</span>
                            <span>Đổi vé / Hủy vé (Đã bị khóa)</span>
                          </button>
                        </div>
                      )}
                    </div>
                  )}

                  {/* THÔNG BÁO VÉ ĐÃ HỦY */}
                  {selectedTicket.status === 'cancelled' && (
                    <div className="bg-amber-950/25 border border-amber-500/30 p-4 rounded-2xl flex flex-col gap-2">
                      <div className="flex items-center gap-2 text-amber-400 font-bold text-sm">
                        <span className="material-symbols-outlined text-xl">replay</span>
                        <span>Vé này đã được hủy thành công</span>
                      </div>
                      <p className="text-xs text-zinc-300 leading-relaxed">
                        Lý do hủy:{' '}
                        <span className="text-white font-medium">
                          {selectedTicket.cancelReason || 'Thay đổi lịch trình cá nhân.'}
                        </span>
                      </p>
                      <div className="pt-2 flex items-center justify-between border-t border-white/10">
                        <span className="text-[11px] text-zinc-400">
                          Trạng thái: <strong className="text-amber-300">Đã hoàn tiền</strong>
                        </span>
                        <Link
                          to="/profile?tab=transactions"
                          className="text-xs text-emerald-400 hover:text-emerald-300 font-semibold flex items-center gap-1"
                        >
                          <span>Xem Lịch sử giao dịch</span>
                          <span className="material-symbols-outlined text-sm">arrow_forward</span>
                        </Link>
                      </div>
                    </div>
                  )}

                  {/* NÚT LẤY MÃ VÉ QR / BARCODE */}
                  <div className="flex flex-col gap-4 pt-1">
                    <button
                      type="button"
                      className="inline-flex items-center justify-center gap-2.5 w-full py-3.5 px-6 rounded-2xl text-[15px] font-bold border-none cursor-pointer bg-gradient-to-r from-primary to-[#b80610] text-white shadow-glow-primary hover:from-primary-hover hover:to-primary hover:-translate-y-0.5 transition-all"
                      onClick={handleToggleQrCode}
                    >
                      <span className="material-symbols-outlined text-[22px]">
                        {showQrCode ? 'visibility_off' : 'qr_code_scanner'}
                      </span>
                      <span>{showQrCode ? 'Ẩn mã vé điện tử' : 'Lấy mã vé'}</span>
                    </button>

                    {/* Khung hiển thị QR Code & Barcode */}
                    {showQrCode && (
                      <div className="bg-surface-lowest border border-white/20 rounded-2xl p-6 flex flex-col items-center gap-4.5 animate-fade-in-down">
                        <div className="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-secondary">
                          <span className="material-symbols-outlined text-lg">sensors</span>
                          <span>Đưa mã này cho nhân viên soát vé tại rạp</span>
                        </div>

                        <div className="bg-white rounded-2xl p-5 shadow-2xl flex flex-col items-center gap-3.5 w-full max-w-[280px] box-border">
                          {/* Mã QR Code Vector */}
                          <div className="size-40 flex items-center justify-center">
                            <svg
                              viewBox="0 0 120 120"
                              className="size-full"
                              aria-label={`Mã QR vé ${selectedTicket.bookingCode}`}
                            >
                              <rect x="10" y="10" width="30" height="30" rx="4" fill="#0f131d" />
                              <rect x="16" y="16" width="18" height="18" rx="2" fill="#ffffff" />
                              <rect x="20" y="20" width="10" height="10" rx="1" fill="#0f131d" />

                              <rect x="80" y="10" width="30" height="30" rx="4" fill="#0f131d" />
                              <rect x="86" y="16" width="18" height="18" rx="2" fill="#ffffff" />
                              <rect x="90" y="20" width="10" height="10" rx="1" fill="#0f131d" />

                              <rect x="10" y="80" width="30" height="30" rx="4" fill="#0f131d" />
                              <rect x="16" y="86" width="18" height="18" rx="2" fill="#ffffff" />
                              <rect x="20" y="90" width="10" height="10" rx="1" fill="#0f131d" />

                              <rect x="48" y="12" width="6" height="14" fill="#0f131d" />
                              <rect x="62" y="16" width="8" height="6" fill="#0f131d" />
                              <rect x="52" y="32" width="14" height="6" fill="#0f131d" />
                              <rect x="46" y="46" width="28" height="28" rx="4" fill="#e50914" />
                              <text
                                x="60"
                                y="65"
                                fontSize="13"
                                fontWeight="bold"
                                fill="#ffffff"
                                textAnchor="middle"
                                fontFamily="Space Grotesk, sans-serif"
                              >
                                CT
                              </text>
                            </svg>
                          </div>

                          {/* Barcode */}
                          <div className="w-full flex flex-col items-center gap-1 pt-2 border-t border-dashed border-gray-300">
                            <span className="font-headline text-sm font-bold text-[#11141e] tracking-[0.15em]">
                              #{selectedTicket.bookingCode}
                            </span>
                          </div>
                        </div>

                        <button
                          type="button"
                          className="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-surface-high text-on-surface border border-white/10 text-[13px] font-semibold cursor-pointer transition-all hover:bg-surface-highest hover:text-white"
                          onClick={() => handleCopyBookingCode(selectedTicket.bookingCode)}
                        >
                          <span className="material-symbols-outlined text-lg text-tertiary">
                            {copiedCode ? 'check' : 'content_copy'}
                          </span>
                          <span>{copiedCode ? 'Đã sao chép mã vé!' : 'Sao chép mã vé'}</span>
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* ================================================================
              BƯỚC 3: MODAL CHỌN LÝ DO & XÁC NHẬN HỦY VÉ
              ================================================================ */}
          {showCancelModal && selectedTicket && (
            <div
              className="fixed inset-0 z-50 bg-[#0a0e18]/92 backdrop-blur-md flex items-center justify-center p-4 animate-fade-in"
              onClick={() => setShowCancelModal(false)}
              role="dialog"
              aria-modal="true"
              aria-labelledby="modal-cancel-title"
            >
              <div
                className="relative w-full max-w-lg bg-[#11141e] border border-white/20 rounded-3xl p-6 sm:p-7 shadow-modal space-y-5 animate-modal-scale"
                onClick={(e) => e.stopPropagation()}
              >
                {/* Header Modal Hủy vé */}
                <div className="flex items-center justify-between pb-4 border-b border-white/10">
                  <div className="flex items-center gap-3">
                    <div className="size-11 rounded-2xl bg-red-600/15 border border-red-500/30 text-red-500 flex items-center justify-center">
                      <span className="material-symbols-outlined text-2xl">
                        remove_shopping_cart
                      </span>
                    </div>
                    <div>
                      <h3 id="modal-cancel-title" className="font-headline text-lg font-bold text-white">
                        Xác Nhận Hủy Vé &amp; Hoàn Tiền
                      </h3>
                      <p className="text-[11px] text-zinc-400">
                        Vé #{selectedTicket.bookingCode} • {selectedTicket.movieTitle}
                      </p>
                    </div>
                  </div>

                  <button
                    type="button"
                    onClick={() => setShowCancelModal(false)}
                    className="size-8 rounded-xl bg-white/5 hover:bg-white/10 text-zinc-400 hover:text-white flex items-center justify-center transition-colors cursor-pointer"
                  >
                    <span className="material-symbols-outlined text-[20px]">close</span>
                  </button>
                </div>

                {cancelError && (
                  <div className="p-3 rounded-xl bg-red-950/50 border border-red-500/30 text-xs text-red-300 flex items-center gap-2">
                    <span className="material-symbols-outlined text-[18px]">error</span>
                    <span>{cancelError}</span>
                  </div>
                )}

                {/* Tóm tắt thông tin vé */}
                <div className="bg-[#171b26] rounded-2xl p-4 border border-white/5 space-y-2 text-xs">
                  <div className="flex justify-between items-center text-zinc-300">
                    <span className="text-zinc-400">Rạp &amp; Phòng:</span>
                    <span className="font-semibold text-white">
                      {selectedTicket.theaterName} ({selectedTicket.roomName})
                    </span>
                  </div>
                  <div className="flex justify-between items-center text-zinc-300">
                    <span className="text-zinc-400">Suất chiếu:</span>
                    <span className="font-semibold text-amber-400">
                      {selectedTicket.showTime} — {selectedTicket.showDate}
                    </span>
                  </div>
                  <div className="flex justify-between items-center text-zinc-300">
                    <span className="text-zinc-400">Ghế ngồi:</span>
                    <span className="font-headline font-bold text-primary-light">
                      {selectedTicket.seats.join(', ')}
                    </span>
                  </div>
                </div>

                {/* Danh sách 4 Lý do hủy vé */}
                <div className="space-y-3">
                  <label className="text-xs font-bold text-white uppercase tracking-wider block">
                    Vui lòng chọn lý do hủy vé <span className="text-red-500">*</span>:
                  </label>
                  <div className="space-y-2">
                    {CANCELLATION_REASONS.map((reason, index) => (
                      <label
                        key={index}
                        className={cn(
                          'flex items-center gap-3 p-3 rounded-xl border text-xs cursor-pointer transition-all',
                          selectedReason === reason
                            ? 'bg-red-600/10 border-red-500/50 text-white font-medium'
                            : 'bg-[#171b26]/70 border-white/5 text-zinc-300 hover:bg-[#171b26] hover:border-white/10'
                        )}
                      >
                        <input
                          type="radio"
                          name="cancelReason"
                          value={reason}
                          checked={selectedReason === reason}
                          onChange={() => {
                            setSelectedReason(reason);
                            setCancelError('');
                          }}
                          className="accent-[#e50914] size-4 cursor-pointer"
                        />
                        <span>{reason}</span>
                      </label>
                    ))}
                  </div>

                  {/* Textarea khi chọn "Lý do khác" */}
                  {selectedReason === 'Lý do khác' && (
                    <div className="pt-1">
                      <textarea
                        rows={3}
                        value={otherReasonText}
                        onChange={(e) => {
                          setOtherReasonText(e.target.value);
                          setCancelError('');
                        }}
                        placeholder="Vui lòng nhập lý do chi tiết để Vibe Cinema cải thiện dịch vụ..."
                        className="w-full bg-[#121620] border border-zinc-800 focus:border-[#e50914] rounded-xl p-3 text-xs text-white placeholder-zinc-500 focus:outline-none transition-colors resize-none"
                      />
                    </div>
                  )}
                </div>

                {/* Tóm tắt số tiền dự kiến hoàn lại (100% giá trị vé) */}
                <div className="bg-[#171b26]/80 rounded-2xl p-4.5 border border-white/5 space-y-2.5 text-xs">
                  <div className="flex justify-between items-center text-zinc-400">
                    <span>Giá trị vé ban đầu:</span>
                    <span className="text-white font-medium">
                      {selectedTicket.totalPrice.toLocaleString('vi-VN')} ₫
                    </span>
                  </div>
                  <div className="flex justify-between items-center text-zinc-400">
                    <span>Phí hủy vé trực tuyến:</span>
                    <span className="text-emerald-400 font-semibold">0 ₫ (Miễn phí trước 120 phút)</span>
                  </div>
                  <div className="flex justify-between items-center text-zinc-400">
                    <span>Phương thức nhận tiền:</span>
                    <span className="text-white font-medium">
                      Hoàn về {selectedTicket.paymentMethod}
                    </span>
                  </div>
                  <div className="pt-2 border-t border-white/10 flex justify-between items-baseline">
                    <span className="font-bold text-white text-sm">Số tiền hoàn lại (100%):</span>
                    <span className="font-headline text-xl font-bold text-emerald-400">
                      {selectedTicket.totalPrice.toLocaleString('vi-VN')} ₫
                    </span>
                  </div>
                </div>

                {/* Nút hành động Xác nhận hủy vé */}
                <div className="flex items-center justify-end gap-3 pt-2">
                  <button
                    type="button"
                    onClick={() => setShowCancelModal(false)}
                    className="px-4 py-2.5 rounded-xl bg-[#1c202c] hover:bg-[#272e3f] text-zinc-300 text-xs font-semibold transition-colors cursor-pointer"
                  >
                    Quay lại
                  </button>
                  <button
                    type="button"
                    onClick={handleConfirmCancelTicket}
                    className="px-5 py-2.5 rounded-xl bg-[#e50914] hover:bg-[#f40612] text-white text-xs font-bold transition-all shadow-md shadow-red-900/40 cursor-pointer flex items-center gap-1.5 active:scale-95"
                  >
                    <span className="material-symbols-outlined text-[17px]">check</span>
                    <span>Xác nhận hủy vé</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </MainLayout>
  );
};

export default MyTicket;
