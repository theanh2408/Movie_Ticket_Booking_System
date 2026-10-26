import React, { useState, useRef, useEffect, useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { MainLayout } from '../../layouts/MainLayout';
import { Button } from '../../components/Button';
import { MOCK_USER, INITIAL_TRANSACTIONS } from '../../assets/mockData';
import { cn } from '../../lib/cn';

export const AccountProfile = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const paramTab = searchParams.get('tab');

  // Active view tab: 'all' | 'profile' | 'transactions'
  const [activeTab, setActiveTab] = useState(() => {
    if (paramTab === 'transactions') return 'transactions';
    if (paramTab === 'profile') return 'profile';
    return 'all';
  });

  useEffect(() => {
    if (paramTab === 'transactions') {
      setActiveTab('transactions');
    } else if (paramTab === 'profile') {
      setActiveTab('profile');
    }
  }, [paramTab]);

  // Load current user from localStorage or fallback to MOCK_USER
  const [user, setUser] = useState(() => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_user');
      return stored ? JSON.parse(stored) : MOCK_USER;
    } catch {
      return MOCK_USER;
    }
  });

  const fileInputRef = useRef(null);
  const transactionsSectionRef = useRef(null);

  // Form states
  const [fullName, setFullName] = useState(user.name || '');
  const [email] = useState(user.email || 'nguyenvanan@vibecinema.vn');
  const [phone] = useState(user.phone || '0912 345 678');
  const [gender, setGender] = useState(user.gender || 'Nam');
  const [dob, setDob] = useState(user.dob || '1995-08-15');
  const [avatar, setAvatar] = useState(user.avatar || '');

  // Modal change password state
  const [showPasswordModal, setShowPasswordModal] = useState(false);
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordError, setPasswordError] = useState('');

  // Selected invoice for detail modal
  const [selectedInvoice, setSelectedInvoice] = useState(null);

  // Search & Filter states for Transaction History
  const [searchQuery, setSearchQuery] = useState('');
  const [paymentFilter, setPaymentFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all'); // 'all' | 'success' | 'failed' | 'refunded'

  // Toast notification
  const [toastMessage, setToastMessage] = useState(null);
  const [toastType, setToastType] = useState('success'); // 'success' | 'error'

  const showToast = (msg, type = 'success') => {
    setToastMessage(msg);
    setToastType(type);
    setTimeout(() => {
      setToastMessage(null);
    }, 3500);
  };

  // Format current date time helper
  const getCurrentFormattedTime = () => {
    const now = new Date();
    const d = String(now.getDate()).padStart(2, '0');
    const m = String(now.getMonth() + 1).padStart(2, '0');
    const y = now.getFullYear();
    const h = String(now.getHours()).padStart(2, '0');
    const min = String(now.getMinutes()).padStart(2, '0');
    return `${d}/${m}/${y} ${h}:${min}`;
  };

  // Load transactions from localStorage or INITIAL_TRANSACTIONS
  const [transactions, setTransactions] = useState(() => {
    try {
      const storedTx = localStorage.getItem('Vibe Cinema_transactions');
      const customTransactions = storedTx ? JSON.parse(storedTx) : [];

      const storedTickets = localStorage.getItem('Vibe Cinema_tickets');
      const tickets = storedTickets ? JSON.parse(storedTickets) : [];

      const ticketTransactions = tickets.map((t, idx) => ({
        id: t.id || `ticket-tx-${idx}`,
        transactionTime:
          t.cancelledAt ||
          (t.bookingDate === 'Vừa xong'
            ? getCurrentFormattedTime()
            : t.bookingDate || '25/10/2026 14:00'),
        paymentMethod: t.paymentMethod || 'Ví MoMo',
        invoiceCode: t.bookingCode
          ? t.bookingCode.replace('CT-', 'HD-')
          : `HD-${800000 + idx}`,
        theaterInfo: `${t.theaterName || 'Vibe Cinema'} - ${t.roomName || 'Phòng 1'}`,
        theaterName: t.theaterName || 'Vibe Cinema',
        roomName: t.roomName || 'Phòng Chiếu 1',
        movieTitle: t.movieTitle || 'Vé xem phim',
        seats: t.seats || ['Ghế VIP'],
        ticketCount: Array.isArray(t.seats) ? t.seats.length : 1,
        totalAmount: t.totalPrice || 190000,
        status:
          t.status === 'cancelled'
            ? 'Đã hoàn tiền'
            : t.status === 'failed'
            ? 'Thanh toán không thành công'
            : 'Thanh toán thành công',
        refundReason: t.cancelReason || '',
      }));

      // Deduplicate by invoiceCode
      const existingCodes = new Set();
      const merged = [];

      // Ưu tiên customTransactions mới nhất (giao dịch hoàn tiền vừa tạo)
      customTransactions.forEach((tx) => {
        if (!existingCodes.has(tx.invoiceCode)) {
          existingCodes.add(tx.invoiceCode);
          merged.push(tx);
        }
      });

      // Kế tiếp là các giao dịch tạo từ vé
      ticketTransactions.forEach((tx) => {
        if (!existingCodes.has(tx.invoiceCode)) {
          existingCodes.add(tx.invoiceCode);
          merged.push(tx);
        }
      });

      // Cuối cùng là mock ban đầu
      INITIAL_TRANSACTIONS.forEach((tx) => {
        if (!existingCodes.has(tx.invoiceCode)) {
          existingCodes.add(tx.invoiceCode);
          merged.push(tx);
        }
      });

      return merged;
    } catch {
      return INITIAL_TRANSACTIONS;
    }
  });

  // Filtered transactions
  const filteredTransactions = useMemo(() => {
    return transactions.filter((tx) => {
      // Status filter
      if (statusFilter !== 'all') {
        const s = (tx.status || '').toLowerCase();
        if (statusFilter === 'success') {
          if (s.includes('không') || s.includes('hoàn') || s.includes('thất bại')) return false;
        } else if (statusFilter === 'failed') {
          if (!s.includes('không') && !s.includes('thất bại')) return false;
        } else if (statusFilter === 'refunded') {
          if (!s.includes('hoàn')) return false;
        }
      }

      // Payment filter
      if (paymentFilter !== 'all') {
        const method = (tx.paymentMethod || '').toLowerCase();
        if (paymentFilter === 'momo' && !method.includes('momo')) return false;
        if (paymentFilter === 'vnpay' && !method.includes('vnpay')) return false;
        if (paymentFilter === 'shopeepay' && !method.includes('shopee')) return false;
        if (
          paymentFilter === 'card' &&
          !method.includes('visa') &&
          !method.includes('mastercard') &&
          !method.includes('quốc tế')
        )
          return false;
        if (
          paymentFilter === 'napas' &&
          !method.includes('napas') &&
          !method.includes('atm') &&
          !method.includes('nội địa')
        )
          return false;
      }

      // Search query
      if (searchQuery.trim()) {
        const query = searchQuery.toLowerCase().trim();
        const code = (tx.invoiceCode || '').toLowerCase();
        const theater = (tx.theaterInfo || tx.theaterName || '').toLowerCase();
        const movie = (tx.movieTitle || '').toLowerCase();
        const method = (tx.paymentMethod || '').toLowerCase();
        const status = (tx.status || '').toLowerCase();
        return (
          code.includes(query) ||
          theater.includes(query) ||
          movie.includes(query) ||
          method.includes(query) ||
          status.includes(query)
        );
      }

      return true;
    });
  }, [transactions, paymentFilter, statusFilter, searchQuery]);

  // Statistics
  const stats = useMemo(() => {
    let spent = 0;
    let successCount = 0;
    let failedCount = 0;
    let refundCount = 0;

    transactions.forEach((tx) => {
      const s = (tx.status || '').toLowerCase();
      if (s.includes('không') || s.includes('thất bại')) {
        failedCount++;
      } else if (s.includes('hoàn')) {
        refundCount++;
      } else {
        successCount++;
        spent += tx.totalAmount || 0;
      }
    });

    return { spent, successCount, failedCount, refundCount };
  }, [transactions]);

  // Handle avatar upload and compression via canvas
  const handleImageUpload = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      showToast('Vui lòng chọn tệp hình ảnh hợp lệ (PNG, JPG, WEBP).', 'error');
      return;
    }

    if (file.size > 5 * 1024 * 1024) {
      showToast('Kích thước ảnh tối đa cho phép là 5MB.', 'error');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const img = new Image();
      img.onload = () => {
        const canvas = document.createElement('canvas');
        const targetSize = 300;
        canvas.width = targetSize;
        canvas.height = targetSize;
        const ctx = canvas.getContext('2d');

        const minSide = Math.min(img.width, img.height);
        const startX = (img.width - minSide) / 2;
        const startY = (img.height - minSide) / 2;

        ctx.drawImage(img, startX, startY, minSide, minSide, 0, 0, targetSize, targetSize);

        const optimizedDataUrl = canvas.toDataURL('image/jpeg', 0.85);
        setAvatar(optimizedDataUrl);

        const updatedUser = {
          ...user,
          avatar: optimizedDataUrl,
        };
        try {
          localStorage.setItem('Vibe Cinema_user', JSON.stringify(updatedUser));
          setUser(updatedUser);
          window.dispatchEvent(new Event('authChange'));
          showToast('Tải lên ảnh đại diện thành công!', 'success');
        } catch {
          showToast('Không thể lưu ảnh đại diện. Dung lượng ảnh quá lớn.', 'error');
        }
      };
      img.src = event.target?.result;
    };
    reader.readAsDataURL(file);

    e.target.value = '';
  };

  const handleRemoveAvatar = (e) => {
    e.stopPropagation();
    setAvatar('');
    const updatedUser = {
      ...user,
      avatar: '',
    };
    try {
      localStorage.setItem('Vibe Cinema_user', JSON.stringify(updatedUser));
      setUser(updatedUser);
      window.dispatchEvent(new Event('authChange'));
      showToast('Đã xóa ảnh đại diện.', 'success');
    } catch {
      // no-op
    }
  };

  // Handle profile update
  const handleUpdateProfile = (e) => {
    e.preventDefault();

    if (!fullName.trim()) {
      showToast('Vui lòng nhập Họ và tên.', 'error');
      return;
    }

    if (!dob) {
      showToast('Vui lòng chọn Ngày sinh.', 'error');
      return;
    }

    const updatedUser = {
      ...user,
      name: fullName.trim(),
      gender,
      dob,
      avatar,
    };

    try {
      localStorage.setItem('Vibe Cinema_user', JSON.stringify(updatedUser));
      setUser(updatedUser);
      window.dispatchEvent(new Event('authChange'));
      showToast('Cập nhật thông tin tài khoản thành công!', 'success');
    } catch {
      showToast('Đã có lỗi xảy ra khi lưu thông tin.', 'error');
    }
  };

  // Handle password change
  const handleChangePassword = (e) => {
    e.preventDefault();
    setPasswordError('');

    if (!currentPassword.trim()) {
      setPasswordError('Vui lòng nhập mật khẩu hiện tại.');
      return;
    }

    if (!newPassword || newPassword.length < 6) {
      setPasswordError('Mật khẩu mới phải có ít nhất 6 ký tự.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setPasswordError('Mật khẩu xác nhận không khớp với mật khẩu mới.');
      return;
    }

    const updatedUser = {
      ...user,
      password: newPassword,
    };

    try {
      localStorage.setItem('Vibe Cinema_user', JSON.stringify(updatedUser));
      setUser(updatedUser);
      setShowPasswordModal(false);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      showToast('Đổi mật khẩu thành công!', 'success');
    } catch {
      setPasswordError('Không thể lưu mật khẩu mới. Vui lòng thử lại.');
    }
  };

  // Copy invoice code helper
  const handleCopyCode = (code) => {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(code);
      showToast(`Đã sao chép mã hoá đơn: ${code}`, 'success');
    }
  };

  // Scroll smoothly to transactions
  const scrollToTransactions = () => {
    setActiveTab('all');
    setTimeout(() => {
      transactionsSectionRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, 100);
  };

  // Helper to render payment method badges
  const renderPaymentBadge = (method) => {
    const m = (method || '').toLowerCase();
    if (m.includes('momo')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-pink-500/15 text-pink-400 border border-pink-500/30">
          <span className="material-symbols-outlined text-[15px]">account_balance_wallet</span>
          <span>Ví MoMo</span>
        </span>
      );
    }
    if (m.includes('vnpay')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-blue-500/15 text-blue-400 border border-blue-500/30">
          <span className="material-symbols-outlined text-[15px]">qr_code_scanner</span>
          <span>VNPAY-QR</span>
        </span>
      );
    }
    if (m.includes('shopee')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-orange-500/15 text-orange-400 border border-orange-500/30">
          <span className="material-symbols-outlined text-[15px]">shopping_bag</span>
          <span>ShopeePay</span>
        </span>
      );
    }
    if (m.includes('visa') || m.includes('mastercard') || m.includes('quốc tế')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-indigo-500/15 text-indigo-300 border border-indigo-500/30">
          <span className="material-symbols-outlined text-[15px]">credit_card</span>
          <span>Thẻ Quốc Tế</span>
        </span>
      );
    }
    if (m.includes('napas') || m.includes('atm') || m.includes('nội địa')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-teal-500/15 text-teal-300 border border-teal-500/30">
          <span className="material-symbols-outlined text-[15px]">account_balance</span>
          <span>Thẻ ATM Napas</span>
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-zinc-800 text-zinc-300 border border-white/10">
        <span className="material-symbols-outlined text-[15px]">payments</span>
        <span>{method || 'Trực tuyến'}</span>
      </span>
    );
  };

  // Helper to render transaction status badges:
  // "Thanh toán thành công" | "Thanh toán không thành công" | "Đã hoàn tiền"
  const renderStatusBadge = (status) => {
    const s = (status || '').toLowerCase();
    if (s.includes('không') || s.includes('thất bại') || s.includes('failed')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-red-500/15 text-red-400 border border-red-500/30 whitespace-nowrap shadow-sm">
          <span className="material-symbols-outlined text-[15px]">cancel</span>
          <span>Thanh toán không thành công</span>
        </span>
      );
    }
    if (s.includes('hoàn') || s.includes('refund')) {
      return (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-amber-500/15 text-amber-300 border border-amber-500/30 whitespace-nowrap shadow-sm">
          <span className="material-symbols-outlined text-[15px]">replay</span>
          <span>Đã hoàn tiền</span>
        </span>
      );
    }
    return (
      <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 whitespace-nowrap shadow-sm">
        <span className="material-symbols-outlined text-[15px]">check_circle</span>
        <span>Thanh toán thành công</span>
      </span>
    );
  };

  return (
    <MainLayout>
      <div className="w-full px-4 sm:px-8 lg:px-12 py-10 max-w-5xl mx-auto space-y-8">
        {/* Toast Alert */}
        {toastMessage && (
          <div
            className={`fixed top-24 right-6 z-50 px-4 py-3 rounded-2xl shadow-2xl text-xs flex items-center gap-2.5 backdrop-blur-xl border transition-all animate-bounce-short ${
              toastType === 'success'
                ? 'bg-emerald-950/90 text-emerald-300 border-emerald-500/40'
                : 'bg-red-950/90 text-red-300 border-red-500/40'
            }`}
          >
            <span className="material-symbols-outlined text-[20px]">
              {toastType === 'success' ? 'check_circle' : 'error'}
            </span>
            <span className="font-medium">{toastMessage}</span>
          </div>
        )}

        {/* Hidden File Input for Avatar Upload */}
        <input
          type="file"
          ref={fileInputRef}
          accept="image/png, image/jpeg, image/jpg, image/webp"
          onChange={handleImageUpload}
          className="hidden"
        />

        {/* Top Header Card: User Summary & Interactive Avatar */}
        <div className="p-6 sm:p-8 rounded-3xl bg-[#11141e] border border-white/10 shadow-2xl flex flex-col md:flex-row items-start md:items-center justify-between gap-6">
          <div className="flex items-center gap-4 sm:gap-6">
            {/* Clickable Avatar Box */}
            <div
              className="relative group cursor-pointer shrink-0"
              onClick={() => fileInputRef.current?.click()}
              title="Bấm vào đây để tải ảnh đại diện lên"
            >
              <div className="size-20 sm:size-24 rounded-2xl overflow-hidden ring-2 ring-white/15 shadow-xl relative flex items-center justify-center bg-gradient-to-br from-[#e50914] to-[#93000a] text-white">
                {avatar ? (
                  <img
                    src={avatar}
                    alt={user.name}
                    className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
                  />
                ) : (
                  <span className="font-headline text-3xl sm:text-4xl font-bold select-none">
                    {user.name ? user.name.charAt(0).toUpperCase() : 'U'}
                  </span>
                )}

                {/* Dark Hover Overlay */}
                <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex flex-col items-center justify-center text-white gap-0.5 backdrop-blur-[2px]">
                  <span className="material-symbols-outlined text-[24px]">photo_camera</span>
                  <span className="text-[10px] font-semibold">Đổi avatar</span>
                </div>
              </div>

              {/* Camera Icon Badge Button */}
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  fileInputRef.current?.click();
                }}
                className="absolute -bottom-1 -right-1 size-7 sm:size-8 rounded-full bg-[#e50914] hover:bg-[#f40612] text-white flex items-center justify-center shadow-lg border-2 border-[#11141e] transition-transform group-hover:scale-110 cursor-pointer"
                title="Tải ảnh lên"
              >
                <span className="material-symbols-outlined text-[15px] sm:text-[17px]">
                  photo_camera
                </span>
              </button>
            </div>

            {/* User Meta Info & Avatar Action Links */}
            <div className="space-y-1.5">
              <div className="flex flex-wrap items-center gap-2.5">
                <h1 className="font-headline text-xl sm:text-2xl font-bold text-white">
                  {user.name || 'Người Dùng'}
                </h1>
                <span className="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-[#ffb95f]/15 text-[#ffb95f] border border-[#ffb95f]/30">
                  Thành viên Vibe Cinema
                </span>
              </div>
              <p className="text-xs text-zinc-400">
                {user.email || 'nguyenvanan@vibecinema.vn'} • {user.phone || '0912 345 678'}
              </p>

              {/* Avatar upload & remove actions */}
              <div className="flex items-center gap-3 pt-1">
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="text-xs font-medium text-amber-400 hover:text-amber-300 flex items-center gap-1 transition-colors cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[16px]">edit</span>
                  <span>Thay ảnh mới</span>
                </button>
                {avatar && (
                  <>
                    <span className="text-zinc-600">•</span>
                    <button
                      type="button"
                      onClick={handleRemoveAvatar}
                      className="text-xs font-medium text-red-400 hover:text-red-300 flex items-center gap-1 transition-colors cursor-pointer"
                    >
                      <span className="material-symbols-outlined text-[16px]">delete</span>
                      <span>Xóa ảnh</span>
                    </button>
                  </>
                )}
              </div>
            </div>
          </div>

          {/* Quick Action Navigation Links */}
          <div className="flex flex-wrap items-center gap-2.5 self-start md:self-center">
            <button
              type="button"
              onClick={scrollToTransactions}
              className="px-4 py-2.5 rounded-xl bg-[#1c202c] hover:bg-[#272e3f] text-emerald-400 hover:text-emerald-300 border border-emerald-500/30 hover:border-emerald-500/50 text-xs font-semibold flex items-center gap-2 transition-all cursor-pointer shadow-sm active:scale-95"
            >
              <span className="material-symbols-outlined text-[18px]">receipt_long</span>
              <span>Lịch sử giao dịch</span>
              <span className="px-1.5 py-0.2 rounded-full text-[10px] bg-emerald-500/20 text-emerald-300 font-bold">
                {transactions.length}
              </span>
            </button>

            <Link
              to="/my-tickets"
              className="px-4 py-2.5 rounded-xl bg-[#1a1f2c] hover:bg-[#252c3d] text-zinc-300 hover:text-white border border-white/10 text-xs font-semibold flex items-center gap-2 transition-colors cursor-pointer shadow-sm"
            >
              <span className="material-symbols-outlined text-[18px] text-amber-400">
                confirmation_number
              </span>
              <span>Xem vé của tôi</span>
            </Link>
          </div>
        </div>

        {/* Navigation Tabs Switcher */}
        <div className="flex items-center gap-2 p-1.5 rounded-2xl bg-[#11141e] border border-white/10 w-fit">
          <button
            type="button"
            onClick={() => {
              setActiveTab('all');
              setSearchParams({});
            }}
            className={cn(
              'px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-2',
              activeTab === 'all'
                ? 'bg-[#e50914] text-white shadow-md shadow-red-900/30'
                : 'text-zinc-400 hover:text-white hover:bg-white/5'
            )}
          >
            <span className="material-symbols-outlined text-[17px]">dashboard</span>
            <span>Tất cả</span>
          </button>

          <button
            type="button"
            onClick={() => {
              setActiveTab('profile');
              setSearchParams({ tab: 'profile' });
            }}
            className={cn(
              'px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-2',
              activeTab === 'profile'
                ? 'bg-[#e50914] text-white shadow-md shadow-red-900/30'
                : 'text-zinc-400 hover:text-white hover:bg-white/5'
            )}
          >
            <span className="material-symbols-outlined text-[17px]">manage_accounts</span>
            <span>Thông tin cá nhân</span>
          </button>

          <button
            type="button"
            onClick={() => {
              setActiveTab('transactions');
              setSearchParams({ tab: 'transactions' });
            }}
            className={cn(
              'px-4 py-2 rounded-xl text-xs font-bold transition-all cursor-pointer flex items-center gap-2',
              activeTab === 'transactions'
                ? 'bg-[#e50914] text-white shadow-md shadow-red-900/30'
                : 'text-zinc-400 hover:text-white hover:bg-white/5'
            )}
          >
            <span className="material-symbols-outlined text-[17px]">receipt_long</span>
            <span>Lịch sử giao dịch</span>
            <span
              className={cn(
                'px-1.5 py-0.2 rounded-full text-[10px] font-bold',
                activeTab === 'transactions'
                  ? 'bg-white/20 text-white'
                  : 'bg-emerald-500/20 text-emerald-300'
              )}
            >
              {transactions.length}
            </span>
          </button>
        </div>

        {/* =========================================================================
            SECTION 1: Profile Edit Form Card
            ========================================================================= */}
        {(activeTab === 'all' || activeTab === 'profile') && (
          <div className="p-6 sm:p-8 rounded-3xl bg-[#11141e] border border-white/10 shadow-2xl space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-white/5">
              <div>
                <div className="text-xs font-bold text-amber-400 uppercase tracking-wider">
                  Hồ Sơ Của Tôi
                </div>
                <h2 className="font-headline text-xl font-bold text-white mt-1">
                  Cập Nhật Thông Tin Tài Khoản
                </h2>
              </div>
              <span className="material-symbols-outlined text-zinc-500 text-2xl">
                manage_accounts
              </span>
            </div>

            <form onSubmit={handleUpdateProfile} className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-5 sm:gap-6">
                {/* 1. Họ và tên */}
                <div className="space-y-2">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Họ và tên <span className="text-[#e50914]">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="text"
                      value={fullName}
                      onChange={(e) => setFullName(e.target.value)}
                      placeholder="Nhập họ và tên của bạn"
                      className="w-full bg-[#121620] border border-zinc-800 hover:border-zinc-700 focus:border-[#e50914] rounded-xl px-4 py-3 text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                    />
                    <span className="absolute right-3.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-500 text-[18px] pointer-events-none">
                      badge
                    </span>
                  </div>
                </div>

                {/* 2. Email (Read-only, cannot edit) */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-semibold text-zinc-300 block">Email</label>
                    <span className="text-[11px] text-zinc-500 italic">Cố định</span>
                  </div>
                  <div className="relative">
                    <input
                      type="email"
                      value={email}
                      disabled
                      readOnly
                      className="w-full bg-[#171b26]/70 border border-white/5 text-zinc-400 rounded-xl px-4 py-3 text-sm cursor-not-allowed select-none"
                    />
                    <span className="absolute right-3.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-500 text-[18px]">
                      mail
                    </span>
                  </div>
                </div>

                {/* 3. Mật khẩu (Read-only masked) + Nút Đổi mật khẩu kế bên */}
                <div className="space-y-2 md:col-span-2">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-semibold text-zinc-300 block">Mật khẩu</label>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="relative flex-1">
                      <input
                        type="password"
                        value="••••••••••••"
                        disabled
                        readOnly
                        className="w-full bg-[#171b26]/70 border border-white/5 text-zinc-400 rounded-xl px-4 py-3 text-sm cursor-not-allowed select-none tracking-widest"
                      />
                      <span className="absolute right-3.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-500 text-[18px]">
                        lock
                      </span>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        setPasswordError('');
                        setShowPasswordModal(true);
                      }}
                      className="px-4 py-3 rounded-xl bg-[#1c202c] hover:bg-[#272e3f] text-white border border-white/10 hover:border-white/20 text-sm font-semibold whitespace-nowrap transition-all cursor-pointer flex items-center gap-1.5 shrink-0 active:scale-95"
                    >
                      <span className="material-symbols-outlined text-[18px] text-amber-400">
                        key
                      </span>
                      <span>Đổi mật khẩu</span>
                    </button>
                  </div>
                </div>

                {/* 4. Số điện thoại (Read-only, cannot edit) */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-semibold text-zinc-300 block">
                      Số điện thoại
                    </label>
                    <span className="text-[11px] text-zinc-500 italic">Cố định</span>
                  </div>
                  <div className="relative">
                    <input
                      type="text"
                      value={phone}
                      disabled
                      readOnly
                      className="w-full bg-[#171b26]/70 border border-white/5 text-zinc-400 rounded-xl px-4 py-3 text-sm cursor-not-allowed select-none"
                    />
                    <span className="absolute right-3.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-500 text-[18px]">
                      call
                    </span>
                  </div>
                </div>

                {/* 5. Giới tính (Dropdown với 3 giá trị: Nam, Nữ, Khác) */}
                <div className="space-y-2">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Giới tính <span className="text-[#e50914]">*</span>
                  </label>
                  <div className="relative">
                    <select
                      value={gender}
                      onChange={(e) => setGender(e.target.value)}
                      className="w-full bg-[#121620] border border-zinc-800 hover:border-zinc-700 focus:border-[#e50914] rounded-xl px-4 py-3 text-sm text-white focus:outline-none transition-colors appearance-none cursor-pointer pr-10"
                    >
                      <option value="Nam" className="bg-[#11141e] text-white">
                        Nam
                      </option>
                      <option value="Nữ" className="bg-[#11141e] text-white">
                        Nữ
                      </option>
                      <option value="Khác" className="bg-[#11141e] text-white">
                        Khác
                      </option>
                    </select>
                    <span className="absolute right-3.5 top-1/2 -translate-y-1/2 material-symbols-outlined text-zinc-400 pointer-events-none text-[20px]">
                      expand_more
                    </span>
                  </div>
                </div>

                {/* 6. Ngày sinh (Date Picker) */}
                <div className="space-y-2 md:col-span-2">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Ngày sinh <span className="text-[#e50914]">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="date"
                      value={dob}
                      onChange={(e) => setDob(e.target.value)}
                      className="w-full bg-[#121620] border border-zinc-800 hover:border-zinc-700 focus:border-[#e50914] rounded-xl px-4 py-3 text-sm text-white focus:outline-none transition-colors cursor-pointer [color-scheme:dark]"
                    />
                  </div>
                </div>
              </div>

              {/* 7. Nút Cập Nhật */}
              <div className="pt-2 flex justify-center">
                <button
                  type="submit"
                  className="w-full sm:w-auto px-8 py-3.5 bg-[#e50914] hover:bg-[#f40612] text-white font-bold rounded-xl shadow-lg shadow-red-900/30 transition-all duration-200 active:scale-[0.99] cursor-pointer flex items-center justify-center gap-2"
                >
                  <span className="material-symbols-outlined text-[20px]">check</span>
                  <span>Cập Nhật Thông Tin</span>
                </button>
              </div>
            </form>
          </div>
        )}

        {/* =========================================================================
            SECTION 2: LỊCH SỬ GIAO DỊCH (Bảng 6 cột đúng yêu cầu)
            6 CỘT: STT | Thời gian giao dịch | Phương thức thanh toán | Mã hoá đơn | Thông tin rạp | Tổng tiền
            ========================================================================= */}
        {(activeTab === 'all' || activeTab === 'transactions') && (
          <div
            ref={transactionsSectionRef}
            className="p-6 sm:p-8 rounded-3xl bg-[#11141e] border border-white/10 shadow-2xl space-y-6"
          >
            {/* Header section */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-white/5">
              <div>
                <div className="text-xs font-bold text-emerald-400 uppercase tracking-wider flex items-center gap-1.5">
                  <span className="material-symbols-outlined text-[16px]">receipt_long</span>
                  <span>Lịch Sử Giao Dịch & Hoá Đơn</span>
                </div>
                <h2 className="font-headline text-xl font-bold text-white mt-1">
                  Quản Lý Hoá Đơn Thanh Toán
                </h2>
                <p className="text-xs text-zinc-400 mt-0.5">
                  Xem lại lịch sử thanh toán vé xem phim, suất chiếu và hoá đơn chi tiết
                </p>
              </div>

              <div className="flex items-center gap-2 self-start sm:self-auto">
                <span className="px-3 py-1 rounded-full text-xs font-semibold bg-emerald-500/15 text-emerald-300 border border-emerald-500/30">
                  {filteredTransactions.length} hoá đơn
                </span>
              </div>
            </div>

            {/* Quick Summary Statistics Cards */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <div className="p-4 rounded-2xl bg-[#171b26] border border-white/5 flex items-center gap-3.5">
                <div className="size-11 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-400 flex items-center justify-center shrink-0">
                  <span className="material-symbols-outlined text-2xl">account_balance_wallet</span>
                </div>
                <div>
                  <div className="text-[11px] text-zinc-400 font-medium">Tổng tiền thanh toán</div>
                  <div className="font-headline text-base sm:text-lg font-bold text-emerald-400">
                    {stats.spent.toLocaleString('vi-VN')} ₫
                  </div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">
                    {stats.successCount} giao dịch thành công
                  </div>
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#171b26] border border-white/5 flex items-center gap-3.5">
                <div className="size-11 rounded-xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-400 flex items-center justify-center shrink-0">
                  <span className="material-symbols-outlined text-2xl">check_circle</span>
                </div>
                <div>
                  <div className="text-[11px] text-zinc-400 font-medium">Thanh toán thành công</div>
                  <div className="font-headline text-base sm:text-lg font-bold text-white">
                    {stats.successCount} lượt
                  </div>
                  <div className="text-[10px] text-emerald-400 font-medium mt-0.5">
                    Hiệu lực sử dụng vé
                  </div>
                </div>
              </div>

              <div className="p-4 rounded-2xl bg-[#171b26] border border-white/5 flex items-center gap-3.5">
                <div className="size-11 rounded-xl bg-amber-400/15 border border-amber-400/30 text-amber-400 flex items-center justify-center shrink-0">
                  <span className="material-symbols-outlined text-2xl">published_with_changes</span>
                </div>
                <div>
                  <div className="text-[11px] text-zinc-400 font-medium">Hoàn tiền / Thất bại</div>
                  <div className="font-headline text-base sm:text-lg font-bold text-amber-300">
                    {stats.refundCount} hoàn • {stats.failedCount} lỗi
                  </div>
                  <div className="text-[10px] text-zinc-500 mt-0.5">
                    Đã hoàn tiền vào tài khoản
                  </div>
                </div>
              </div>
            </div>

            {/* Filter and Search Bar */}
            <div className="space-y-3 pt-2">
              <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
                {/* Search input */}
                <div className="relative flex-1">
                  <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-zinc-400 text-[18px]">
                    search
                  </span>
                  <input
                    type="text"
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Tìm kiếm theo mã hoá đơn, tên rạp, phim, trạng thái..."
                    className="w-full bg-[#121620] border border-zinc-800 hover:border-zinc-700 focus:border-emerald-500 rounded-xl pl-10 pr-4 py-2.5 text-xs sm:text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                  />
                  {searchQuery && (
                    <button
                      type="button"
                      onClick={() => setSearchQuery('')}
                      className="absolute right-3 top-1/2 -translate-y-1/2 text-zinc-400 hover:text-white"
                    >
                      <span className="material-symbols-outlined text-[16px]">close</span>
                    </button>
                  )}
                </div>

                {/* Payment Method Filter */}
                <div className="flex items-center gap-2 overflow-x-auto pb-1 md:pb-0">
                  <button
                    type="button"
                    onClick={() => setPaymentFilter('all')}
                    className={cn(
                      'px-3 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer',
                      paymentFilter === 'all'
                        ? 'bg-zinc-700 text-white border border-white/20'
                        : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                    )}
                  >
                    Tất cả cổng thanh toán
                  </button>
                  <button
                    type="button"
                    onClick={() => setPaymentFilter('momo')}
                    className={cn(
                      'px-3 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer',
                      paymentFilter === 'momo'
                        ? 'bg-pink-500/20 text-pink-300 border border-pink-500/40'
                        : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                    )}
                  >
                    MoMo
                  </button>
                  <button
                    type="button"
                    onClick={() => setPaymentFilter('vnpay')}
                    className={cn(
                      'px-3 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer',
                      paymentFilter === 'vnpay'
                        ? 'bg-blue-500/20 text-blue-300 border border-blue-500/40'
                        : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                    )}
                  >
                    VNPAY
                  </button>
                  <button
                    type="button"
                    onClick={() => setPaymentFilter('card')}
                    className={cn(
                      'px-3 py-2 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer',
                      paymentFilter === 'card'
                        ? 'bg-indigo-500/20 text-indigo-300 border border-indigo-500/40'
                        : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                    )}
                  >
                    Thẻ Quốc Tế
                  </button>
                </div>
              </div>

              {/* Status Filter Chips */}
              <div className="flex flex-wrap items-center gap-2 pt-1 border-t border-white/5">
                <span className="text-xs text-zinc-400 font-medium mr-1 flex items-center gap-1">
                  <span className="material-symbols-outlined text-[16px] text-zinc-500">filter_list</span>
                  <span>Trạng thái:</span>
                </span>
                <button
                  type="button"
                  onClick={() => setStatusFilter('all')}
                  className={cn(
                    'px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer',
                    statusFilter === 'all'
                      ? 'bg-zinc-700 text-white border border-white/20'
                      : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                  )}
                >
                  Tất cả ({transactions.length})
                </button>
                <button
                  type="button"
                  onClick={() => setStatusFilter('success')}
                  className={cn(
                    'px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer flex items-center gap-1.5',
                    statusFilter === 'success'
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                      : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                  )}
                >
                  <span className="size-2 rounded-full bg-emerald-400"></span>
                  <span>Thanh toán thành công ({stats.successCount})</span>
                </button>
                <button
                  type="button"
                  onClick={() => setStatusFilter('failed')}
                  className={cn(
                    'px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer flex items-center gap-1.5',
                    statusFilter === 'failed'
                      ? 'bg-red-500/20 text-red-300 border border-red-500/40'
                      : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                  )}
                >
                  <span className="size-2 rounded-full bg-red-400"></span>
                  <span>Thanh toán không thành công ({stats.failedCount})</span>
                </button>
                <button
                  type="button"
                  onClick={() => setStatusFilter('refunded')}
                  className={cn(
                    'px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors cursor-pointer flex items-center gap-1.5',
                    statusFilter === 'refunded'
                      ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40'
                      : 'bg-[#171b26] text-zinc-400 hover:text-white border border-white/5'
                  )}
                >
                  <span className="size-2 rounded-full bg-amber-400"></span>
                  <span>Đã hoàn tiền ({stats.refundCount})</span>
                </button>
              </div>
            </div>

            {/* BẢNG LỊCH SỬ GIAO DỊCH KÈM CỘT TRẠNG THÁI */}
            <div className="overflow-x-auto rounded-2xl border border-white/10 bg-[#0d1017] shadow-inner">
              <table className="w-full text-left border-collapse min-w-[850px]">
                <thead>
                  <tr className="border-b border-white/10 bg-[#141824] text-[11px] font-bold text-zinc-300 uppercase tracking-wider">
                    {/* CỘT 1 */}
                    <th scope="col" className="py-4 px-3 text-center w-14">
                      STT
                    </th>
                    {/* CỘT 2 */}
                    <th scope="col" className="py-4 px-4 whitespace-nowrap">
                      Thời gian giao dịch
                    </th>
                    {/* CỘT 3 */}
                    <th scope="col" className="py-4 px-4 whitespace-nowrap">
                      Phương thức thanh toán
                    </th>
                    {/* CỘT 4 */}
                    <th scope="col" className="py-4 px-4 whitespace-nowrap">
                      Mã hoá đơn
                    </th>
                    {/* CỘT 5 */}
                    <th scope="col" className="py-4 px-4">
                      Thông tin rạp
                    </th>
                    {/* CỘT 6 */}
                    <th scope="col" className="py-4 px-4 text-right whitespace-nowrap">
                      Tổng tiền
                    </th>
                    {/* CỘT 7: TRẠNG THÁI */}
                    <th scope="col" className="py-4 px-4 text-center whitespace-nowrap">
                      Trạng thái
                    </th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-white/5 text-xs sm:text-sm">
                  {filteredTransactions.length === 0 ? (
                    <tr>
                      <td colSpan={7} className="py-12 text-center text-zinc-400">
                        <div className="flex flex-col items-center justify-center gap-2">
                          <span className="material-symbols-outlined text-4xl text-zinc-600">
                            receipt_long
                          </span>
                          <span className="font-semibold text-zinc-300">
                            Không tìm thấy giao dịch nào phù hợp
                          </span>
                          <span className="text-xs text-zinc-500">
                            Thử tìm kiếm với từ khóa khác hoặc thay đổi bộ lọc trạng thái / phương thức thanh toán
                          </span>
                        </div>
                      </td>
                    </tr>
                  ) : (
                    filteredTransactions.map((tx, index) => (
                      <tr
                        key={tx.id || index}
                        onClick={() => setSelectedInvoice(tx)}
                        className="hover:bg-white/[0.04] transition-colors cursor-pointer group"
                        title="Bấm vào để xem chi tiết hoá đơn"
                      >
                        {/* 1. STT */}
                        <td className="py-4 px-3 text-center font-mono font-bold text-zinc-400 text-xs sm:text-sm group-hover:text-amber-400 transition-colors">
                          #{index + 1}
                        </td>

                        {/* 2. Thời gian giao dịch */}
                        <td className="py-4 px-4 whitespace-nowrap">
                          <div className="flex items-center gap-2">
                            <span className="material-symbols-outlined text-[17px] text-zinc-500 group-hover:text-amber-400 transition-colors">
                              schedule
                            </span>
                            <span className="text-zinc-200 font-medium text-xs sm:text-sm">
                              {tx.transactionTime}
                            </span>
                          </div>
                        </td>

                        {/* 3. Phương thức thanh toán */}
                        <td className="py-4 px-4 whitespace-nowrap">
                          {renderPaymentBadge(tx.paymentMethod)}
                        </td>

                        {/* 4. Mã hoá đơn */}
                        <td className="py-4 px-4 whitespace-nowrap">
                          <div className="flex items-center gap-1.5">
                            <span className="font-mono font-bold text-xs sm:text-sm text-amber-400 bg-amber-400/10 px-2.5 py-1 rounded-lg border border-amber-400/25 group-hover:border-amber-400/50 transition-colors">
                              {tx.invoiceCode}
                            </span>
                            <button
                              type="button"
                              onClick={(e) => {
                                e.stopPropagation();
                                handleCopyCode(tx.invoiceCode);
                              }}
                              className="p-1 rounded-lg text-zinc-500 hover:text-white hover:bg-white/10 transition-colors cursor-pointer"
                              title="Sao chép mã hoá đơn"
                            >
                              <span className="material-symbols-outlined text-[16px]">
                                content_copy
                              </span>
                            </button>
                          </div>
                        </td>

                        {/* 5. Thông tin rạp */}
                        <td className="py-4 px-4">
                          <div className="space-y-0.5">
                            <div className="text-xs sm:text-sm font-semibold text-white group-hover:text-amber-300 transition-colors flex items-center gap-1.5">
                              <span className="material-symbols-outlined text-[#e50914] text-[16px] shrink-0">
                                theaters
                              </span>
                              <span>{tx.theaterName || tx.theaterInfo}</span>
                            </div>
                            <div className="text-[11px] text-zinc-400 flex flex-wrap items-center gap-2">
                              <span className="text-amber-400/90 font-medium">
                                {tx.roomName || 'Phòng chiếu chuẩn'}
                              </span>
                              {tx.movieTitle && (
                                <>
                                  <span className="text-zinc-600">•</span>
                                  <span className="text-zinc-300 line-clamp-1 max-w-[200px]">
                                    {tx.movieTitle}
                                  </span>
                                </>
                              )}
                              {tx.seats && tx.seats.length > 0 && (
                                <>
                                  <span className="text-zinc-600">•</span>
                                  <span className="text-emerald-400/90 font-mono text-[10px]">
                                    Ghế: {Array.isArray(tx.seats) ? tx.seats.join(', ') : tx.seats}
                                  </span>
                                </>
                              )}
                            </div>
                          </div>
                        </td>

                        {/* 6. Tổng tiền */}
                        <td className="py-4 px-4 text-right whitespace-nowrap">
                          <div className={cn(
                            'font-headline font-bold text-sm sm:text-base',
                            tx.status === 'Thanh toán không thành công'
                              ? 'text-zinc-500 line-through'
                              : tx.status === 'Đã hoàn tiền'
                              ? 'text-amber-400'
                              : 'text-emerald-400'
                          )}>
                            {tx.totalAmount.toLocaleString('vi-VN')} ₫
                          </div>
                        </td>

                        {/* 7. Trạng thái */}
                        <td className="py-4 px-4 text-center whitespace-nowrap">
                          {renderStatusBadge(tx.status)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>

            {/* Note & Hint */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 text-xs text-zinc-400 pt-2">
              <div className="flex items-center gap-2">
                <span className="material-symbols-outlined text-[16px] text-amber-400">info</span>
                <span>Nhấn vào bất kỳ hàng nào để xem hoặc in chi tiết hoá đơn điện tử.</span>
              </div>

              <div className="flex items-center gap-2 text-zinc-400">
                <span>Hiển thị {filteredTransactions.length} trên {transactions.length} giao dịch</span>
              </div>
            </div>
          </div>
        )}

        {/* =========================================================================
            MODAL 1: Chi Tiết Hoá Đơn Điện Tử
            ========================================================================= */}
        {selectedInvoice && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fadeIn">
            <div className="relative w-full max-w-lg bg-[#11141e] border border-white/15 rounded-3xl p-6 sm:p-7 shadow-2xl space-y-6">
              {/* Modal Header */}
              <div className="flex items-center justify-between pb-4 border-b border-white/10">
                <div className="flex items-center gap-3">
                  <div className="size-11 rounded-2xl bg-emerald-500/15 border border-emerald-500/30 text-emerald-400 flex items-center justify-center">
                    <span className="material-symbols-outlined text-2xl">receipt_long</span>
                  </div>
                  <div>
                    <h3 className="font-headline text-lg font-bold text-white">
                      Hoá Đơn Điện Tử
                    </h3>
                    <p className="text-[11px] text-zinc-400">Hệ thống rạp chiếu phim Vibe Cinema</p>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => setSelectedInvoice(null)}
                  className="size-8 rounded-xl bg-white/5 hover:bg-white/10 text-zinc-400 hover:text-white flex items-center justify-center transition-colors cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[20px]">close</span>
                </button>
              </div>

              {/* Bill Details Content */}
              <div className="bg-[#171b26]/80 rounded-2xl p-5 border border-white/5 space-y-3.5 text-xs">
                {/* Mã hoá đơn & Ngày giờ */}
                <div className="flex justify-between items-center pb-3 border-b border-white/5">
                  <span className="text-zinc-400">Mã hoá đơn:</span>
                  <div className="flex items-center gap-2">
                    <span className="font-mono font-bold text-sm text-amber-400">
                      {selectedInvoice.invoiceCode}
                    </span>
                    <button
                      type="button"
                      onClick={() => handleCopyCode(selectedInvoice.invoiceCode)}
                      className="text-zinc-400 hover:text-white"
                      title="Sao chép mã"
                    >
                      <span className="material-symbols-outlined text-[15px]">content_copy</span>
                    </button>
                  </div>
                </div>

                <div className="flex justify-between items-center pb-3 border-b border-white/5">
                  <span className="text-zinc-400">Thời gian giao dịch:</span>
                  <span className="text-white font-medium">{selectedInvoice.transactionTime}</span>
                </div>

                <div className="flex justify-between items-center pb-3 border-b border-white/5">
                  <span className="text-zinc-400">Trạng thái hoá đơn:</span>
                  <div>{renderStatusBadge(selectedInvoice.status)}</div>
                </div>

                <div className="flex justify-between items-center pb-3 border-b border-white/5">
                  <span className="text-zinc-400">Phương thức thanh toán:</span>
                  <span className="text-white font-medium">{selectedInvoice.paymentMethod}</span>
                </div>

                <div className="flex justify-between items-start gap-4 pb-3 border-b border-white/5">
                  <span className="text-zinc-400 shrink-0">Thông tin rạp:</span>
                  <div className="text-right">
                    <div className="text-white font-semibold">
                      {selectedInvoice.theaterName || selectedInvoice.theaterInfo}
                    </div>
                    <div className="text-[11px] text-zinc-400">{selectedInvoice.roomName}</div>
                  </div>
                </div>

                {selectedInvoice.movieTitle && (
                  <div className="flex justify-between items-start gap-4 pb-3 border-b border-white/5">
                    <span className="text-zinc-400 shrink-0">Phim & Suất:</span>
                    <div className="text-right">
                      <div className="text-amber-400 font-semibold">
                        {selectedInvoice.movieTitle}
                      </div>
                      <div className="text-[11px] text-zinc-300">
                        Ghế:{' '}
                        {Array.isArray(selectedInvoice.seats)
                          ? selectedInvoice.seats.join(', ')
                          : selectedInvoice.seats || 'Ghế tiêu chuẩn'}
                      </div>
                    </div>
                  </div>
                )}

                {/* Khách hàng */}
                <div className="flex justify-between items-center pb-3 border-b border-white/5">
                  <span className="text-zinc-400">Khách hàng:</span>
                  <span className="text-white font-medium">
                    {user.name} ({user.phone})
                  </span>
                </div>

                {/* Thông báo trạng thái chi tiết */}
                {selectedInvoice.status === 'Thanh toán không thành công' && (
                  <div className="p-3 rounded-xl bg-red-950/40 border border-red-500/30 text-xs text-red-300 flex items-center gap-2">
                    <span className="material-symbols-outlined text-[18px] shrink-0 text-red-400">error</span>
                    <span>Giao dịch không thành công do thẻ bị từ chối hoặc phiên thanh toán hết hạn.</span>
                  </div>
                )}

                {selectedInvoice.status === 'Đã hoàn tiền' && (
                  <div className="p-3 rounded-xl bg-amber-950/40 border border-amber-500/30 text-xs text-amber-300 flex flex-col gap-1.5">
                    <div className="flex items-center gap-2">
                      <span className="material-symbols-outlined text-[18px] shrink-0 text-amber-400">replay</span>
                      <span className="font-semibold">Vé đã được hủy và hoàn tiền 100% về tài khoản / ví thanh toán.</span>
                    </div>
                    {(selectedInvoice.refundReason || selectedInvoice.cancelReason) && (
                      <div className="text-[11px] text-zinc-300 pl-6.5">
                        <span className="text-zinc-400">Lý do hủy:</span> {selectedInvoice.refundReason || selectedInvoice.cancelReason}
                      </div>
                    )}
                  </div>
                )}

                {/* Tổng tiền */}
                <div className="flex justify-between items-baseline pt-2">
                  <span className="text-sm font-bold text-white">Tổng tiền thanh toán:</span>
                  <span className={cn(
                    'font-headline text-xl font-bold',
                    selectedInvoice.status === 'Thanh toán không thành công'
                      ? 'text-zinc-500 line-through'
                      : selectedInvoice.status === 'Đã hoàn tiền'
                      ? 'text-amber-400'
                      : 'text-emerald-400'
                  )}>
                    {selectedInvoice.totalAmount.toLocaleString('vi-VN')} ₫
                  </span>
                </div>
                <div className="text-[10px] text-zinc-400 text-right">
                  Đã bao gồm thuế giá trị gia tăng (VAT 8%)
                </div>
              </div>

              {/* Actions */}
              <div className="flex items-center justify-end gap-3 pt-1">
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="px-4 py-2.5 rounded-xl bg-[#1c202c] hover:bg-[#272e3f] text-zinc-300 hover:text-white border border-white/10 text-xs font-semibold flex items-center gap-2 transition-colors cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[18px]">print</span>
                  <span>In hoá đơn</span>
                </button>
                <button
                  type="button"
                  onClick={() => setSelectedInvoice(null)}
                  className="px-5 py-2.5 rounded-xl bg-[#e50914] hover:bg-[#f40612] text-white text-xs font-bold transition-all shadow-md shadow-red-900/30 cursor-pointer"
                >
                  Đóng
                </button>
              </div>
            </div>
          </div>
        )}

        {/* =========================================================================
            MODAL 2: Đổi Mật Khẩu
            ========================================================================= */}
        {showPasswordModal && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md">
            <div className="relative w-full max-w-md bg-[#11141e] border border-white/10 rounded-3xl p-6 sm:p-7 shadow-2xl space-y-5">
              <div className="flex items-center justify-between pb-3 border-b border-white/5">
                <div className="flex items-center gap-2.5">
                  <div className="size-9 rounded-xl bg-amber-400/15 text-amber-400 flex items-center justify-center">
                    <span className="material-symbols-outlined text-[20px]">lock_reset</span>
                  </div>
                  <div>
                    <h3 className="font-headline text-lg font-bold text-white">Đổi Mật Khẩu</h3>
                    <p className="text-[11px] text-zinc-400">
                      Tạo mật khẩu mới cho tài khoản của bạn
                    </p>
                  </div>
                </div>

                <button
                  type="button"
                  onClick={() => setShowPasswordModal(false)}
                  className="size-8 rounded-lg bg-[#1a1f2c] hover:bg-[#252b3d] text-zinc-400 hover:text-white flex items-center justify-center transition-colors cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[18px]">close</span>
                </button>
              </div>

              {passwordError && (
                <div className="p-3 rounded-xl bg-red-950/50 border border-red-500/30 text-xs text-red-300 flex items-center gap-2">
                  <span className="material-symbols-outlined text-[18px]">error</span>
                  <span>{passwordError}</span>
                </div>
              )}

              <form onSubmit={handleChangePassword} className="space-y-4">
                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Mật khẩu hiện tại
                  </label>
                  <input
                    type="password"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    placeholder="Nhập mật khẩu hiện tại"
                    className="w-full bg-[#121620] border border-zinc-800 focus:border-[#e50914] rounded-xl px-4 py-2.5 text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Mật khẩu mới
                  </label>
                  <input
                    type="password"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder="Tối thiểu 6 ký tự"
                    className="w-full bg-[#121620] border border-zinc-800 focus:border-[#e50914] rounded-xl px-4 py-2.5 text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                  />
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-semibold text-zinc-300 block">
                    Xác nhận mật khẩu mới
                  </label>
                  <input
                    type="password"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder="Nhập lại mật khẩu mới"
                    className="w-full bg-[#121620] border border-zinc-800 focus:border-[#e50914] rounded-xl px-4 py-2.5 text-sm text-white placeholder-zinc-500 focus:outline-none transition-colors"
                  />
                </div>

                <div className="pt-2 flex items-center justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => setShowPasswordModal(false)}
                    className="px-4 py-2.5 rounded-xl bg-[#1c202c] hover:bg-[#252b3d] text-zinc-300 text-xs font-semibold transition-colors cursor-pointer"
                  >
                    Hủy Bỏ
                  </button>
                  <button
                    type="submit"
                    className="px-5 py-2.5 rounded-xl bg-[#e50914] hover:bg-[#f40612] text-white text-xs font-bold transition-all shadow-md shadow-red-900/30 cursor-pointer"
                  >
                    Xác Nhận Đổi Mật Khẩu
                  </button>
                </div>
              </form>
            </div>
          </div>
        )}
      </div>
    </MainLayout>
  );
};

export default AccountProfile;
