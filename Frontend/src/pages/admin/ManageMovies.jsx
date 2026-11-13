import { useState, useEffect } from 'react';
import axiosClient from '../../api/axiosClient';

export default function ManageMovies() {
  const [movies, setMovies] = useState([]);
  const [genres, setGenres] = useState([]);
  const [loading, setLoading] = useState(true);
  const [showModal, setShowModal] = useState(false);
  const [editingMovie, setEditingMovie] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const [alertMsg, setAlertMsg] = useState('');

  const [formData, setFormData] = useState({
    title: '',
    description: '',
    duration: 120,
    posterUrl: '',
    trailerUrl: '',
    ageRating: 'P',
    releaseDate: new Date().toISOString().split('T')[0],
    status: 'NOW_SHOWING',
    genreIds: [1],
  });

  useEffect(() => {
    fetchMoviesAndGenres();
  }, []);

  const fetchMoviesAndGenres = async () => {
    try {
      setLoading(true);
      const [movRes, genRes] = await Promise.all([
        axiosClient.get('/admin/movies'),
        axiosClient.get('/admin/rooms/genres'),
      ]);

      if (movRes.success && movRes.data) {
        setMovies(movRes.data);
      }
      if (genRes.success && genRes.data) {
        setGenres(genRes.data);
      }
    } catch (err) {
      console.error('Lỗi tải dữ liệu phim:', err);
    } finally {
      setLoading(false);
    }
  };

  const openCreateModal = () => {
    setEditingMovie(null);
    setFormData({
      title: '',
      description: '',
      duration: 120,
      posterUrl: '',
      trailerUrl: '',
      ageRating: 'P',
      releaseDate: new Date().toISOString().split('T')[0],
      status: 'NOW_SHOWING',
      genreIds: [genres[0]?.genreId || 1],
    });
    setShowModal(true);
  };

  const openEditModal = (movie) => {
    setEditingMovie(movie);
    setFormData({
      title: movie.title || '',
      description: movie.description || '',
      duration: movie.duration || 120,
      posterUrl: movie.posterUrl || '',
      trailerUrl: movie.trailerUrl || '',
      ageRating: movie.ageRating || 'P',
      releaseDate: movie.releaseDate || new Date().toISOString().split('T')[0],
      status: movie.status || 'NOW_SHOWING',
      genreIds: movie.genres?.map((g) => g.genreId) || [1],
    });
    setShowModal(true);
  };

  const handleToggleGenre = (genreId) => {
    if (formData.genreIds.includes(genreId)) {
      if (formData.genreIds.length === 1) return; // Giữ ít nhất 1 thể loại
      setFormData({ ...formData, genreIds: formData.genreIds.filter((id) => id !== genreId) });
    } else {
      setFormData({ ...formData, genreIds: [...formData.genreIds, genreId] });
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      setSubmitting(true);
      setAlertMsg('');

      if (editingMovie) {
        // Cập nhật phim
        const res = await axiosClient.put(`/admin/movies/${editingMovie.movieId}`, formData);
        if (res.success) {
          setAlertMsg('Cập nhật phim thành công!');
          setShowModal(false);
          fetchMoviesAndGenres();
        }
      } else {
        // Tạo phim mới
        const res = await axiosClient.post('/admin/movies', formData);
        if (res.success) {
          setAlertMsg('Thêm phim mới thành công!');
          setShowModal(false);
          fetchMoviesAndGenres();
        }
      }
    } catch (err) {
      alert(err.response?.data?.message || err.message || 'Lỗi khi lưu phim.');
    } finally {
      setSubmitting(false);
    }
  };

  const handleDeleteMovie = async (movieId) => {
    if (!confirm('Bạn có chắc muốn chuyển trạng thái phim này thành KẾT THÚC (ENDED)?')) return;
    try {
      const res = await axiosClient.delete(`/admin/movies/${movieId}`);
      if (res.success) {
        setAlertMsg('Đã kết thúc chiếu phim thành công.');
        fetchMoviesAndGenres();
      }
    } catch (err) {
      alert(err.response?.data?.message || 'Lỗi khi cập nhật trạng thái phim.');
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-black text-white">Quản Lý Phim</h1>
          <p className="text-slate-400 text-xs mt-1">Danh sách phim trong hệ thống rạp và trạng thái phát hành</p>
        </div>

        <button
          onClick={openCreateModal}
          className="bg-red-600 hover:bg-red-500 text-white font-bold px-4 py-2.5 rounded-xl shadow-lg shadow-red-600/30 transition text-sm flex items-center gap-2 self-start sm:self-auto"
        >
          <span>➕</span> Thêm Phim Mới
        </button>
      </div>

      {alertMsg && (
        <div className="bg-emerald-500/20 border border-emerald-500/40 text-emerald-300 px-4 py-2.5 rounded-lg text-xs">
          ✓ {alertMsg}
        </div>
      )}

      {/* Movies Table */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-slate-300">
            <thead className="bg-slate-950 text-xs uppercase text-slate-400 border-b border-slate-800 font-semibold tracking-wider">
              <tr>
                <th className="px-5 py-4">Phim</th>
                <th className="px-4 py-4">Thời lượng</th>
                <th className="px-4 py-4">Độ tuổi</th>
                <th className="px-4 py-4">Thể loại</th>
                <th className="px-4 py-4">Trạng thái</th>
                <th className="px-5 py-4 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/80">
              {loading ? (
                <tr>
                  <td colSpan="6" className="text-center py-12 text-slate-500">
                    Đang tải dữ liệu phim...
                  </td>
                </tr>
              ) : movies.length === 0 ? (
                <tr>
                  <td colSpan="6" className="text-center py-12 text-slate-500">
                    Chưa có phim nào trong hệ thống.
                  </td>
                </tr>
              ) : (
                movies.map((m) => (
                  <tr key={m.movieId} className="hover:bg-slate-800/40 transition">
                    <td className="px-5 py-3.5 flex items-center gap-3">
                      <img
                        src={m.posterUrl || 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=100'}
                        alt={m.title}
                        className="w-10 h-14 object-cover rounded bg-slate-950 shrink-0 border border-slate-700"
                      />
                      <div>
                        <div className="font-bold text-white text-sm">{m.title}</div>
                        <div className="text-[11px] text-slate-500">Khởi chiếu: {m.releaseDate || 'N/A'}</div>
                      </div>
                    </td>
                    <td className="px-4 py-3.5 text-xs">{m.duration} phút</td>
                    <td className="px-4 py-3.5">
                      <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-slate-800 border border-slate-700">
                        {m.ageRating || 'P'}
                      </span>
                    </td>
                    <td className="px-4 py-3.5 text-xs text-slate-400">
                      {m.genres?.map((g) => g.genreName).join(', ') || 'N/A'}
                    </td>
                    <td className="px-4 py-3.5">
                      <span
                        className={`px-2.5 py-0.5 rounded-full text-xs font-semibold ${
                          m.status === 'NOW_SHOWING'
                            ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                            : m.status === 'UPCOMING'
                            ? 'bg-blue-500/20 text-blue-400 border border-blue-500/30'
                            : 'bg-slate-700 text-slate-400'
                        }`}
                      >
                        {m.status === 'NOW_SHOWING' ? 'Đang Chiếu' : m.status === 'UPCOMING' ? 'Sắp Chiếu' : 'Kết Thúc'}
                      </span>
                    </td>
                    <td className="px-5 py-3.5 text-right space-x-2">
                      <button
                        onClick={() => openEditModal(m)}
                        className="text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 px-2.5 py-1 rounded transition border border-slate-700"
                      >
                        Sửa
                      </button>
                      {m.status !== 'ENDED' && (
                        <button
                          onClick={() => handleDeleteMovie(m.movieId)}
                          className="text-xs bg-rose-950/60 hover:bg-rose-900 text-rose-300 px-2.5 py-1 rounded transition border border-rose-800/40"
                        >
                          Ẩn
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal Form Thêm / Sửa Phim */}
      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm">
          <div className="bg-slate-900 border border-slate-700 rounded-2xl w-full max-w-xl max-h-[90vh] overflow-y-auto p-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3 mb-4">
              <h2 className="text-lg font-bold text-white">
                {editingMovie ? 'Cập Nhật Thông Tin Phim' : 'Thêm Phim Mới'}
              </h2>
              <button
                onClick={() => setShowModal(false)}
                className="text-slate-400 hover:text-white text-lg font-bold"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block text-slate-300 font-semibold mb-1">Tên phim *</label>
                <input
                  type="text"
                  required
                  value={formData.title}
                  onChange={(e) => setFormData({ ...formData, title: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  placeholder="Nhập tên phim"
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Mô tả nội dung</label>
                <textarea
                  rows="3"
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  placeholder="Mô tả cốt truyện..."
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Thời lượng (phút) *</label>
                  <input
                    type="number"
                    required
                    min="1"
                    value={formData.duration}
                    onChange={(e) => setFormData({ ...formData, duration: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  />
                </div>
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Độ tuổi (Age Rating) *</label>
                  <select
                    value={formData.ageRating}
                    onChange={(e) => setFormData({ ...formData, ageRating: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  >
                    <option value="P">P (Mọi lứa tuổi)</option>
                    <option value="K">K (Dưới 13 có phụ huynh)</option>
                    <option value="T13">T13 (Trên 13 tuổi)</option>
                    <option value="T16">T16 (Trên 16 tuổi)</option>
                    <option value="T18">T18 (Trên 18 tuổi)</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Ngày khởi chiếu</label>
                  <input
                    type="date"
                    value={formData.releaseDate}
                    onChange={(e) => setFormData({ ...formData, releaseDate: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  />
                </div>
                <div>
                  <label className="block text-slate-300 font-semibold mb-1">Trạng thái phát hành</label>
                  <select
                    value={formData.status}
                    onChange={(e) => setFormData({ ...formData, status: e.target.value })}
                    className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  >
                    <option value="NOW_SHOWING">Đang Chiếu (NOW_SHOWING)</option>
                    <option value="UPCOMING">Sắp Chiếu (UPCOMING)</option>
                    <option value="ENDED">Kết Thúc (ENDED)</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">URL Poster (Ảnh) *</label>
                <input
                  type="url"
                  required
                  value={formData.posterUrl}
                  onChange={(e) => setFormData({ ...formData, posterUrl: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  placeholder="https://images.unsplash.com/..."
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">URL Trailer (YouTube) *</label>
                <input
                  type="url"
                  required
                  value={formData.trailerUrl}
                  onChange={(e) => setFormData({ ...formData, trailerUrl: e.target.value })}
                  className="w-full bg-slate-950 border border-slate-700 rounded-lg p-2.5 text-white"
                  placeholder="https://www.youtube.com/watch?v=..."
                />
              </div>

              <div>
                <label className="block text-slate-300 font-semibold mb-1">Thể loại phim</label>
                <div className="flex flex-wrap gap-2 pt-1">
                  {genres.map((g) => {
                    const isSelected = formData.genreIds.includes(g.genreId);
                    return (
                      <button
                        type="button"
                        key={g.genreId}
                        onClick={() => handleToggleGenre(g.genreId)}
                        className={`px-3 py-1.5 rounded-lg border text-xs font-medium transition ${
                          isSelected
                            ? 'bg-red-600 border-red-500 text-white'
                            : 'bg-slate-950 border-slate-700 text-slate-400 hover:text-white'
                        }`}
                      >
                        {g.genreName}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-4 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowModal(false)}
                  className="px-4 py-2 bg-slate-800 text-slate-300 rounded-lg hover:bg-slate-700 transition"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submitting}
                  className="px-5 py-2 bg-red-600 hover:bg-red-500 text-white font-bold rounded-lg shadow-md transition"
                >
                  {submitting ? 'Đang lưu...' : 'Lưu Phim'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
