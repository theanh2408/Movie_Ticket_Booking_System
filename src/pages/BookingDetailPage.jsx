import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api } from '../api';
import { useToast } from '../context/ToastContext';
import { useAsync } from '../lib/hooks';
import { INVOICE_STATUS, PAYMENT_STATUS, TICKET_STATUS, showtimeState } from '../lib/constants';
import { fmtDate, fmtDateTime, money, seatCode } from '../lib/format';
import { Badge, ConfirmDialog, ErrorBox, Modal, PageHeader, SeatGrid, SeatLegend, Spinner } from '../components/ui';

function ChangeSeatModal({ booking, ticket, onClose, onDone }) {
  const toast = useToast();
  const { data: seats, loading, error } = useAsync(() => api.showtimes.seatMap(booking.Showtime.ShowtimeID), []);
  const [pick, setPick] = useState(null);
  const [busy, setBusy] = useState(false);
  const byCode = new Map((seats || []).map((s) => [seatCode(s.Seat), s]));
  const rows = [...new Set((seats || []).map((s) => s.Seat.Row))].sort();
  const cols = Math.max(0, ...(seats || []).map((s) => Number(s.Seat.Column)));
  const mineCode = seatCode(ticket.Seat);
  const getCell = (row, col) => {
    const code = `${row}${col}`;
    const s = byCode.get(code);
    if (!s) return null;
    if (code === mineCode) return { state: 'mine', title: `Ghế hiện tại ${code}`, disabled: true };
    if (pick?.Showtime_SeatID === s.Showtime_SeatID) return { state: 'selected', title: `Đã chọn ${code}` };
    if (s.Status === 'Available') return { state: 'available', title: `Ghế ${code} còn trống` };
    return { state: s.Status === 'Held' ? 'held' : 'booked', title: `Ghế ${code} ${s.Status === 'Held' ? 'đang được giữ' : 'đã có người đặt'}`, disabled: true };
  };
  const submit = async () => {
    setBusy(true);
    try { await api.bookings.changeSeat(booking.InvoiceID, ticket.TicketID, pick.Showtime_SeatID); toast.success(`Đã đổi ghế ${mineCode} sang ${seatCode(pick.Seat)}.`); onDone(); }
    catch (err) { toast.error(err.message); onDone(); } finally { setBusy(false); }
  };
  return (
    <Modal wide title={`Đổi ghế ${mineCode}`} onClose={onClose}
      footer={<><button className="btn" onClick={onClose}>Quay lại</button><button className="btn primary" disabled={!pick || busy} onClick={submit}>{busy ? 'Đang đổi…' : pick ? `Đổi sang ghế ${seatCode(pick.Seat)}` : 'Chọn ghế mới'}</button></>}>
      <p className="muted">{booking.Showtime.MovieTitle} · {fmtDate(booking.Showtime.Date)} · {booking.Showtime.Start_hour} · {booking.Showtime.RoomName}. Giá vé không đổi.</p>
      {loading && <Spinner />}<ErrorBox error={error} />
      {seats && <><SeatGrid rows={rows} cols={cols} getCell={getCell} onCellClick={(r, c, cell) => { if (cell) setPick(byCode.get(`${r}${c}`)); }} />
        <SeatLegend items={[['available', 'Còn trống'], ['selected', 'Ghế mới'], ['mine', 'Ghế hiện tại'], ['booked', 'Đã đặt']]} /></>}
    </Modal>
  );
}

export default function BookingDetailPage() {
  const { id } = useParams();
  const toast = useToast();
  const { data: b, loading, error, reload } = useAsync(() => api.bookings.get(id), [id]);
  const [sel, setSel] = useState([]);
  const [confirm, setConfirm] = useState(false);
  const [change, setChange] = useState(null);
  const [busy, setBusy] = useState(false);

  if (error?.status === 404) return <div className="page"><ErrorBox error={error} /><Link className="btn" to="/bookings">Về danh sách đặt vé</Link></div>;
  if (!b) return <div className="page">{loading && <Spinner />}<ErrorBox error={error} onRetry={reload} /></div>;

  const state = b.Showtime ? showtimeState(b.Showtime) : 'Ended';
  const canEdit = b.Status !== 'Cancelled' && state === 'Upcoming';
  const live = b.Tickets.filter((t) => t.Status === 'Booked');
  const toggle = (tid) => setSel((s) => (s.includes(tid) ? s.filter((x) => x !== tid) : [...s, tid]));
  const selTickets = live.filter((t) => sel.includes(t.TicketID));
  const cancelAll = selTickets.length === live.length;

  const doCancel = async () => {
    setBusy(true);
    try { await api.bookings.cancelTickets(b.InvoiceID, sel); toast.success('Đã hủy vé. Ghế đã được giải phóng.'); setSel([]); setConfirm(false); reload(); }
    catch (err) { toast.error(err.message); setConfirm(false); reload(); } finally { setBusy(false); }
  };

  return (
    <div className="page">
      <p className="crumb"><Link to="/bookings">Đặt vé</Link> / {b.InvoiceID}</p>
      <PageHeader title={`Đặt vé ${b.InvoiceID}`} subtitle={`Tạo lúc ${fmtDateTime(b.CreateDate)}`}>
        <Badge map={INVOICE_STATUS} value={b.Status} />
      </PageHeader>
      {!canEdit && b.Status !== 'Cancelled' && <div className="errorbox info">Suất chiếu đã bắt đầu hoặc đã kết thúc nên không thể hủy vé hay đổi ghế.</div>}

      <div className="two-col">
        <section className="panel">
          <h2>Khách hàng</h2>
          <dl className="dl"><dt>Họ tên</dt><dd>{b.Customer.FullName}</dd><dt>Điện thoại</dt><dd>{b.Customer.Phone}</dd><dt>Email</dt><dd>{b.Customer.Email}</dd></dl>
        </section>
        <section className="panel">
          <h2>Suất chiếu</h2>
          <dl className="dl"><dt>Phim</dt><dd>{b.Showtime.MovieTitle}</dd><dt>Ngày giờ</dt><dd>{fmtDate(b.Showtime.Date)} · {b.Showtime.Start_hour}–{b.Showtime.EndHour}</dd><dt>Phòng</dt><dd>{b.Showtime.RoomName}</dd></dl>
        </section>
      </div>

      <section className="panel">
        <div className="panel-head">
          <h2>Vé ({live.length} đang hiệu lực)</h2>
          {canEdit && <button className="btn danger" disabled={!sel.length} onClick={() => setConfirm(true)}>Hủy {sel.length ? `${sel.length} vé đã chọn` : 'vé đã chọn'}</button>}
        </div>
        <div className="table-wrap flat">
          <table className="table">
            <thead><tr>{canEdit && <th className="chk"><span className="sr">Chọn</span></th>}<th>Ghế</th><th>Mã vé</th><th className="num">Giá</th><th>Trạng thái</th>{canEdit && <th><span className="sr">Thao tác</span></th>}</tr></thead>
            <tbody>
              {b.Tickets.map((t) => (
                <tr key={t.TicketID} className={t.Status === 'Cancelled' ? 'row-off' : ''}>
                  {canEdit && <td className="chk">{t.Status === 'Booked' && <input type="checkbox" checked={sel.includes(t.TicketID)} onChange={() => toggle(t.TicketID)} aria-label={`Chọn ghế ${seatCode(t.Seat)}`} />}</td>}
                  <td><strong>{seatCode(t.Seat)}</strong></td><td>{t.TicketID}</td><td className="num">{money(t.Price)}</td>
                  <td><Badge map={TICKET_STATUS} value={t.Status} /></td>
                  {canEdit && <td className="actions">{t.Status === 'Booked' && <button className="btn sm" onClick={() => setChange(t)}>Đổi ghế</button>}</td>}
                </tr>
              ))}
            </tbody>
            <tfoot><tr><td colSpan={canEdit ? 3 : 2}>Tổng tiền các vé còn hiệu lực</td><td className="num"><strong>{money(b.Total)}</strong></td><td colSpan={canEdit ? 2 : 1} /></tr></tfoot>
          </table>
        </div>
      </section>

      {b.Payment && (
        <section className="panel">
          <div className="panel-head"><h2>Thanh toán</h2><Link className="btn sm" to="/payments">Mở trang thanh toán</Link></div>
          <dl className="dl">
            <dt>Mã giao dịch</dt><dd>{b.Payment.TransactionNo}</dd><dt>Phương thức</dt><dd>{b.Payment.PaymentMethod}</dd>
            <dt>Số tiền</dt><dd>{money(b.Payment.Amount)}</dd><dt>Trạng thái</dt><dd><Badge map={PAYMENT_STATUS} value={b.Payment.PaymentStatus} /></dd>
            <dt>Thời gian</dt><dd>{fmtDateTime(b.Payment.PaymentDate)}</dd>
          </dl>
          {b.Status === 'Cancelled' && b.Payment.PaymentStatus === 'Success' && <p className="callout inline">Lượt đặt vé đã hủy nhưng chưa hoàn tiền. Vào trang Thanh toán để hoàn tiền.</p>}
        </section>
      )}

      {confirm && (
        <ConfirmDialog danger busy={busy} title="Hủy vé đã chọn?" confirmText={`Hủy ${selTickets.length} vé`} onCancel={() => setConfirm(false)} onConfirm={doCancel}>
          <p>Ghế sẽ hủy: <strong>{selTickets.map((t) => seatCode(t.Seat)).join(', ')}</strong> ({money(selTickets.reduce((s, t) => s + t.Price, 0))}).</p>
          <p className="muted">Các ghế này được giải phóng ngay để khách khác đặt.{cancelAll ? ' Đây là toàn bộ vé còn lại nên cả lượt đặt vé sẽ chuyển sang “Đã hủy”.' : ''}</p>
        </ConfirmDialog>
      )}
      {change && <ChangeSeatModal booking={b} ticket={change} onClose={() => setChange(null)} onDone={() => { setChange(null); reload(); }} />}
    </div>
  );
}
