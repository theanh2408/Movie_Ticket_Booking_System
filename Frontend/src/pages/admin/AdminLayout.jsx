import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { useEffect } from 'react';

export default function AdminLayout() {
  const { user, isAdmin, loading } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  useEffect(() => {
    if (!loading && (!user || !isAdmin)) {
      alert('Bạn không có quyền truy cập trang quản trị!');
      navigate('/');
    }
  }, [user, isAdmin, loading, navigate]);

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-950 text-slate-400">
        Đang kiểm tra quyền truy cập...
      </div>
    );
  }

  if (!isAdmin) return null;

  const navItems = [
    { path: '/admin/movies', label: 'Quản Lý Phim', icon: '🎬' },
    { path: '/admin/showtimes', label: 'Quản Lý Suất Chiếu', icon: '🕒' },
    { path: '/admin/bookings', label: 'Soát Vé & Quét QR', icon: '🔍' },
    { path: '/admin/rooms', label: 'Phòng Chiếu & Rạp', icon: '🏢' },
  ];

  return (
    <div className="min-h-screen bg-slate-950 flex flex-col md:flex-row">
      {/* Sidebar */}
      <aside className="w-full md:w-64 bg-slate-900 border-r border-slate-800 p-5 shrink-0 flex flex-col justify-between">
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <Link to="/" className="flex items-center gap-2 font-black text-xl text-red-500">
              <span>🎬</span>
              <span>ADMIN<span className="text-white">PANEL</span></span>
            </Link>
          </div>

          <div className="bg-slate-950/80 p-3 rounded-xl border border-slate-800 flex items-center gap-3">
            <div className="w-9 h-9 rounded-full bg-red-600 text-white font-bold flex items-center justify-center text-sm">
              AD
            </div>
            <div className="truncate">
              <div className="text-xs font-bold text-white truncate">{user?.fullName || 'Quản Trị Viên'}</div>
              <div className="text-[10px] text-amber-400 font-semibold uppercase tracking-wider">Hệ Thống Rạp</div>
            </div>
          </div>

          <nav className="space-y-1.5">
            {navItems.map((item) => {
              const active = location.pathname.startsWith(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={`flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-medium transition ${
                    active
                      ? 'bg-red-600 text-white shadow-md shadow-red-600/30'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800/80'
                  }`}
                >
                  <span className="text-base">{item.icon}</span>
                  <span>{item.label}</span>
                </Link>
              );
            })}
          </nav>
        </div>

        <div className="pt-6 border-t border-slate-800/80 mt-6">
          <Link
            to="/"
            className="flex items-center gap-2 text-xs text-slate-400 hover:text-white transition py-2 px-3 rounded-lg hover:bg-slate-800"
          >
            <span>←</span> Quay Lại Trang Bán Vé
          </Link>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 p-6 md:p-10 overflow-x-hidden">
        <Outlet />
      </main>
    </div>
  );
}
