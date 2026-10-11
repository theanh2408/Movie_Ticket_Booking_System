import { useEffect, useMemo, useState } from 'react';
import { api } from '../api';
import { useToast } from '../context/ToastContext';
import { useAsync } from '../lib/hooks';
import { ROOM_STATUS } from '../lib/constants';
import { rowLabel } from '../lib/format';
import { Badge, ErrorBox, Field, Modal, PageHeader, SeatGrid, SeatLegend, Spinner } from '../components/ui';

function RoomForm({ room, onClose, onSaved }) {
  const toast = useToast();
  const [f, setF] = useState({ RoomName: room?.RoomName || '', Status: room?.Status || 'Active' });
  const [errs, setErrs] = useState({});
  const [busy, setBusy] = useState(false);
  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErrs({});
    try {
      const saved = room ? await api.rooms.update(room.RoomID, f) : await api.rooms.create(f);
      toast.success(room ? 'Đã cập nhật phòng chiếu.' : 'Đã thêm phòng chiếu. Hãy tạo sơ đồ ghế cho phòng.');
      onSaved(saved);
    } catch (err) { if (err.fields) setErrs(err.fields); toast.error(err.message); } finally { setBusy(false); }
  };
  return (
    <Modal title={room ? 'Sửa phòng chiếu' : 'Thêm phòng chiếu'} onClose={onClose}
      footer={<><button className="btn" type="button" onClick={onClose}>Hủy</button><button className="btn primary" form="room-form" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu phòng'}</button></>}>
      <form id="room-form" onSubmit={submit} noValidate>
        <Field label="Tên phòng" required error={errs.RoomName}><input className="input" value={f.RoomName} onChange={(e) => setF({ ...f, RoomName: e.target.value })} autoFocus /></Field>
        <Field label="Trạng thái" required error={errs.Status} hint="Phòng đang bảo trì sẽ không xếp được suất chiếu mới.">
          <select className="input" value={f.Status} onChange={(e) => setF({ ...f, Status: e.target.value })}>{Object.entries(ROOM_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}</select>
        </Field>
      </form>
    </Modal>
  );
}

function SeatEditor({ room, onSaved }) {
  const toast = useToast();
  const orig = useMemo(() => new Map(room.Seats.map((s) => [`${s.Row}${s.Column}`, s])), [room]);
  const [draft, setDraft] = useState(orig);
  const [dims, setDims] = useState({ rows: 8, cols: 10 });
  const [gen, setGen] = useState({ rows: 8, cols: 10 });
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    setDraft(orig);
    const rows = Math.max(0, ...room.Seats.map((s) => s.Row.charCodeAt(0) - 64));
    const cols = Math.max(0, ...room.Seats.map((s) => Number(s.Column)));
    const d = { rows: rows || 6, cols: cols || 8 };
    setDims(d); setGen(d);
  }, [orig, room.Seats]);

  const dirty = draft.size !== orig.size || [...draft.keys()].some((k) => !orig.has(k));
  const rows = Array.from({ length: dims.rows }, (_, i) => rowLabel(i));
  const getCell = (row, col) => {
    const code = `${row}${col}`;
    const s = draft.get(code);
    if (!s) return null;
    return s.Locked ? { state: 'locked', title: `Ghế ${code} đã có vé, không thể bỏ` } : { state: 'available', title: `Ghế ${code} – bấm để bỏ ghế` };
  };
  const onCell = (row, col, cell) => {
    const code = `${row}${col}`;
    const next = new Map(draft);
    if (!cell) next.set(code, { Row: row, Column: String(col) });
    else if (cell.state === 'locked') { toast.error(`Ghế ${code} đã có vé đặt nên không thể bỏ.`); return; }
    else next.delete(code);
    setDraft(next);
  };
  const generate = () => {
    const rowsN = Math.min(Math.max(Number(gen.rows) || 0, 1), 20);
    const colsN = Math.min(Math.max(Number(gen.cols) || 0, 1), 30);
    const lockedOutside = [...orig.values()].filter((s) => s.Locked && (s.Row.charCodeAt(0) - 64 > rowsN || Number(s.Column) > colsN));
    if (lockedOutside.length) { toast.error(`Không thể thu nhỏ lưới vì ghế ${lockedOutside.map((s) => s.Row + s.Column).join(', ')} đã có vé.`); return; }
    const next = new Map();
    for (let r = 0; r < rowsN; r++) for (let c = 1; c <= colsN; c++) {
      const code = `${rowLabel(r)}${c}`;
      next.set(code, orig.get(code) || { Row: rowLabel(r), Column: String(c) });
    }
    setDraft(next); setDims({ rows: rowsN, cols: colsN }); setGen({ rows: rowsN, cols: colsN });
  };
  const save = async () => {
    setBusy(true);
    try {
      await api.rooms.saveSeats(room.RoomID, [...draft.values()].map((s) => ({ Row: s.Row, Column: s.Column })));
      toast.success('Đã lưu sơ đồ ghế.'); onSaved();
    } catch (err) { toast.error(err.message); } finally { setBusy(false); }
  };

  return (
    <section className="panel">
      <div className="panel-head">
        <h2>Sơ đồ ghế · {room.RoomName}</h2>
        <span className="muted">{draft.size} ghế</span>
      </div>
      <div className="toolbar tight">
        <Field label="Số hàng"><input className="input narrow" type="number" min="1" max="20" value={gen.rows} onChange={(e) => setGen({ ...gen, rows: e.target.value })} /></Field>
        <Field label="Ghế mỗi hàng"><input className="input narrow" type="number" min="1" max="30" value={gen.cols} onChange={(e) => setGen({ ...gen, cols: e.target.value })} /></Field>
        <button className="btn" onClick={generate}>Tạo lưới ghế</button>
      </div>
      <p className="hint">Bấm vào ghế để bỏ ghế (lối đi), bấm vào ô trống để thêm ghế. Ghế đã có vé bị khóa.</p>
      <SeatGrid rows={rows} cols={dims.cols} getCell={getCell} onCellClick={onCell} emptyClickable />
      <SeatLegend items={[['available', 'Ghế'], ['locked', 'Đã có vé (khóa)'], ['empty', 'Ô trống']]} />
      <div className="panel-foot">
        {dirty && <span className="muted">Có thay đổi chưa lưu.</span>}
        <button className="btn" disabled={!dirty || busy} onClick={() => setDraft(orig)}>Hoàn tác</button>
        <button className="btn primary" disabled={!dirty || busy} onClick={save}>{busy ? 'Đang lưu…' : 'Lưu sơ đồ ghế'}</button>
      </div>
    </section>
  );
}

export default function RoomsPage() {
  const { data: cinema } = useAsync(() => api.rooms.cinema(), []);
  const { data: rooms, loading, error, reload } = useAsync(() => api.rooms.list(), []);
  const [selected, setSelected] = useState(null);
  const [form, setForm] = useState(null);
  const current = selected || rooms?.[0]?.RoomID;
  const { data: room, loading: roomLoading, error: roomError, reload: reloadRoom } = useAsync(() => (current ? api.rooms.get(current) : Promise.resolve(null)), [current]);
  const refreshAll = () => { reload(); reloadRoom(); };

  return (
    <div className="page">
      <PageHeader title="Phòng chiếu & ghế" subtitle={cinema ? cinema.CinemaName : ''}>
        <button className="btn primary" onClick={() => setForm({})}>Thêm phòng</button>
      </PageHeader>
      <ErrorBox error={error} onRetry={reload} />
      {!rooms && loading && <Spinner />}
      {rooms && (
        <div className="rooms-layout">
          <ul className="room-list" aria-label="Danh sách phòng">
            {rooms.map((r) => (
              <li key={r.RoomID}>
                <button className={`room-item ${r.RoomID === current ? 'on' : ''}`} onClick={() => setSelected(r.RoomID)} aria-current={r.RoomID === current}>
                  <strong>{r.RoomName}</strong>
                  <span className="muted">{r.SeatCount} ghế · {r.UpcomingShowtimes} suất sắp tới</span>
                  <Badge map={ROOM_STATUS} value={r.Status} />
                </button>
              </li>
            ))}
          </ul>
          <div className="room-detail">
            <ErrorBox error={roomError} onRetry={reloadRoom} />
            {roomLoading && !room && <Spinner />}
            {room && (
              <>
                <div className="room-bar">
                  <div><strong>{room.RoomName}</strong> <Badge map={ROOM_STATUS} value={room.Status} /></div>
                  <button className="btn sm" onClick={() => setForm({ room })}>Sửa thông tin phòng</button>
                </div>
                <SeatEditor key={room.RoomID + room.Seats.length} room={room} onSaved={refreshAll} />
              </>
            )}
          </div>
        </div>
      )}
      {form && <RoomForm room={form.room} onClose={() => setForm(null)} onSaved={(saved) => { setForm(null); if (!form.room && saved?.RoomID) setSelected(saved.RoomID); refreshAll(); }} />}
    </div>
  );
}
