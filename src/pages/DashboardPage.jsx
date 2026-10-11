import { useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api';
import { useAsync } from '../lib/hooks';
import { addDaysISO, compactMoney, fmtDate, money, todayISO } from '../lib/format';
import { ErrorBox, Field, PageHeader, Spinner } from '../components/ui';

const PRESETS = [[7, '7 ngày'], [30, '30 ngày'], [90, '90 ngày']];

function RevenueChart({ data }) {
  const W = 760, H = 240, L = 46, B = 28, T = 10, R = 8;
  const max = Math.max(...data.map((d) => d.revenue), 1);
  const step = Math.pow(10, Math.floor(Math.log10(max)));
  const top = Math.ceil(max / step) * step;
  const bw = (W - L - R) / data.length;
  const ticks = [0, 0.25, 0.5, 0.75, 1].map((p) => p * top);
  const every = Math.ceil(data.length / 8);
  return (
    <div className="chart-scroll">
      <svg className="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Biểu đồ doanh thu theo ngày">
        {ticks.map((t) => {
          const y = T + (H - T - B) * (1 - t / top);
          return (
            <g key={t}>
              <line x1={L} x2={W - R} y1={y} y2={y} className="grid-line" />
              <text x={L - 6} y={y + 4} textAnchor="end" className="axis">{compactMoney(t)}</text>
            </g>
          );
        })}
        {data.map((d, i) => {
          const h = (H - T - B) * (d.revenue / top);
          const x = L + i * bw + bw * 0.15;
          return (
            <g key={d.date}>
              <rect x={x} y={H - B - h} width={Math.max(bw * 0.7, 1)} height={h} rx="2" className="bar">
                <title>{`${fmtDate(d.date)}: ${money(d.revenue)} · ${d.tickets} vé`}</title>
              </rect>
              {i % every === 0 && <text x={x + bw * 0.35} y={H - 8} textAnchor="middle" className="axis">{fmtDate(d.date).slice(0, 5)}</text>}
            </g>
          );
        })}
      </svg>
    </div>
  );
}

export default function DashboardPage() {
  const [range, setRange] = useState({ from: addDaysISO(todayISO(), -29), to: todayISO() });
  const { data, loading, error, reload } = useAsync(() => api.reports.revenue(range), [range.from, range.to]);
  const setPreset = (n) => setRange({ from: addDaysISO(todayISO(), -(n - 1)), to: todayISO() });
  const invalid = range.from > range.to;

  return (
    <div className="page">
      <PageHeader title="Tổng quan doanh thu" subtitle="Tính theo các giao dịch thanh toán thành công trong khoảng thời gian đã chọn.">
        <div className="seg" role="group" aria-label="Khoảng thời gian nhanh">
          {PRESETS.map(([n, label]) => <button key={n} className="btn sm" onClick={() => setPreset(n)}>{label}</button>)}
        </div>
      </PageHeader>

      <div className="toolbar">
        <Field label="Từ ngày"><input className="input" type="date" value={range.from} max={range.to} onChange={(e) => setRange((r) => ({ ...r, from: e.target.value }))} /></Field>
        <Field label="Đến ngày" error={invalid ? 'Ngày kết thúc phải sau ngày bắt đầu.' : ''}>
          <input className="input" type="date" value={range.to} min={range.from} onChange={(e) => setRange((r) => ({ ...r, to: e.target.value }))} />
        </Field>
      </div>

      <ErrorBox error={error} onRetry={reload} />
      {!data && loading && <Spinner />}
      {data && (
        <>
          <section className="stats" aria-label="Chỉ số chính">
            <div className="stat main"><span>Doanh thu</span><strong>{money(data.total)}</strong><small>{fmtDate(data.from)} – {fmtDate(data.to)}</small></div>
            <div className="stat"><span>Hóa đơn đã thanh toán</span><strong>{data.invoices}</strong></div>
            <div className="stat"><span>Vé đã bán</span><strong>{data.tickets}</strong></div>
            <div className="stat"><span>Đã hoàn tiền</span><strong>{money(data.refunded)}</strong></div>
          </section>

          {data.needsRefundCount > 0 && (
            <div className="callout">
              <span><strong>{data.needsRefundCount} giao dịch cần hoàn tiền</strong> ({money(data.needsRefundAmount)}) vì hóa đơn đã bị hủy.</span>
              <Link className="btn sm" to="/payments?needsRefund=1">Xử lý ngay</Link>
            </div>
          )}

          <section className="panel">
            <h2>Doanh thu theo ngày</h2>
            {data.total === 0 ? <p className="muted">Chưa có doanh thu trong khoảng này.</p> : <RevenueChart data={data.byDay} />}
          </section>

          <div className="two-col">
            <section className="panel">
              <h2>Phim doanh thu cao nhất</h2>
              {data.byMovie.length === 0 ? <p className="muted">Chưa có dữ liệu.</p> : (
                <table className="table compact">
                  <thead><tr><th>Phim</th><th className="num">Vé</th><th className="num">Doanh thu</th></tr></thead>
                  <tbody>{data.byMovie.map((m) => <tr key={m.title}><td>{m.title}</td><td className="num">{m.tickets}</td><td className="num">{money(m.revenue)}</td></tr>)}</tbody>
                </table>
              )}
            </section>
            <section className="panel">
              <h2>Theo phương thức thanh toán</h2>
              {data.byMethod.length === 0 ? <p className="muted">Chưa có dữ liệu.</p> : (
                <ul className="bars">
                  {data.byMethod.map((m) => (
                    <li key={m.method}>
                      <div className="bars-top"><span>{m.method}</span><span>{money(m.revenue)}</span></div>
                      <div className="bars-track"><div className="bars-fill" style={{ width: `${(m.revenue / data.total) * 100}%` }} /></div>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        </>
      )}
    </div>
  );
}
