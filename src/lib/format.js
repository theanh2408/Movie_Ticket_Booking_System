const pad = (n) => String(n).padStart(2, '0');

export const money = (n) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(n || 0);

export const compactMoney = (n) => {
  if (n >= 1e9) return `${(n / 1e9).toFixed(1)} tỷ`;
  if (n >= 1e6) return `${(n / 1e6).toFixed(n >= 1e7 ? 0 : 1)} tr`;
  if (n >= 1e3) return `${Math.round(n / 1e3)}k`;
  return String(n);
};

// Date -> 'YYYY-MM-DD' theo giờ địa phương
export const toISODate = (d) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
export const todayISO = () => toISODate(new Date());
export const addDaysISO = (iso, n) => {
  const d = new Date(`${iso}T00:00:00`);
  d.setDate(d.getDate() + n);
  return toISODate(d);
};

// 'YYYY-MM-DD' hoặc ISO datetime -> 'dd/MM/yyyy'
export const fmtDate = (v) => {
  if (!v) return '—';
  const d = new Date(v.length === 10 ? `${v}T00:00:00` : v);
  return `${pad(d.getDate())}/${pad(d.getMonth() + 1)}/${d.getFullYear()}`;
};
export const fmtTime = (v) => {
  const d = new Date(v);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};
export const fmtDateTime = (v) => (v ? `${fmtTime(v)} ${fmtDate(v)}` : '—');

export const showtimeStart = (st) => new Date(`${st.Date}T${st.Start_hour}:00`);
export const addMinutesToDate = (d, min) => new Date(d.getTime() + min * 60000);
export const hhmm = (d) => `${pad(d.getHours())}:${pad(d.getMinutes())}`;

export const rowLabel = (i) => String.fromCharCode(65 + i); // 0 -> A
export const seatCode = (s) => `${s.Row}${s.Column}`;
export const sortSeats = (a, b) => (a.Row === b.Row ? Number(a.Column) - Number(b.Column) : a.Row.localeCompare(b.Row));

export const initials = (name = '') =>
  name.split(/\s+/).filter(Boolean).slice(-2).map((w) => w[0]).join('').toUpperCase();
