import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { CineLogo } from '../../components/CineLogo';
import { Button } from '../../components/Button';
import authApi from '../../api/authApi';

const TOKEN_KEY = import.meta.env?.VITE_AUTH_TOKEN_KEY || 'vibe_cinema_access_token';
const USER_KEY = import.meta.env?.VITE_AUTH_USER_KEY || 'vibe_cinema_user';

export const Login = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMsg, setErrorMsg] = useState(location.state?.message || '');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setErrorMsg('');

    if (!email.trim() || !password.trim()) {
      setErrorMsg('Vui lòng điền đầy đủ Email và Mật khẩu.');
      return;
    }

    if (!/\S+@\S+\.\S+/.test(email)) {
      setErrorMsg('Email không đúng định dạng.');
      return;
    }

    setIsLoading(true);

    try {
      const res = await authApi.login({
        email: email.trim(),
        password: password,
      });

      if (res && res.data) {
        const { accessToken, user } = res.data;

        // Lưu JWT token và thông tin người dùng vào localStorage
        if (accessToken) {
          localStorage.setItem(TOKEN_KEY, accessToken);
        }
        if (user) {
          localStorage.setItem(USER_KEY, JSON.stringify(user));
          localStorage.setItem('Vibe Cinema_user', JSON.stringify(user));
        }

        window.dispatchEvent(new Event('authChange'));

        const returnUrl = location.state?.returnUrl || '/';
        const bookingData = location.state?.bookingData;
        navigate(returnUrl, { state: bookingData, replace: true });
      } else {
        setErrorMsg('Không thể đăng nhập. Vui lòng thử lại.');
      }
    } catch (err) {
      console.error('Lỗi đăng nhập:', err);
      setErrorMsg(err?.message || 'Email hoặc mật khẩu không chính xác.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDemoLogin = () => {
    setEmail('a@example.com');
    setPassword('Example123!');
  };

  return (
    <div className="min-h-screen w-full bg-surface-lowest flex items-center justify-center p-4 sm:p-6 lg:p-8 relative overflow-hidden">
      {/* Background Decor */}
      <div className="absolute top-1/4 -left-20 size-80 bg-primary/20 rounded-full blur-[100px] pointer-events-none" />
      <div className="absolute bottom-1/4 -right-20 size-80 bg-secondary/15 rounded-full blur-[100px] pointer-events-none" />

      <div className="w-full max-w-md relative z-10">
        {/* Brand Logo Header */}
        <div className="text-center mb-8 space-y-2">
          <Link to="/" className="inline-block hover:scale-105 transition-transform">
            <CineLogo size="lg" />
          </Link>
          <h1 className="font-headline text-2xl sm:text-3xl font-bold text-on-surface">
            Chào mừng trở lại!
          </h1>
          <p className="text-xs text-outline">
            Đăng nhập để xem lịch sử đặt vé, giữ ghế và nhận ưu đãi độc quyền.
          </p>
        </div>

        {/* Login Form Card */}
        <div className="p-6 sm:p-8 rounded-3xl bg-surface-low border border-white/5 shadow-2xl space-y-6">
          {/* Error Notification */}
          {errorMsg && (
            <div className="p-3.5 rounded-xl bg-primary-dark/30 border border-primary-dark text-xs text-[#ffdad6] flex items-center gap-2">
              <span className="material-symbols-outlined text-[18px]">error</span>
              <span>{errorMsg}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email Field */}
            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
                Email tài khoản
              </label>
              <div className="relative">
                <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-outline text-[18px]">
                  mail
                </span>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@example.com"
                  className="w-full bg-surface text-on-surface placeholder:text-outline text-xs sm:text-sm pl-10 pr-4 py-3 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary focus:bg-surface-high transition-all"
                  autoComplete="email"
                />
              </div>
            </div>

            {/* Password Field */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label className="text-xs font-semibold text-outline uppercase tracking-wider block">
                  Mật khẩu
                </label>
                <Link
                  to="#"
                  onClick={(e) => {
                    e.preventDefault();
                    alert('Tính năng quên mật khẩu đang được phát triển.');
                  }}
                  className="text-xs text-secondary hover:underline"
                >
                  Quên mật khẩu?
                </Link>
              </div>
              <div className="relative">
                <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-outline text-[18px]">
                  lock
                </span>
                <input
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Nhập mật khẩu..."
                  className="w-full bg-surface text-on-surface placeholder:text-outline text-xs sm:text-sm pl-10 pr-10 py-3 rounded-xl border border-white/5 focus:outline-none focus:ring-1 focus:ring-primary focus:bg-surface-high transition-all"
                  autoComplete="current-password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 top-1/2 -translate-y-1/2 text-outline hover:text-on-surface cursor-pointer"
                >
                  <span className="material-symbols-outlined text-[18px]">
                    {showPassword ? 'visibility_off' : 'visibility'}
                  </span>
                </button>
              </div>
            </div>

            {/* Submit Button */}
            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full justify-center shadow-glow-primary"
              disabled={isLoading}
            >
              {isLoading ? 'Đang xác thực...' : 'Đăng nhập ngay'}
            </Button>
          </form>

          {/* Demo account autofill */}
          <div className="pt-2">
            <button
              type="button"
              onClick={handleDemoLogin}
              className="w-full py-2.5 rounded-xl border border-dashed border-white/10 hover:border-secondary/40 text-xs text-outline hover:text-secondary transition-colors cursor-pointer text-center"
            >
              Điền tài khoản mẫu (Khách hàng)
            </button>
          </div>

          {/* Register Link */}
          <div className="pt-4 border-t border-white/5 text-center text-xs text-outline">
            Chưa có tài khoản Vibe Cinema?{' '}
            <Link to="/register" className="text-secondary font-semibold hover:underline">
              Đăng ký thành viên mới
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
