import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function Navbar() {
  const { user, logout, isAdmin } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="sticky top-0 z-50 bg-slate-900/90 backdrop-blur border-b border-slate-800">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
        <div className="flex items-center gap-8">
          <Link to="/" className="flex items-center gap-2 text-red-500 font-extrabold text-2xl tracking-wider hover:opacity-90">
            <span>🎬</span>
            <span>CINEMA<span className="text-white">PLUS</span></span>
          </Link>

          <nav className="hidden md:flex items-center gap-6 text-sm font-medium text-slate-300">
            <Link to="/" className="hover:text-red-400 transition-colors">Phim Đang Chiếu</Link>
            <Link to="/#upcoming" className="hover:text-red-400 transition-colors">Phim Sắp Chiếu</Link>
            {isAdmin && (
              <Link to="/admin/movies" className="text-amber-400 font-semibold hover:text-amber-300 transition-colors flex items-center gap-1">
                <span>⚙️</span> Trang Quản Trị
              </Link>
            )}
          </nav>
        </div>

        <div className="flex items-center gap-3">
          {user ? (
            <div className="flex items-center gap-4">
              <Link to="/profile" className="flex items-center gap-2 text-sm text-slate-200 hover:text-white bg-slate-800/80 hover:bg-slate-800 px-3 py-1.5 rounded-full border border-slate-700 transition">
                <span className="w-7 h-7 rounded-full bg-red-600 text-white flex items-center justify-center font-bold text-xs uppercase">
                  {user.fullName ? user.fullName[0] : 'U'}
                </span>
                <span className="font-medium max-w-[120px] truncate">{user.fullName}</span>
                {isAdmin && (
                  <span className="bg-amber-500/20 text-amber-300 text-xs px-2 py-0.5 rounded-full border border-amber-500/30">Admin</span>
                )}
              </Link>
              <button
                onClick={handleLogout}
                className="text-xs text-slate-400 hover:text-rose-400 border border-slate-700 hover:border-rose-500/50 px-3 py-1.5 rounded-md transition"
              >
                Đăng xuất
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link
                to="/login"
                className="text-sm font-medium text-slate-200 hover:text-white px-3 py-1.5 rounded-md hover:bg-slate-800 transition"
              >
                Đăng nhập
              </Link>
              <Link
                to="/register"
                className="text-sm font-medium bg-red-600 hover:bg-red-500 text-white px-4 py-1.5 rounded-md shadow-md shadow-red-600/30 transition"
              >
                Đăng ký
              </Link>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
