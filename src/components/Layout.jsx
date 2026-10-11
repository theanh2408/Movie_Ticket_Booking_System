import { useEffect, useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { USE_MOCK } from '../api';

const NAV = [
  ['/dashboard', 'Tổng quan'],
  ['/movies', 'Phim'],
  ['/rooms', 'Phòng chiếu & ghế'],
  ['/showtimes', 'Lịch chiếu'],
  ['/bookings', 'Đặt vé'],
  ['/payments', 'Thanh toán'],
];

export default function Layout() {
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);
  const { pathname } = useLocation();
  useEffect(() => { setOpen(false); }, [pathname]);

  return (
    <div className="app">
      <a className="skip" href="#main">Bỏ qua điều hướng</a>
      <aside className={`sidebar ${open ? 'open' : ''}`}>
        <div className="brand">
          <span className="brand-mark" aria-hidden="true" />
          <div>
            <strong>Quản trị rạp</strong>
            <small>Hệ thống đặt vé xem phim</small>
          </div>
        </div>
        <nav aria-label="Menu chính">
          {NAV.map(([to, label]) => (
            <NavLink key={to} to={to} className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>{label}</NavLink>
          ))}
        </nav>
        <div className="side-foot">
          {USE_MOCK && <span className="mock-flag">Đang dùng dữ liệu giả</span>}
          <div className="who">
            <span className="avatar" aria-hidden="true">{user.name?.[0] || 'A'}</span>
            <div><strong>{user.name}</strong><small>{user.email}</small></div>
          </div>
          <button className="btn ghost-light" onClick={() => logout()}>Đăng xuất</button>
        </div>
      </aside>
      {open && <div className="scrim" onClick={() => setOpen(false)} />}
      <div className="content">
        <div className="topbar">
          <button className="icon-btn" onClick={() => setOpen(true)} aria-label="Mở menu">☰</button>
          <strong>Quản trị rạp</strong>
        </div>
        <main id="main" tabIndex={-1}><Outlet /></main>
      </div>
    </div>
  );
}
