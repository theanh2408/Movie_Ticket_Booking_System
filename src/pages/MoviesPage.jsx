import { useState } from 'react';
import { api } from '../api';
import { useToast } from '../context/ToastContext';
import { useAsync, useDebounced } from '../lib/hooks';
import { AGE_RATINGS, MOVIE_STATUS } from '../lib/constants';
import { fmtDate } from '../lib/format';
import { Badge, ConfirmDialog, EmptyState, ErrorBox, Field, Modal, PageHeader, Poster, Spinner } from '../components/ui';

const BLANK = { Title: '', Description: '', Duration: '', PosterURL: '', TrailerURL: '', AgeRating: 'T13', ReleaseDate: '', Status: 'ComingSoon', GenreIDs: [] };

function MovieForm({ movie, genres, onClose, onSaved }) {
  const toast = useToast();
  const editing = !!movie;
  const [f, setF] = useState(editing ? { ...BLANK, ...movie } : BLANK);
  const [errs, setErrs] = useState({});
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setF((x) => ({ ...x, [k]: e.target.value }));
  const toggleGenre = (id) => setF((x) => ({ ...x, GenreIDs: x.GenreIDs.includes(id) ? x.GenreIDs.filter((g) => g !== id) : [...x.GenreIDs, id] }));

  const submit = async (e) => {
    e.preventDefault();
    setBusy(true); setErrs({});
    try {
      const body = { ...f, Duration: Number(f.Duration) };
      if (editing) await api.movies.update(movie.MovieID, body); else await api.movies.create(body);
      toast.success(editing ? 'Đã cập nhật phim.' : 'Đã thêm phim mới.');
      onSaved();
    } catch (err) {
      if (err.fields) setErrs(err.fields);
      toast.error(err.message);
    } finally { setBusy(false); }
  };

  return (
    <Modal wide title={editing ? 'Sửa thông tin phim' : 'Thêm phim'} onClose={onClose}
      footer={<><button className="btn" type="button" onClick={onClose}>Hủy</button><button className="btn primary" form="movie-form" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu phim'}</button></>}>
      <form id="movie-form" onSubmit={submit} noValidate className="form-grid">
        <Field label="Tên phim" required error={errs.Title} className="span-2"><input className="input" value={f.Title} onChange={set('Title')} autoFocus /></Field>
        <Field label="Thời lượng (phút)" required error={errs.Duration}><input className="input" type="number" min="1" value={f.Duration} onChange={set('Duration')} /></Field>
        <Field label="Ngày khởi chiếu" required error={errs.ReleaseDate}><input className="input" type="date" value={f.ReleaseDate} onChange={set('ReleaseDate')} /></Field>
        <Field label="Độ tuổi" required error={errs.AgeRating}>
          <select className="input" value={f.AgeRating} onChange={set('AgeRating')}>{AGE_RATINGS.map((a) => <option key={a.value} value={a.value}>{a.label}</option>)}</select>
        </Field>
        <Field label="Trạng thái" required error={errs.Status}>
          <select className="input" value={f.Status} onChange={set('Status')}>{Object.entries(MOVIE_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}</select>
        </Field>
        <div className="field span-2">
          <span className="field-label">Thể loại<span className="req"> *</span></span>
          <div className="chips" role="group" aria-label="Thể loại">
            {genres.map((g) => (
              <button key={g.Genre_ID} type="button" className={`chip ${f.GenreIDs.includes(g.Genre_ID) ? 'on' : ''}`} aria-pressed={f.GenreIDs.includes(g.Genre_ID)} onClick={() => toggleGenre(g.Genre_ID)}>{g.GenreName}</button>
            ))}
          </div>
          {errs.GenreIDs && <span className="error-text" role="alert">{errs.GenreIDs}</span>}
        </div>
        <Field label="Mô tả" className="span-2"><textarea className="input" rows="3" value={f.Description} onChange={set('Description')} /></Field>
        <Field label="Đường dẫn poster" error={errs.PosterURL} hint="Ảnh dọc, bắt đầu bằng https://"><input className="input" value={f.PosterURL} onChange={set('PosterURL')} /></Field>
        <Field label="Đường dẫn trailer" error={errs.TrailerURL}><input className="input" value={f.TrailerURL} onChange={set('TrailerURL')} /></Field>
      </form>
    </Modal>
  );
}

function MovieDetail({ id, genres, onClose, onEdit }) {
  const { data: m, loading, error } = useAsync(() => api.movies.get(id), [id]);
  const names = (m?.GenreIDs || []).map((g) => genres.find((x) => x.Genre_ID === g)?.GenreName).filter(Boolean);
  return (
    <Modal title="Chi tiết phim" onClose={onClose} footer={m && <><button className="btn" onClick={onClose}>Đóng</button><button className="btn primary" onClick={() => onEdit(m)}>Sửa phim</button></>}>
      {loading && <Spinner />}
      <ErrorBox error={error} />
      {m && (
        <div className="detail-movie">
          <Poster movie={m} size="lg" />
          <div>
            <h3>{m.Title}</h3>
            <p className="muted">{names.join(', ')}</p>
            <dl className="dl">
              <dt>Thời lượng</dt><dd>{m.Duration} phút</dd>
              <dt>Độ tuổi</dt><dd>{m.AgeRating}</dd>
              <dt>Khởi chiếu</dt><dd>{fmtDate(m.ReleaseDate)}</dd>
              <dt>Trạng thái</dt><dd><Badge map={MOVIE_STATUS} value={m.Status} /></dd>
              <dt>Suất chiếu sắp tới</dt><dd>{m.UpcomingShowtimes}</dd>
              {m.TrailerURL && <><dt>Trailer</dt><dd><a href={m.TrailerURL} target="_blank" rel="noreferrer">Mở trailer</a></dd></>}
            </dl>
            {m.Description && <p>{m.Description}</p>}
          </div>
        </div>
      )}
    </Modal>
  );
}

export default function MoviesPage() {
  const toast = useToast();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const q = useDebounced(search);
  const { data: genres } = useAsync(() => api.genres.list(), []);
  const { data: movies, loading, error, reload } = useAsync(() => api.movies.list({ search: q, status }), [q, status]);
  const [form, setForm] = useState(null); // { movie? }
  const [detailId, setDetailId] = useState(null);
  const [confirm, setConfirm] = useState(null); // { movie, to }
  const [busy, setBusy] = useState(false);

  const doStatus = async () => {
    setBusy(true);
    try {
      await api.movies.setStatus(confirm.movie.MovieID, confirm.to);
      toast.success(confirm.to === 'Hidden' ? 'Đã ngừng hiển thị phim.' : 'Đã hiển thị lại phim.');
      setConfirm(null); reload();
    } catch (err) { toast.error(err.message); setConfirm(null); } finally { setBusy(false); }
  };

  return (
    <div className="page">
      <PageHeader title="Phim" subtitle="Thêm, sửa thông tin và ngừng hiển thị phim. Phim không bị xóa để giữ nguyên lịch sử đặt vé.">
        <button className="btn primary" onClick={() => setForm({})} disabled={!genres}>Thêm phim</button>
      </PageHeader>

      <div className="toolbar">
        <Field label="Tìm theo tên"><input className="input" placeholder="Nhập tên phim" value={search} onChange={(e) => setSearch(e.target.value)} /></Field>
        <Field label="Trạng thái">
          <select className="input" value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="">Tất cả</option>{Object.entries(MOVIE_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}
          </select>
        </Field>
      </div>

      <ErrorBox error={error} onRetry={reload} />
      {!movies && loading && <Spinner />}
      {movies && movies.length === 0 && <EmptyState title="Không có phim phù hợp" action={<button className="btn primary" onClick={() => setForm({})}>Thêm phim</button>}>Thử đổi từ khóa hoặc bộ lọc.</EmptyState>}
      {movies && movies.length > 0 && (
        <div className={`table-wrap ${loading ? 'dim' : ''}`}>
          <table className="table">
            <thead><tr><th>Phim</th><th>Thể loại</th><th className="num">Thời lượng</th><th>Độ tuổi</th><th>Khởi chiếu</th><th>Trạng thái</th><th><span className="sr">Thao tác</span></th></tr></thead>
            <tbody>
              {movies.map((m) => (
                <tr key={m.MovieID}>
                  <td><button className="linklike movie-cell" onClick={() => setDetailId(m.MovieID)}><Poster movie={m} /><span>{m.Title}</span></button></td>
                  <td>{m.GenreIDs.map((g) => genres?.find((x) => x.Genre_ID === g)?.GenreName).filter(Boolean).join(', ')}</td>
                  <td className="num">{m.Duration} phút</td>
                  <td>{m.AgeRating}</td>
                  <td>{fmtDate(m.ReleaseDate)}</td>
                  <td><Badge map={MOVIE_STATUS} value={m.Status} /></td>
                  <td className="actions">
                    <button className="btn sm" onClick={() => setForm({ movie: m })}>Sửa</button>
                    {m.Status === 'Hidden'
                      ? <button className="btn sm" onClick={() => setConfirm({ movie: m, to: 'ComingSoon' })}>Hiển thị lại</button>
                      : <button className="btn sm danger-ghost" onClick={() => setConfirm({ movie: m, to: 'Hidden' })}>Ngừng hiển thị</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {form && genres && <MovieForm movie={form.movie} genres={genres} onClose={() => setForm(null)} onSaved={() => { setForm(null); reload(); }} />}
      {detailId && genres && <MovieDetail id={detailId} genres={genres} onClose={() => setDetailId(null)} onEdit={(m) => { setDetailId(null); setForm({ movie: m }); }} />}
      {confirm && (
        <ConfirmDialog danger={confirm.to === 'Hidden'} busy={busy} onCancel={() => setConfirm(null)} onConfirm={doStatus}
          title={confirm.to === 'Hidden' ? 'Ngừng hiển thị phim?' : 'Hiển thị lại phim?'}
          confirmText={confirm.to === 'Hidden' ? 'Ngừng hiển thị' : 'Hiển thị lại'}>
          {confirm.to === 'Hidden'
            ? <p>“{confirm.movie.Title}” sẽ không còn hiện cho khách hàng. Các vé đã đặt trước đó vẫn được giữ nguyên.</p>
            : <p>“{confirm.movie.Title}” sẽ được chuyển sang trạng thái “Sắp chiếu”. Bạn có thể đổi lại trong phần Sửa phim.</p>}
        </ConfirmDialog>
      )}
    </div>
  );
}
