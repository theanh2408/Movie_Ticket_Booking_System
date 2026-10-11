import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { USE_MOCK } from '../api';
import { Field } from '../components/ui';

export default function LoginPage() {
  const { user, login, notice, clearNotice } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState(USE_MOCK ? 'admin@cinema.vn' : '');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [errs, setErrs] = useState({});

  if (user) return <Navigate to={location.state?.from || '/dashboard'} replace />;

  const submit = async (e) => {
    e.preventDefault();
    const f = {};
    if (!email.trim()) f.email = 'Nhập email.';
    if (!password) f.password = 'Nhập mật khẩu.';
    setErrs(f); setError('');
    if (Object.keys(f).length) return;
    setBusy(true); clearNotice();
    try {
      await login(email, password);
      navigate(location.state?.from || '/dashboard', { replace: true });
    } catch (err) {
      setError(err.message || 'Không đăng nhập được. Vui lòng thử lại.');
    } finally { setBusy(false); }
  };

  return (
    <div className="login">
      <div className="login-art" aria-hidden="true">
        <div className="curtain" />
        <div className="login-art-text">
          <h2>Mọi suất chiếu, một nơi quản lý.</h2>
          <p>Phim, phòng chiếu, lịch chiếu, đặt vé và doanh thu của rạp.</p>
        </div>
      </div>
      <form className="login-card" onSubmit={submit} noValidate>
        <h1>Đăng nhập quản trị</h1>
        <p className="muted">Dành cho nhân viên quản lý của rạp.</p>
        {(notice || error) && <div className={`errorbox ${notice && !error ? 'info' : ''}`} role="alert">{error || notice}</div>}
        <Field label="Email" error={errs.email}>
          <input className="input" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus />
        </Field>
        <Field label="Mật khẩu" error={errs.password}>
          <input className="input" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </Field>
        <button className="btn primary block" disabled={busy}>{busy ? 'Đang đăng nhập…' : 'Đăng nhập'}</button>
        {USE_MOCK && <p className="hint demo">Dữ liệu giả: admin@cinema.vn / admin123</p>}
      </form>
    </div>
  );
}
