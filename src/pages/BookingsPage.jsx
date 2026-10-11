import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { useAsync, useDebounced } from '../lib/hooks';
import { INVOICE_STATUS } from '../lib/constants';
import { fmtDate, fmtDateTime, money, seatCode } from '../lib/format';
import { Badge, EmptyState, ErrorBox, Field, PageHeader, Pagination, Spinner } from '../components/ui';

const PAGE_SIZE = 10;

export default function BookingsPage() {
  const [flt, setFlt] = useState({ search: '', movieId: '', from: '', to: '', status: '' });
  const [page, setPage] = useState(1);
  const q = useDebounced(flt.search);
  const set = (k) => (e) => { setFlt((x) => ({ ...x, [k]: e.target.value })); setPage(1); };
  const { data: movies } = useAsync(() => api.movies.list({}), []);
  const { data, loading, error, reload } = useAsync(
    () => api.bookings.list({ ...flt, search: q, page, pageSize: PAGE_SIZE }),
    [q, flt.movieId, flt.from, flt.to, flt.status, page],
  );
  const clear = () => { setFlt({ search: '', movieId: '', from: '', to: '', status: '' }); setPage(1); };
  const filtered = Object.values(flt).some(Boolean);

  return (
    <div className="page">
      <PageHeader title="Đặt vé" subtitle="Tra cứu lượt đặt vé của khách hàng, hủy vé hoặc đổi ghế khi cần." />
      <div className="toolbar">
        <Field label="Tìm kiếm"><input className="input" placeholder="Mã đặt vé, tên, SĐT, email" value={flt.search} onChange={set('search')} /></Field>
        <Field label="Phim"><select className="input" value={flt.movieId} onChange={set('movieId')}><option value="">Tất cả</option>{(movies || []).map((m) => <option key={m.MovieID} value={m.MovieID}>{m.Title}</option>)}</select></Field>
        <Field label="Chiếu từ ngày"><input className="input" type="date" value={flt.from} onChange={set('from')} /></Field>
        <Field label="Đến ngày"><input className="input" type="date" value={flt.to} min={flt.from} onChange={set('to')} /></Field>
        <Field label="Trạng thái"><select className="input" value={flt.status} onChange={set('status')}><option value="">Tất cả</option>{Object.entries(INVOICE_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}</select></Field>
        {filtered && <button className="btn" onClick={clear}>Xóa bộ lọc</button>}
      </div>

      <ErrorBox error={error} onRetry={reload} />
      {!data && loading && <Spinner />}
      {data && data.total === 0 && <EmptyState title="Không tìm thấy lượt đặt vé nào">Thử đổi từ khóa hoặc xóa bớt bộ lọc.</EmptyState>}
      {data && data.total > 0 && (
        <>
          <div className={`table-wrap ${loading ? 'dim' : ''}`}>
            <table className="table">
              <thead><tr><th>Mã đặt vé</th><th>Khách hàng</th><th>Phim / suất chiếu</th><th>Ghế</th><th className="num">Tổng tiền</th><th>Trạng thái</th><th>Đặt lúc</th></tr></thead>
              <tbody>
                {data.items.map((b) => (
                  <tr key={b.InvoiceID}>
                    <td><Link to={`/bookings/${b.InvoiceID}`} className="strong-link">{b.InvoiceID}</Link></td>
                    <td>{b.Customer.FullName}<br /><small className="muted">{b.Customer.Phone}</small></td>
                    <td>{b.Showtime?.MovieTitle}<br /><small className="muted">{fmtDate(b.Showtime?.Date)} · {b.Showtime?.Start_hour} · {b.Showtime?.RoomName}</small></td>
                    <td>{b.Tickets.filter((t) => t.Status === 'Booked' || b.Status === 'Cancelled').map((t) => seatCode(t.Seat)).join(', ')}</td>
                    <td className="num">{money(b.Status === 'Cancelled' ? b.Tickets.reduce((s, t) => s + t.Price, 0) : b.Total)}</td>
                    <td><Badge map={INVOICE_STATUS} value={b.Status} /></td>
                    <td>{fmtDateTime(b.CreateDate)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} pageSize={PAGE_SIZE} total={data.total} onChange={setPage} />
        </>
      )}
    </div>
  );
}
