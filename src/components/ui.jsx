import { useEffect, useRef, useState } from 'react';
import { initials } from '../lib/format';

export function Badge({ map, value }) {
  const s = map[value] || { label: value, tone: 'muted' };
  return <span className={`badge tone-${s.tone}`}>{s.label}</span>;
}

export function Spinner({ label = 'Đang tải…' }) {
  return <div className="spinner-row" role="status"><span className="spinner" aria-hidden="true" />{label}</div>;
}

export function EmptyState({ title, children, action }) {
  return (
    <div className="empty">
      <strong>{title}</strong>
      {children && <p>{children}</p>}
      {action}
    </div>
  );
}

export function ErrorBox({ error, onRetry }) {
  if (!error) return null;
  return (
    <div className="errorbox" role="alert">
      <span>{error.message || 'Không tải được dữ liệu.'}</span>
      {onRetry && <button className="btn sm" onClick={onRetry}>Thử lại</button>}
    </div>
  );
}

export function PageHeader({ title, subtitle, children }) {
  return (
    <header className="page-head">
      <div>
        <h1>{title}</h1>
        {subtitle && <p className="muted">{subtitle}</p>}
      </div>
      <div className="page-actions">{children}</div>
    </header>
  );
}

export function Field({ label, error, hint, required, children, className = '' }) {
  return (
    <label className={`field ${error ? 'has-error' : ''} ${className}`}>
      <span className="field-label">{label}{required && <span className="req" aria-hidden="true"> *</span>}</span>
      {children}
      {hint && !error && <span className="hint">{hint}</span>}
      {error && <span className="error-text" role="alert">{error}</span>}
    </label>
  );
}

export function Modal({ title, onClose, children, footer, wide = false }) {
  const ref = useRef(null);
  useEffect(() => {
    const onKey = (e) => { if (e.key === 'Escape') onClose(); };
    document.addEventListener('keydown', onKey);
    const prev = document.activeElement;
    ref.current?.focus();
    document.body.style.overflow = 'hidden';
    return () => {
      document.removeEventListener('keydown', onKey);
      document.body.style.overflow = '';
      prev?.focus?.();
    };
  }, [onClose]);
  return (
    <div className="overlay" onMouseDown={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <div className={`modal ${wide ? 'wide' : ''}`} role="dialog" aria-modal="true" aria-label={title} tabIndex={-1} ref={ref}>
        <div className="modal-head">
          <h2>{title}</h2>
          <button className="icon-btn" onClick={onClose} aria-label="Đóng">×</button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-foot">{footer}</div>}
      </div>
    </div>
  );
}

export function ConfirmDialog({ title, children, confirmText = 'Xác nhận', cancelText = 'Quay lại', danger, busy, onConfirm, onCancel }) {
  return (
    <Modal
      title={title}
      onClose={busy ? () => {} : onCancel}
      footer={(
        <>
          <button className="btn" onClick={onCancel} disabled={busy}>{cancelText}</button>
          <button className={`btn ${danger ? 'danger' : 'primary'}`} onClick={onConfirm} disabled={busy}>
            {busy ? 'Đang xử lý…' : confirmText}
          </button>
        </>
      )}
    >
      {children}
    </Modal>
  );
}

export function Pagination({ page, pageSize, total, onChange }) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  if (total <= pageSize) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(total, page * pageSize);
  return (
    <nav className="pager" aria-label="Phân trang">
      <span className="muted">{from}–{to} / {total}</span>
      <div>
        <button className="btn sm" disabled={page <= 1} onClick={() => onChange(page - 1)}>Trước</button>
        <span className="pager-now">Trang {page}/{pages}</span>
        <button className="btn sm" disabled={page >= pages} onClick={() => onChange(page + 1)}>Sau</button>
      </div>
    </nav>
  );
}

// Poster: dùng ảnh nếu có và tải được, nếu không hiện ô màu theo tên phim
export function Poster({ movie, size = 'sm' }) {
  const [broken, setBroken] = useState(false);
  const hue = [...(movie.Title || '')].reduce((h, c) => (h * 31 + c.charCodeAt(0)) % 360, 7);
  if (movie.PosterURL && !broken) {
    return <img className={`poster ${size}`} src={movie.PosterURL} alt={`Poster ${movie.Title}`} onError={() => setBroken(true)} />;
  }
  return (
    <div className={`poster ${size} ph`} style={{ background: `linear-gradient(160deg, hsl(${hue} 38% 36%), hsl(${(hue + 40) % 360} 42% 22%))` }} aria-hidden="true">
      {initials(movie.Title)}
    </div>
  );
}

// Sơ đồ ghế dùng chung. getCell(row, col) trả về { state, title, disabled } hoặc null (ô trống).
// state: available | booked | held | selected | locked | mine
export function SeatGrid({ rows, cols, getCell, onCellClick, emptyClickable = false }) {
  return (
    <div className="seatmap">
      <div className="screen" aria-hidden="true"><span>Màn hình</span></div>
      <div className="seat-scroll">
        <div className="seat-grid" style={{ gridTemplateColumns: `1.6rem repeat(${cols}, var(--seat))` }}>
          {rows.map((r) => (
            <RowCells key={r} row={r} cols={cols} getCell={getCell} onCellClick={onCellClick} emptyClickable={emptyClickable} />
          ))}
        </div>
      </div>
    </div>
  );
}
function RowCells({ row, cols, getCell, onCellClick, emptyClickable }) {
  const cells = [<span key="l" className="seat-row-label">{row}</span>];
  for (let c = 1; c <= cols; c++) {
    const cell = getCell(row, c);
    const code = `${row}${c}`;
    if (!cell) {
      cells.push(emptyClickable
        ? <button key={c} type="button" className="seat empty" onClick={() => onCellClick?.(row, c, null)} aria-label={`Thêm ghế ${code}`} title={`Thêm ghế ${code}`} />
        : <span key={c} className="seat-gap" />);
    } else {
      cells.push(
        <button key={c} type="button" className={`seat ${cell.state}`} disabled={cell.disabled} onClick={() => onCellClick?.(row, c, cell)}
          title={cell.title || code} aria-label={cell.title || code} aria-pressed={cell.state === 'selected'}>
          {c}
        </button>,
      );
    }
  }
  return cells;
}
export function SeatLegend({ items }) {
  return (
    <ul className="legend">
      {items.map(([state, label]) => <li key={state}><span className={`seat mini ${state}`} aria-hidden="true" />{label}</li>)}
    </ul>
  );
}
