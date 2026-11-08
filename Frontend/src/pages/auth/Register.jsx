import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { CineLogo } from '../../components/CineLogo';
import { Button } from '../../components/Button';
import authApi from '../../api/authApi';

export const Register = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [dob, setDob] = useState('');
  const [gender, setGender] = useState('Nam');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [agreeTerms, setAgreeTerms] = useState(true);

  const [errorMsg, setErrorMsg] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');
    setSuccessMsg('');

    if (!fullName.trim()) {
      setErrorMsg('Vui lòng nhập Họ và tên.');
      return;
    }
    if (!phone.trim() || phone.trim().length < 9) {
      setErrorMsg('Vui lòng nhập Số điện thoại hợp lệ (từ 10 chữ số).');
      return;
    }
    if (!email.trim() || !/\S+@\S+\.\S+/.test(email)) {
      setErrorMsg('Vui lòng nhập Email hợp lệ.');
      return;
    }
    if (!dob) {
      setErrorMsg('Vui lòng chọn Ngày sinh.');
      return;
    }
    if (!password || password.length < 6) {
      setErrorMsg('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }
    if (password !== confirmPassword) {
      setErrorMsg('Mật khẩu xác nhận không khớp.');
      return;
    }
    if (!agreeTerms) {
      setErrorMsg('Bạn cần đồng ý với Điều khoản và Quy định của Vibe Cinema.');
      return;
    }

    setIsLoading(true);

    try {
      await authApi.register({
        fullName: fullName.trim(),
        phone: phone.trim(),
        email: email.trim(),
        dob: dob,
        gender: gender,
        password: password,
      });

      setSuccessMsg('Đăng ký tài khoản thành công! Đang chuyển hướng...');
      setTimeout(() => {
        navigate('/login', {
          state: {
            returnUrl: location.state?.returnUrl || '/',
            bookingData: location.state?.bookingData,
            message: 'Đăng ký thành công! Vui lòng đăng nhập với tài khoản vừa tạo.',
          },
        });
      }, 1000);
    } catch (err) {
      console.error('Lỗi đăng ký:', err);
      setErrorMsg(err?.message || 'Không thể đăng ký tài khoản. Vui lòng kiểm tra lại.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-surface text-on-surface">
      <div className="w-full max-w-lg bg-surface-low border border-white/10 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6">
        <div className="text-center space-y-2">
          <Link to="/" className="inline-block">
            <CineLogo size="md" />
          </Link>
          <h1 className="font-headline text-2xl font-bold text-on-surface">
            Đăng Ký Thành Viên Vibe Cinema
          </h1>
          <p className="text-xs text-outline">
            Tạo tài khoản để nhận ưu đãi thành viên và quản lý lịch sử xem phim
          </p>
        </div>

        {errorMsg && (
          <div className="p-3.5 rounded-xl bg-primary-dark/30 border border-primary-dark text-xs text-[#ffdad6] flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">error</span>
            <span>{errorMsg}</span>
          </div>
        )}

        {successMsg && (
          <div className="p-3.5 rounded-xl bg-emerald-950/40 border border-emerald-800 text-xs text-emerald-400 flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">check_circle</span>
            <span>{successMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline uppercase tracking-wider">
              Họ và tên
            </label>
            <input
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="Nguyễn Văn A"
              className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline uppercase tracking-wider">
                Số điện thoại
              </label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="0912345678"
                className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline uppercase tracking-wider">
                Ngày sinh
              </label>
              <input
                type="date"
                value={dob}
                onChange={(e) => setDob(e.target.value)}
                className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline uppercase tracking-wider">
              Giới tính
            </label>
            <div className="flex gap-4">
              {['Nam', 'Nữ', 'Khác'].map((g) => (
                <label key={g} className="flex items-center gap-2 text-xs cursor-pointer">
                  <input
                    type="radio"
                    name="gender"
                    value={g}
                    checked={gender === g}
                    onChange={(e) => setGender(e.target.value)}
                    className="accent-primary"
                  />
                  <span>{g}</span>
                </label>
              ))}
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline uppercase tracking-wider">
              Email đăng nhập
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="name@example.com"
              className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline uppercase tracking-wider">
              Mật khẩu (Tối thiểu 6 ký tự)
            </label>
            <div className="relative">
              <input
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 pr-10 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
              />
              <button
                type="button"
                onClick={() => setShowPassword(!showPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-outline hover:text-on-surface cursor-pointer"
              >
                <span className="material-symbols-outlined text-[18px]">
                  {showPassword ? 'visibility_off' : 'visibility'}
                </span>
              </button>
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline uppercase tracking-wider">
              Xác nhận mật khẩu
            </label>
            <input
              type={showPassword ? 'text' : 'password'}
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              className="w-full bg-surface-container text-on-surface text-xs sm:text-sm px-4 py-2.5 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary"
            />
          </div>

          <label className="flex items-start gap-2 text-xs text-outline cursor-pointer pt-2">
            <input
              type="checkbox"
              checked={agreeTerms}
              onChange={(e) => setAgreeTerms(e.target.checked)}
              className="mt-0.5 accent-primary"
            />
            <span>
              Tôi đã đọc và đồng ý với{' '}
              <a href="#" className="text-secondary hover:underline">
                Điều khoản &amp; Điều kiện
              </a>{' '}
              của Vibe Cinema.
            </span>
          </label>

          <Button
            type="submit"
            variant="primary"
            size="lg"
            className="w-full justify-center shadow-glow-primary mt-2"
            disabled={isLoading}
          >
            {isLoading ? 'Đang khởi tạo tài khoản...' : 'Tạo Tài Khoản Thành Viên'}
          </Button>
        </form>

        <div className="text-center text-xs text-outline pt-2 border-t border-white/5">
          Đã có tài khoản?{' '}
          <Link to="/login" className="text-secondary font-semibold hover:underline">
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    </div>
  );
};

export default Register;
