import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../api';
import { useToast } from '../context/ToastContext';
import { useAsync, useDebounced } from '../lib/hooks';
import { PAYMENT_METHODS, PAYMENT_STATUS } from '../lib/constants';
import { fmtDateTime, money } from '../lib/format';
import { Badge, ConfirmDialog, EmptyState, ErrorBox, Field, PageHeader, Pagination, Spinner } from '../components/ui';

const PAGE_SIZE = 10;
const ACTIONS = {
  confirm: { to: 'Success', title: 'Xác nhận đã thanh toán?', btn: 'Xác nhận thanh toán', done: 'Đã xác nhận thanh toán.', text: 'Hóa đơn sẽ chuyển sang “Đã thanh toán”.' },
  fail: { to: 'Failed', title: 'Đánh dấu giao dịch thất bại?', btn: 'Đánh dấu thất bại', done: 'Đã đánh dấu giao dịch thất bại.', text: 'Giao dịch sẽ không được tính vào doanh thu.', danger: true },
  refund: { to: 'Refunded', title: 'Hoàn tiền cho khách?', btn: 'Đã hoàn tiền', done: 'Đã ghi nhận hoàn tiền.', text: 'Hãy chỉ xác nhận sau khi đã hoàn tiền cho khách qua cổng thanh toán.' },
};

export default function PaymentsPage() {
  const toast = useToast();
  const [params, setParams] = useSearchParams();
  const needsRefund = params.get('needsRefund') === '1';
  const [flt, setFlt] = useState({ search: '', status: '', method: '' });
  const [page, setPage] = useState(1);
  const q = useDebounced(flt.search);
  const set = (k) => (e) => { setFlt((x) => ({ ...x, [k]: e.target.value })); setPage(1); };
  const { data, loading, error, reload } = useAsync(
    () => api.payments.list({ ...flt, search: q, needsRefund, page, pageSize: PAGE_SIZE }),
    [q, flt.status, flt.method, needsRefund, page],
  );
  const [act, setAct] = useState(null); // { payment, key }
  const [busy, setBusy] = useState(false);
  const toggleRefund = () => { const n = new URLSearchParams(params); if (needsRefund) n.delete('needsRefund'); else n.set('needsRefund', '1'); setParams(n); setPage(1); };

  const run = async () => {
    setBusy(true);
    try { await api.payments.updateStatus(act.payment.PaymentID, ACTIONS[act.key].to); toast.success(ACTIONS[act.key].done); setAct(null); reload(); }
    catch (err) { toast.error(err.message); setAct(null); reload(); } finally { setBusy(false); }
  };

  return (
    <div className="page">
      <PageHeader title="Thanh toán" subtitle="Theo dõi giao dịch, xác nhận thanh toán đang chờ và hoàn tiền cho vé đã hủy." />
      <div className="toolbar">
        <Field label="Tìm kiếm"><input className="input" placeholder="Mã giao dịch, mã đặt vé, tên khách" value={flt.search} onChange={set('search')} /></Field>
        <Field label="Trạng thái"><select className="input" value={flt.status} onChange={set('status')}><option value="">Tất cả</option>{Object.entries(PAYMENT_STATUS).map(([v, s]) => <option key={v} value={v}>{s.label}</option>)}</select></Field>
        <Field label="Phương thức"><select className="input" value={flt.method} onChange={set('method')}><option value="">Tất cả</option>{PAYMENT_METHODS.map((m) => <option key={m}>{m}</option>)}</select></Field>
        <button className={`btn ${needsRefund ? 'primary' : ''}`} onClick={toggleRefund} aria-pressed={needsRefund}>
          Cần hoàn tiền{data ? ` (${data.needsRefundCount})` : ''}
        </button>
      </div>

      <ErrorBox error={error} onRetry={reload} />
      {!data && loading && <Spinner />}
      {data && data.total === 0 && <EmptyState title="Không có giao dịch phù hợp">{needsRefund ? 'Hiện không có giao dịch nào cần hoàn tiền.' : 'Thử đổi từ khóa hoặc bộ lọc.'}</EmptyState>}
      {data && data.total > 0 && (
        <>
          <div className={`table-wrap ${loading ? 'dim' : ''}`}>
            <table className="table">
              <thead><tr><th>Mã giao dịch</th><th>Đặt vé</th><th>Khách hàng</th><th>Phương thức</th><th className="num">Số tiền</th><th>Trạng thái</th><th>Thời gian</th><th><span className="sr">Thao tác</span></th></tr></thead>
              <tbody>
                {data.items.map((p) => (
                  <tr key={p.PaymentID}>
                    <td>{p.TransactionNo}<br /><small className="muted">{p.PaymentID}</small></td>
                    <td><Link className="strong-link" to={`/bookings/${p.InvoiceID}`}>{p.InvoiceID}</Link><br /><small className="muted">{p.MovieTitle}</small></td>
                    <td>{p.CustomerName}</td>
                    <td>{p.PaymentMethod}</td>
                    <td className="num">{money(p.Amount)}</td>
                    <td><Badge map={PAYMENT_STATUS} value={p.PaymentStatus} />{p.NeedsRefund && <><br /><small className="warn-text">Hóa đơn đã hủy</small></>}</td>
                    <td>{fmtDateTime(p.PaymentDate)}</td>
                    <td className="actions">
                      {p.PaymentStatus === 'Pending' && <><button className="btn sm" onClick={() => setAct({ payment: p, key: 'confirm' })}>Xác nhận</button><button className="btn sm danger-ghost" onClick={() => setAct({ payment: p, key: 'fail' })}>Thất bại</button></>}
                      {p.NeedsRefund && <button className="btn sm primary" onClick={() => setAct({ payment: p, key: 'refund' })}>Hoàn tiền</button>}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination page={page} pageSize={PAGE_SIZE} total={data.total} onChange={setPage} />
        </>
      )}

      {act && (
        <ConfirmDialog danger={ACTIONS[act.key].danger} busy={busy} title={ACTIONS[act.key].title} confirmText={ACTIONS[act.key].btn} onCancel={() => setAct(null)} onConfirm={run}>
          <p>Giao dịch <strong>{act.payment.TransactionNo}</strong> · {money(act.payment.Amount)} · {act.payment.CustomerName}</p>
          <p className="muted">{ACTIONS[act.key].text}</p>
        </ConfirmDialog>
      )}
    </div>
  );
}
