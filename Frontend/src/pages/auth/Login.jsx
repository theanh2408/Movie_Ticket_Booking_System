import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { CineLogo } from '../../components/CineLogo';
import { Button } from '../../components/Button';

export const Login = () => {
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = (e) => {
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

    setTimeout(() => {
      setIsLoading(false);
      const user = {
        name: email.includes('an') ? 'Nguyễn Văn An' : email.split('@')[0],
        email: email,
        phone: '0912 345 678',
        dob: '1995-08-15',
        gender: 'Nam',
      };

      try {
        localStorage.setItem('Vibe Cinema_user', JSON.stringify(user));
        window.dispatchEvent(new Event('authChange'));
      } catch {
        // no-op
      }

      const returnUrl = location.state?.returnUrl || '/';
      const bookingData = location.state?.bookingData;
      navigate(returnUrl, { state: bookingData });
    }, 600);
  };

  const handleDemoLogin = () => {
    setEmail('nguyenvanan@Vibe Cinema.vn');
    setPassword('Vibe Cinema123');
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-surface text-on-surface">
      <div className="w-full max-w-md bg-surface-low border border-white/10 rounded-3xl p-6 sm:p-8 shadow-2xl space-y-6 relative">
        <div className="text-center space-y-2">
          <Link to="/" className="inline-block">
            <CineLogo size="md" />
          </Link>
          <h1 className="font-headline text-2xl font-bold text-on-surface">
            Đăng Nhập Tài Khoản
          </h1>
          <p className="text-xs text-outline">
            Đăng nhập để đặt vé nhanh chóng và quản lý vé điện tử của bạn
          </p>
        </div>

        {location.state?.message && !errorMsg && (
          <div className="p-3.5 rounded-xl bg-secondary/15 border border-secondary/30 text-xs text-secondary flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">info</span>
            <span>{location.state.message}</span>
          </div>
        )}

        {errorMsg && (
          <div className="p-3.5 rounded-xl bg-primary-dark/30 border border-primary-dark text-xs text-[#ffdad6] flex items-center gap-2">
            <span className="material-symbols-outlined text-[18px]">error</span>
            <span>{errorMsg}</span>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline">Email đăng nhập:</label>
            <div className="relative">
              <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-outline text-[18px]">
                mail
              </span>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="vidu@Vibe Cinema.vn"
                className="w-full bg-surface border border-white/10 rounded-xl pl-10 pr-4 py-2.5 text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
              />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-xs font-semibold text-outline">Mật khẩu:</label>
            <div className="relative">
              <span className="material-symbols-outlined absolute left-3.5 top-1/2 -translate-y-1/2 text-outline text-[18px]">
                lock
              </span>
              <input
                type={showPassword ? 'text' : 'password'}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Nhập mật khẩu"
                className="w-full bg-surface border border-white/10 rounded-xl pl-10 pr-10 py-2.5 text-sm text-on-surface focus:outline-none focus:border-primary transition-colors"
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

          <div className="pt-2">
            <Button
              type="submit"
              variant="primary"
              size="lg"
              className="w-full"
              disabled={isLoading}
              icon={isLoading ? 'hourglass_top' : 'login'}
            >
              {isLoading ? 'Đang Đăng Nhập...' : 'Đăng Nhập'}
            </Button>
          </div>
        </form>

        {/* Demo Fast Login */}
        <div className="p-3 rounded-xl bg-surface border border-white/5 flex items-center justify-between text-xs">
          <span className="text-outline">Tài khoản mẫu:</span>
          <button
            type="button"
            onClick={handleDemoLogin}
            className="text-secondary hover:underline font-semibold cursor-pointer"
          >
            Điền thông tin mẫu
          </button>
        </div>

        <div className="text-center text-xs text-outline space-y-2">
          <div>
            Chưa có tài khoản?{' '}
            <Link to="/register" state={location.state} className="text-primary hover:underline font-semibold">
              Đăng ký ngay
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
export default Login;
