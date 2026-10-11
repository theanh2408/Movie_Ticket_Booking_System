import { useEffect, useState } from 'react';
import { api } from '../api';
import { useToast } from '../context/ToastContext';
import { useAsync } from '../lib/hooks';
import { SHOWTIME_STATUS, showtimeState } from '../lib/constants';
import { addDaysISO, addMinutesToDate, fmtDate, hhmm, money, todayISO } from '../lib/format';
import { Badge, ConfirmDialog, EmptyState, ErrorBox, Field, Modal, PageHeader, Spinner } from '../components/ui';

function ConflictList({ conflicts }) {
  return (
    <div className="errorbox stack" role="alert">
      <strong>Trùng giờ với {conflicts.length} suất khác trong cùng phòng:</strong>
      <ul>{conflicts.map((c) => <li key={c.ShowtimeID}>{c.MovieTitle} · {fmtDate(c.Date)} · {c.Start_hour}–{c.EndHour}</li>)}</ul>
    </div>
  );
}

function ShowtimeForm({ showtime, movies, rooms, onClose, onSaved }) {
  const toast = useToast();
  const editing = !!showtime;
  const locked = editing && showtime.Booked > 0;
  const [f, setF] = useState({
    MovieID: showtime?.MovieID || '', RoomID: showtime?.RoomID || '', Date: showtime?.Date || addDaysISO(todayISO(), 1),
    Start_hour: showtime?.Start_hour || '', BasePrice: showtime?.BasePrice || 90000,
  });
  const [errs, setErrs] = useState({});
  const [conflicts, setConflicts] = useState([]);
  const [busy, setBusy] = useState(false);
  const set = (k) => (e) => setF((x) => ({ ...x, [k]: e.target.value }));
  const movie = movies.find((m) => m.MovieID === f.MovieID);
  const end = movie && f.Date && f.Start_hour ? hhmm(addMinutesToDate(new Date(`${f.Date}T${f.Start_hour}:00`), movie.Duration)) : '';

  // Kiểm tra trùng giờ ngay khi đổi phim/phòng/ngày/giờ
  useEffect(() => {
    if (!f.MovieID || !f.RoomID || !f.Date || !f.Start_hour) { setConflicts([]); return undefined; }
    let alive = true;
    const t = setTimeout(() => {
      api.showtimes.conflicts({ MovieID: f.MovieID, RoomID: f.RoomID, Date: f.Date, Start_hour: f.Start_hour }, showtime?.ShowtimeID)
        .then((c) => alive && setConflicts(c)).catch(() => {});
    }, 250);
    return () => { alive = false; clearTimeout(t); };
  }, [f.MovieID, f.RoomID, f.Date, f.Start_hour, showtime?.ShowtimeID]);

  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErrs({});
    try {
      const body = { ...f, BasePrice: Number(f.BasePrice) };
      if (editing) await api.showtimes.update(showtime.ShowtimeID, body); else await api.showtimes.create(body);
      toast.success(editing ? 'Đã cập nhật suất chiếu.' : 'Đã tạo suất chiếu.');
      onSaved();
    } catch (err) {
      if (err.fields) setErrs(err.fields);
      if (err.code === 'CONFLICT') setConflicts(err.data?.conflicts || []);
      toast.error(err.message);
    } finally { setBusy(false); }
  };

  return (
    <Modal wide title={editing ? 'Sửa suất chiếu' : 'Tạo suất chiếu'} onClose={onClose}
      footer={<><button className="btn" type="button" onClick={onClose}>Hủy</button><button className="btn primary" form="st-form" disabled={busy || conflicts.length > 0}>{busy ? 'Đang lưu…' : 'Lưu suất chiếu'}</button></>}>
      <form id="st-form" onSubmit={submit} noValidate className="form-grid">
        {locked && <div className="errorbox info span-2">Suất này đã có {showtime.Booked} vé đặt nên chỉ đổi được giá vé. Giá mới chỉ áp dụng cho ghế còn trống.</div>}
        <Field label="Phim" required error={errs.MovieID} className="span-2">
          <select className="input" value={f.MovieID} onChange={set('MovieID')} disabled={locked}>
            <option value="">Chọn phim</option>{movies.map((m) => <option key={m.MovieID} value={m.MovieID}>{m.Title} ({m.Duration} phút)</option>)}
          </select>
        </Field>
        <Field label="Phòng chiếu" required error={errs.RoomID}>
          <select className="input" value={f.RoomID} onChange={set('RoomID')} disabled={locked}>
            <option value="">Chọn phòng</option>{rooms.map((r) => <option key={r.RoomID} value={r.RoomID} disabled={r.Status !== 'Active'}>{r.RoomName}{r.Status !== 'Active' ? ' (bảo trì)' : ''}</option>)}
          </select>
        </Field>
        <Field label="Ngày chiếu" required error={errs.Date}><input className="input" type="date" min={todayISO()} value={f.Date} onChange={set('Date')} disabled={locked} /></Field>
        <Field label="Giờ bắt đầu" required error={errs.Start_hour} hint={end ? `Kết thúc dự kiến ${end}` : ''}><input className="input" type="time" step="300" value={f.Start_hour} onChange={set('Start_hour')} disabled={locked} /></Field>
        <Field label="Giá vé (đ)" required error={errs.BasePrice} hint={f.BasePrice ? money(Number(f.BasePrice)) : ''}><input className="input" type="number" min="10000" step="1000" value={f.BasePrice} onChange={set('BasePrice')} /></Field>
        {conflicts.length > 0 && <div className="span-2"><ConflictList conflicts={conflicts} /></div>}
      </form>
    </Modal>
  );
}

export default function ShowtimesPage() {
  const toast = useToast();
  const [flt, setFlt] = useState({ from: todayISO(), to: addDaysISO(todayISO(), 6), roomId: '', movieId: '', status: '' });
  const set = (k) => (e) => setFlt((x) => ({ ...x, [k]: e.target.value }));
  const { data: movies } = useAsync(() => api.movies.list({}), []);
  const { data: rooms } = useAsync(() => api.rooms.list(), []);
  const { data: list, loading, error, reload } = useAsync(() => api.showtimes.list(flt), [flt.from, flt.to, flt.roomId, flt.movieId, flt.status]);
  const [form, setForm] = useState(null);
  const [cancel, setCancel] = useState(null);
  const [busy, setBusy] = useState(false);

  const doCancel = async () => {
    setBusy(true);
    try { await api.showtimes.cancel(cancel.ShowtimeID); toast.success('Đã hủy suất chiếu.'); setCancel(null); reload(); }
    catch (err) { toast.error(err.message); setCancel(null); } finally { setBusy(false); }
  };
  const usableMovies = (movies || []).filter((m) => m.Status !== 'Hidden');

  return (
    <div className="page">
      <PageHeader title="Lịch chiếu" subtitle="Hệ thống tự kiểm tra trùng giờ trong cùng phòng, có tính thêm 15 phút dọn phòng.">
        <button className="btn primary" onClick={() => setForm({})} disabled={!movies || !rooms}>Tạo suất chiếu</button>
      </PageHeader>

      <div className="toolbar">
        <Field label="Từ ngày"><input className="input" type="date" value={flt.from} onChange={set('from')} /></Field>
        <Field label="Đến ngày"><input className="input" type="date" value={flt.to} min={flt.from} onChange={set('to')} /></Field>
        <Field label="Phòng"><select className="input" value={flt.roomId} onChange={set('roomId')}><option value="">Tất cả</option>{(rooms || []).map((r) => <option key={r.RoomID} value={r.RoomID}>{r.RoomName}</option>)}</select></Field>
        <Field label="Phim"><select className="input" value={flt.movieId} onChange={set('movieId')}><option value="">Tất cả</option>{(movies || []).map((m) => <option key={m.MovieID} value={m.MovieID}>{m.Title}</option>)}</select></Field>
        <Field label="Trạng thái"><select className="input" value={flt.status} onChange={set('status')}><option value="">Tất cả</option>{Object.entries(SHOWTIME_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}</select></Field>
      </div>

      <ErrorBox error={error} onRetry={reload} />
      {!list && loading && <Spinner />}
      {list && list.length === 0 && <EmptyState title="Không có suất chiếu trong khoảng này" action={<button className="btn primary" onClick={() => setForm({})}>Tạo suất chiếu</button>}>Thử mở rộng khoảng ngày hoặc bỏ bớt bộ lọc.</EmptyState>}
      {list && list.length > 0 && (
        <div className={`table-wrap ${loading ? 'dim' : ''}`}>
          <table className="table">
            <thead><tr><th>Ngày</th><th>Giờ chiếu</th><th>Phim</th><th>Phòng</th><th className="num">Giá vé</th><th className="num">Ghế đã đặt</th><th>Trạng thái</th><th><span className="sr">Thao tác</span></th></tr></thead>
            <tbody>
              {list.map((s) => {
                const state = showtimeState(s);
                return (
                  <tr key={s.ShowtimeID}>
                    <td>{fmtDate(s.Date)}</td>
                    <td>{s.Start_hour} – {s.EndHour}</td>
                    <td>{s.MovieTitle}</td>
                    <td>{s.RoomName}</td>
                    <td className="num">{money(s.BasePrice)}</td>
                    <td className="num">{s.Booked}/{s.Total}</td>
                    <td><Badge map={SHOWTIME_STATUS} value={state} /></td>
                    <td className="actions">
                      {state === 'Upcoming' && <>
                        <button className="btn sm" onClick={() => setForm({ showtime: s })}>Sửa</button>
                        <button className="btn sm danger-ghost" onClick={() => setCancel(s)}>Hủy suất</button>
                      </>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {form && movies && rooms && <ShowtimeForm showtime={form.showtime} movies={usableMovies} rooms={rooms} onClose={() => setForm(null)} onSaved={() => { setForm(null); reload(); }} />}
      {cancel && (
        <ConfirmDialog danger busy={busy} title="Hủy suất chiếu?" confirmText="Hủy suất chiếu" onCancel={() => setCancel(null)} onConfirm={doCancel}>
          <p>{cancel.MovieTitle} · {fmtDate(cancel.Date)} · {cancel.Start_hour} · {cancel.RoomName}</p>
          <p className="muted">Chỉ hủy được suất chưa có vé đặt. Suất đã hủy sẽ không hiện cho khách hàng.</p>
        </ConfirmDialog>
      )}
    </div>
  );
}
