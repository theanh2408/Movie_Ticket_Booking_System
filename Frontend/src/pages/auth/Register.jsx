import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { CineLogo } from '../../components/CineLogo';
import { Button } from '../../components/Button';

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

  const handleSubmit = (e) => {
    e.preventDefault();
    setErrorMsg('');

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

    setTimeout(() => {
      setIsLoading(false);
      const newUser = {
        name: fullName.trim(),
        email: email.trim(),
        phone: phone.trim(),
        dob: dob,
        gender: gender,
      };

      try {
        localStorage.setItem('Vibe Cinema_user', JSON.stringify(newUser));
        window.dispatchEvent(new Event('authChange'));
      } catch {
        // no-op
      }

      const returnUrl = location.state?.returnUrl || '/';
      const bookingData = location.state?.bookingData;

      setSuccessMsg('Đăng ký tài khoản thành công!');
      setTimeout(() => {
        navigate(returnUrl, { state: bookingData });
      }, 700);
    }, 700);
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
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Họ và tên *</label>
              <input
                type="text"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nguyễn Văn An"
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Số điện thoại *</label>
              <input
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="0912 345 678"
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline">Email đăng nhập *</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="vidu@Vibe Cinema.vn"
              className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Ngày sinh *</label>
              <input
                type="date"
                value={dob}
                onChange={(e) => setDob(e.target.value)}
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Giới tính</label>
              <select
                value={gender}
                onChange={(e) => setGender(e.target.value)}
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              >
                <option value="Nam">Nam</option>
                <option value="Nữ">Nữ</option>
                <option value="Khác">Khác</option>
              </select>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Mật khẩu *</label>
              <input
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ít nhất 6 ký tự"
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-semibold text-outline">Xác nhận mật khẩu *</label>
              <input
                type={showPassword ? 'text' : 'password'}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Nhập lại mật khẩu"
                className="w-full bg-surface border border-white/10 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>
          </div>

          <div className="flex items-center justify-between text-xs text-outline">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={agreeTerms}
                onChange={(e) => setAgreeTerms(e.target.checked)}
                className="rounded accent-primary"
              />
              <span>Tôi đồng ý với Điều khoản Vibe Cinema</span>
            </label>
            <button
              type="button"
              onClick={() => setShowPassword(!showPassword)}
              className="text-outline hover:text-on-surface cursor-pointer"
            >
              {showPassword ? 'Ẩn' : 'Hiện'} mật khẩu
            </button>
          </div>

          <div className="pt-2">
            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              disabled={isLoading}
              icon={isLoading ? 'hourglass_top' : 'person_add'}
            >
              {isLoading ? 'Đang Đăng Ký...' : 'Hoàn Tất Đăng Ký'}
            </Button>
          </div>
        </form>

        <div className="text-center text-xs text-outline space-y-2">
          <div>
            Đã có tài khoản?{' '}
            <Link to="/login" state={location.state} className="text-primary hover:underline font-semibold">
              Đăng nhập ngay
            </Link>
          </div>
          <div>
            <Link to="/" className="text-outline hover:text-on-surface">
              ← Quay về trang chủ
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};
export default Register;
