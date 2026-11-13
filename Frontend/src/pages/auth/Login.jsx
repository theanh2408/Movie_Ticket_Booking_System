import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from || '/';

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!email || !password) {
      setError('Vui lòng nhập đầy đủ Email và Mật khẩu.');
      return;
    }

    try {
      setLoading(true);
      setError('');
      const user = await login(email, password);
      if (user.role === 'ADMIN') {
        navigate('/admin/movies');
      } else {
        navigate(from);
      }
    } catch (err) {
      setError(err.message || 'Email hoặc mật khẩu không chính xác.');
    } finally {
      setLoading(false);
    }
  };

  const handleQuickLogin = (demoEmail, demoPassword) => {
    setEmail(demoEmail);
    setPassword(demoPassword);
  };

  return (
    <div className="min-h-[80vh] flex items-center justify-center px-4 py-12">
      <div className="max-w-md w-full bg-slate-900 border border-slate-800 rounded-2xl p-8 shadow-2xl space-y-6">
        <div className="text-center">
          <span className="text-4xl">🎬</span>
          <h2 className="text-2xl font-black text-white mt-2">Đăng Nhập Tài Khoản</h2>
          <p className="text-slate-400 text-xs mt-1">Đăng nhập để đặt vé và xem lại lịch sử đặt chỗ</p>
        </div>

        {error && (
          <div className="bg-red-500/20 border border-red-500/40 text-red-300 px-4 py-2.5 rounded-lg text-xs">
            ⚠️ {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1">Email</label>
            <input
              type="email"
              required
              placeholder="name@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500 transition"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1">Mật khẩu</label>
            <input
              type="password"
              required
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3.5 py-2.5 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500 transition"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-red-600 hover:bg-red-500 disabled:bg-slate-800 text-white font-bold py-2.5 rounded-lg transition shadow-lg shadow-red-600/30 text-sm"
          >
            {loading ? 'Đang xác thực...' : 'Đăng Nhập'}
          </button>
        </form>

        {/* Nút bấm nhanh để Demo môn học */}
        <div className="pt-4 border-t border-slate-800">
          <p className="text-[11px] text-slate-400 text-center mb-2">⚡ Tài khoản Demo có sẵn (Bấm để điền):</p>
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => handleQuickLogin('admin@cinema.com', 'Admin@123')}
              className="text-xs bg-slate-800 hover:bg-slate-700 text-amber-300 font-medium py-1.5 px-2 rounded border border-slate-700 text-center transition"
            >
              👑 Quản Trị Viên (Admin)
            </button>
            <button
              type="button"
              onClick={() => handleQuickLogin('customer@gmail.com', 'Customer@123')}
              className="text-xs bg-slate-800 hover:bg-slate-700 text-blue-300 font-medium py-1.5 px-2 rounded border border-slate-700 text-center transition"
            >
              👤 Khách Hàng (Customer)
            </button>
          </div>
        </div>

        <div className="text-center text-xs text-slate-400">
          Chưa có tài khoản?{' '}
          <Link to="/register" className="text-red-400 hover:text-red-300 font-semibold underline">
            Đăng ký ngay
          </Link>
        </div>
      </div>
    </div>
  );
}
