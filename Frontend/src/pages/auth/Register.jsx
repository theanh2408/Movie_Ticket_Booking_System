import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export default function Register() {
  const [formData, setFormData] = useState({
    fullName: '',
    phone: '',
    email: '',
    password: '',
    confirmPassword: '',
    dob: '',
    gender: 'Nam',
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const { register } = useAuth();
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!formData.fullName || !formData.phone || !formData.email || !formData.password) {
      setError('Vui lòng điền đầy đủ các thông tin bắt buộc.');
      return;
    }

    if (formData.password.length < 8) {
      setError('Mật khẩu phải có tối thiểu 8 ký tự.');
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.');
      return;
    }

    try {
      setLoading(true);
      setError('');
      const res = await register({
        fullName: formData.fullName,
        phone: formData.phone,
        email: formData.email,
        password: formData.password,
        dob: formData.dob ? new Date(formData.dob).toISOString() : null,
        gender: formData.gender,
      });

      if (res.success) {
        setSuccess('Đăng ký tài khoản thành công! Đang chuyển hướng sang đăng nhập...');
        setTimeout(() => navigate('/login'), 1500);
      }
    } catch (err) {
      setError(err.response?.data?.message || err.message || 'Đăng ký thất bại.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-[85vh] flex items-center justify-center px-4 py-12">
      <div className="max-w-md w-full bg-slate-900 border border-slate-800 rounded-2xl p-8 shadow-2xl space-y-6">
        <div className="text-center">
          <span className="text-4xl">🍿</span>
          <h2 className="text-2xl font-black text-white mt-2">Đăng Ký Tài Khoản</h2>
          <p className="text-slate-400 text-xs mt-1">Trở thành thành viên để nhận ngay ưu đãi khi đặt vé</p>
        </div>

        {error && (
          <div className="bg-red-500/20 border border-red-500/40 text-red-300 px-4 py-2.5 rounded-lg text-xs">
            ⚠️ {error}
          </div>
        )}

        {success && (
          <div className="bg-emerald-500/20 border border-emerald-500/40 text-emerald-300 px-4 py-2.5 rounded-lg text-xs">
            ✓ {success}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-3.5">
          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1">Họ và tên *</label>
            <input
              type="text"
              name="fullName"
              required
              placeholder="Nguyễn Văn A"
              value={formData.fullName}
              onChange={handleChange}
              className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">Số điện thoại *</label>
              <input
                type="tel"
                name="phone"
                required
                placeholder="0912345678"
                value={formData.phone}
                onChange={handleChange}
                className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">Giới tính</label>
              <select
                name="gender"
                value={formData.gender}
                onChange={handleChange}
                className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-red-500"
              >
                <option value="Nam">Nam</option>
                <option value="Nữ">Nữ</option>
                <option value="Khác">Khác</option>
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-300 mb-1">Email *</label>
            <input
              type="email"
              name="email"
              required
              placeholder="name@example.com"
              value={formData.email}
              onChange={handleChange}
              className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">Mật khẩu *</label>
              <input
                type="password"
                name="password"
                required
                placeholder="Tối thiểu 8 ký tự"
                value={formData.password}
                onChange={handleChange}
                className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-slate-300 mb-1">Nhập lại MK *</label>
              <input
                type="password"
                name="confirmPassword"
                required
                placeholder="Nhập lại mật khẩu"
                value={formData.confirmPassword}
                onChange={handleChange}
                className="w-full bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-sm text-white placeholder-slate-500 focus:outline-none focus:border-red-500"
              />
            </div>
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full bg-red-600 hover:bg-red-500 disabled:bg-slate-800 text-white font-bold py-2.5 rounded-lg transition shadow-lg shadow-red-600/30 text-sm mt-2"
          >
            {loading ? 'Đang đăng ký...' : 'Tạo Tài Khoản'}
          </button>
        </form>

        <div className="text-center text-xs text-slate-400">
          Đã có tài khoản?{' '}
          <Link to="/login" className="text-red-400 hover:text-red-300 font-semibold underline">
            Đăng nhập
          </Link>
        </div>
      </div>
    </div>
  );
}
