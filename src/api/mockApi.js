// API giả chạy trong trình duyệt. Mỗi hàm có cùng tên/tham số với realApi trong index.js,
// và mô phỏng các quy tắc nghiệp vụ trong SRS (BR02, BR06–BR09...) để giao diện xử lý lỗi đúng.
import { db } from './mockDb';
import { ApiError } from '../lib/errors';
import { getToken } from '../lib/token';
import { decodeJwt, isExpired, signMockJwt } from '../lib/jwt';
import { handleUnauthorized } from '../lib/http';
import { addDaysISO, addMinutesToDate, hhmm, showtimeStart, sortSeats, todayISO } from '../lib/format';
import { AGE_RATINGS, CLEANING_MINUTES } from '../lib/constants';

const clone = (v) => JSON.parse(JSON.stringify(v));
const call = (fn, { auth = true } = {}) =>
  new Promise((resolve, reject) => {
    setTimeout(() => {
      try {
        if (auth && isExpired(decodeJwt(getToken() || ''))) {
          handleUnauthorized();
          throw new ApiError('Phiên đăng nhập đã hết hạn.', { status: 401, code: 'UNAUTHORIZED' });
        }
        resolve(clone(fn()));
      } catch (e) { reject(e); }
    }, 180 + Math.random() * 160);
  });
const fail = (fields) => { throw new ApiError('Dữ liệu chưa hợp lệ. Hãy kiểm tra các ô được đánh dấu.', { code: 'VALIDATION', fields }); };

// ---------- helpers ----------
const movieOf = (id) => db.movies.find((m) => m.MovieID === id);
const roomOf = (id) => db.rooms.find((r) => r.RoomID === id);
const userOf = (id) => db.accounts.find((a) => a.UserID === id);
const ssById = new Map(db.showtimeSeats.map((x) => [x.Showtime_SeatID, x]));
const seatById = new Map(db.seats.map((x) => [x.SeatID, x]));
const stById = (id) => db.showtimes.find((s) => s.ShowtimeID === id);
const endOf = (st) => addMinutesToDate(showtimeStart(st), movieOf(st.MovieID).Duration);
const hasStarted = (st) => showtimeStart(st) <= new Date();
const activeTicketsOfShowtime = (showtimeId) =>
  db.tickets.filter((t) => t.Status === 'Booked' && ssById.get(t.Showtime_SeatID)?.ShowtimeID === showtimeId);
const showtimeOfInvoice = (invoiceId) => {
  const t = db.tickets.find((x) => x.InvoiceID === invoiceId);
  return t ? stById(ssById.get(t.Showtime_SeatID).ShowtimeID) : null;
};

function findConflicts({ RoomID, Date: date, Start_hour, MovieID }, excludeId) {
  const movie = movieOf(MovieID);
  if (!movie || !RoomID || !date || !Start_hour) return [];
  const s = new Date(`${date}T${Start_hour}:00`);
  const e = addMinutesToDate(s, movie.Duration + CLEANING_MINUTES);
  return db.showtimes
    .filter((o) => o.RoomID === RoomID && o.Status === 'Scheduled' && o.ShowtimeID !== excludeId)
    .filter((o) => {
      const os = showtimeStart(o);
      const oe = addMinutesToDate(os, movieOf(o.MovieID).Duration + CLEANING_MINUTES);
      return s < oe && os < e;
    })
    .map((o) => ({ ShowtimeID: o.ShowtimeID, MovieTitle: movieOf(o.MovieID).Title, Date: o.Date, Start_hour: o.Start_hour, EndHour: hhmm(endOf(o)) }));
}

function joinShowtime(st, counts) {
  const m = movieOf(st.MovieID);
  return {
    ...st, MovieTitle: m.Title, Duration: m.Duration, RoomName: roomOf(st.RoomID).RoomName,
    EndHour: hhmm(endOf(st)), Booked: counts.booked[st.ShowtimeID] || 0, Total: counts.total[st.ShowtimeID] || 0,
  };
}
function showtimeCounts() {
  const booked = {}; const total = {};
  db.showtimeSeats.forEach((x) => {
    total[x.ShowtimeID] = (total[x.ShowtimeID] || 0) + 1;
    if (x.Status === 'Booked') booked[x.ShowtimeID] = (booked[x.ShowtimeID] || 0) + 1;
  });
  return { booked, total };
}

function joinInvoice(inv) {
  const tickets = db.tickets.filter((t) => t.InvoiceID === inv.InvoiceID);
  const st = showtimeOfInvoice(inv.InvoiceID);
  const payment = db.payments.find((p) => p.InvoiceID === inv.InvoiceID) || null;
  const user = userOf(inv.UserID);
  const live = tickets.filter((t) => t.Status === 'Booked');
  return {
    ...inv,
    Customer: { UserID: user.UserID, FullName: user.FullName, Phone: user.Phone, Email: user.Email },
    Showtime: st && { ...joinShowtime(st, { booked: {}, total: {} }), Booked: undefined, Total: undefined },
    MovieID: st?.MovieID,
    Tickets: tickets.map((t) => {
      const ss = ssById.get(t.Showtime_SeatID);
      return { ...t, Seat: seatById.get(ss.SeatID), ShowtimeSeatID: ss.Showtime_SeatID };
    }).sort((a, b) => sortSeats(a.Seat, b.Seat)),
    Total: live.reduce((s, t) => s + t.Price, 0),
    Payment: payment,
  };
}

// ---------- auth ----------
const auth = {
  login: ({ email, password }) => call(() => {
    const acc = db.accounts.find((a) => a.Email.toLowerCase() === String(email || '').trim().toLowerCase());
    if (!acc || acc._pw !== password) throw new ApiError('Email hoặc mật khẩu không đúng.', { status: 401, code: 'BAD_CREDENTIALS' });
    if (acc.Role !== 'Admin') throw new ApiError('Tài khoản này không có quyền vào trang quản trị.', { status: 403, code: 'FORBIDDEN' });
    return { token: signMockJwt({ sub: acc.UserID, name: acc.FullName, email: acc.Email, role: acc.Role }, 60 * 60) };
  }, { auth: false }),
};

// ---------- movies ----------
const movieView = (m) => ({ ...m, GenreIDs: db.movieGenres.filter((x) => x.MovieID === m.MovieID).map((x) => x.Genre_ID) });
function validateMovie(p) {
  const f = {};
  if (!p.Title?.trim()) f.Title = 'Nhập tên phim.';
  const d = Number(p.Duration);
  if (!Number.isInteger(d) || d < 1 || d > 400) f.Duration = 'Thời lượng là số phút từ 1 đến 400.';
  if (!p.ReleaseDate) f.ReleaseDate = 'Chọn ngày khởi chiếu.';
  if (!AGE_RATINGS.some((a) => a.value === p.AgeRating)) f.AgeRating = 'Chọn độ tuổi.';
  if (!['Showing', 'ComingSoon', 'Hidden'].includes(p.Status)) f.Status = 'Chọn trạng thái.';
  if (p.PosterURL && !/^https?:\/\//i.test(p.PosterURL)) f.PosterURL = 'Đường dẫn ảnh phải bắt đầu bằng http:// hoặc https://.';
  if (p.TrailerURL && !/^https?:\/\//i.test(p.TrailerURL)) f.TrailerURL = 'Đường dẫn trailer phải bắt đầu bằng http:// hoặc https://.';
  if (!p.GenreIDs?.length) f.GenreIDs = 'Chọn ít nhất một thể loại.';
  if (Object.keys(f).length) fail(f);
}
const upcomingShowtimes = (movieId) => db.showtimes.filter((s) => s.MovieID === movieId && s.Status === 'Scheduled' && !hasStarted(s));
function saveGenres(id, ids) {
  db.movieGenres = db.movieGenres.filter((x) => x.MovieID !== id);
  ids.forEach((g) => db.movieGenres.push({ MovieID: id, Genre_ID: g }));
}
const movies = {
  list: ({ search = '', status = '' } = {}) => call(() =>
    db.movies
      .filter((m) => (!status || m.Status === status) && m.Title.toLowerCase().includes(search.trim().toLowerCase()))
      .sort((a, b) => b.ReleaseDate.localeCompare(a.ReleaseDate))
      .map(movieView)),
  get: (id) => call(() => {
    const m = movieOf(id);
    if (!m) throw new ApiError('Không tìm thấy phim.', { status: 404 });
    return { ...movieView(m), UpcomingShowtimes: upcomingShowtimes(id).length };
  }),
  create: (p) => call(() => {
    validateMovie(p);
    const id = `M${String(db.movies.length + 1).padStart(3, '0')}`;
    const m = { MovieID: id, Title: p.Title.trim(), Description: p.Description || '', Duration: Number(p.Duration), PosterURL: p.PosterURL || '', TrailerURL: p.TrailerURL || '', AgeRating: p.AgeRating, ReleaseDate: p.ReleaseDate, Status: p.Status };
    db.movies.push(m); saveGenres(id, p.GenreIDs);
    return movieView(m);
  }),
  update: (id, p) => call(() => {
    const m = movieOf(id);
    if (!m) throw new ApiError('Không tìm thấy phim.', { status: 404 });
    validateMovie(p);
    const up = upcomingShowtimes(id);
    if (Number(p.Duration) !== m.Duration && up.length) {
      throw new ApiError(`Không đổi được thời lượng vì phim còn ${up.length} suất chiếu sắp tới. Hãy hủy hoặc dời các suất đó trước.`, { code: 'LOCKED' });
    }
    if (p.Status === 'Hidden' && m.Status !== 'Hidden') assertCanHide(id);
    Object.assign(m, { Title: p.Title.trim(), Description: p.Description || '', Duration: Number(p.Duration), PosterURL: p.PosterURL || '', TrailerURL: p.TrailerURL || '', AgeRating: p.AgeRating, ReleaseDate: p.ReleaseDate, Status: p.Status });
    saveGenres(id, p.GenreIDs);
    return movieView(m);
  }),
  setStatus: (id, status) => call(() => {
    const m = movieOf(id);
    if (!m) throw new ApiError('Không tìm thấy phim.', { status: 404 });
    if (status === 'Hidden') assertCanHide(id);
    m.Status = status;
    return movieView(m);
  }),
};
function assertCanHide(id) {
  const withBookings = upcomingShowtimes(id).filter((s) => activeTicketsOfShowtime(s.ShowtimeID).length);
  if (withBookings.length) {
    throw new ApiError(`Phim còn ${withBookings.length} suất chiếu sắp tới đã có người đặt vé. Hãy hủy vé hoặc chờ các suất này chiếu xong rồi mới ngừng hiển thị.`, { code: 'LOCKED' });
  }
}
const genres = { list: () => call(() => db.genres) };

// ---------- rooms & seats ----------
const lockedSeatIds = (roomId) => {
  const ids = new Set();
  db.tickets.forEach((t) => {
    if (t.Status !== 'Booked') return;
    const ss = ssById.get(t.Showtime_SeatID);
    const seat = seatById.get(ss.SeatID);
    if (seat.RoomID === roomId) ids.add(seat.SeatID);
  });
  return ids;
};
const roomsApi = {
  cinema: () => call(() => db.cinema),
  list: () => call(() => db.rooms.map((r) => ({
    ...r, SeatCount: db.seats.filter((s) => s.RoomID === r.RoomID).length,
    UpcomingShowtimes: db.showtimes.filter((s) => s.RoomID === r.RoomID && s.Status === 'Scheduled' && !hasStarted(s)).length,
  }))),
  get: (id) => call(() => {
    const r = roomOf(id);
    if (!r) throw new ApiError('Không tìm thấy phòng chiếu.', { status: 404 });
    const locked = lockedSeatIds(id);
    return { ...r, Seats: db.seats.filter((s) => s.RoomID === id).map((s) => ({ ...s, Locked: locked.has(s.SeatID) })).sort(sortSeats) };
  }),
  create: (p) => call(() => {
    validateRoom(p);
    const r = { RoomID: `R${String(db.rooms.length + 1).padStart(2, '0')}`, RoomName: p.RoomName.trim(), Status: p.Status, CinemaID: db.cinema.CinemaID };
    db.rooms.push(r);
    return r;
  }),
  update: (id, p) => call(() => {
    const r = roomOf(id);
    validateRoom(p, id);
    if (p.Status === 'Maintenance' && r.Status !== 'Maintenance') {
      const busy = db.showtimes.filter((s) => s.RoomID === id && s.Status === 'Scheduled' && !hasStarted(s) && activeTicketsOfShowtime(s.ShowtimeID).length);
      if (busy.length) throw new ApiError(`Phòng còn ${busy.length} suất chiếu sắp tới đã có người đặt vé nên chưa thể chuyển sang bảo trì.`, { code: 'LOCKED' });
    }
    r.RoomName = p.RoomName.trim(); r.Status = p.Status;
    return r;
  }),
  // seats: [{ Row: 'A', Column: '1' }, ...] – toàn bộ sơ đồ ghế mới của phòng
  saveSeats: (id, seats) => call(() => {
    if (!roomOf(id)) throw new ApiError('Không tìm thấy phòng chiếu.', { status: 404 });
    if (!seats.length) throw new ApiError('Phòng cần có ít nhất một ghế.', { code: 'VALIDATION' });
    const keys = new Set(seats.map((s) => `${s.Row}${s.Column}`));
    if (keys.size !== seats.length) throw new ApiError('Mã ghế bị trùng trong cùng phòng.', { code: 'VALIDATION' });
    const locked = lockedSeatIds(id);
    const removedLocked = db.seats.filter((s) => s.RoomID === id && locked.has(s.SeatID) && !keys.has(`${s.Row}${s.Column}`));
    if (removedLocked.length) {
      throw new ApiError(`Không thể bỏ ghế ${removedLocked.map((s) => s.Row + s.Column).join(', ')} vì đã có vé đặt.`, { code: 'LOCKED', data: { seats: removedLocked.map((s) => s.Row + s.Column) } });
    }
    const existing = db.seats.filter((s) => s.RoomID === id);
    const existingKeys = new Map(existing.map((s) => [`${s.Row}${s.Column}`, s]));
    const upcoming = db.showtimes.filter((s) => s.RoomID === id && s.Status === 'Scheduled' && !hasStarted(s));
    // ghế bị bỏ: xóa luôn Showtime_Seat còn trống
    existing.filter((s) => !keys.has(`${s.Row}${s.Column}`)).forEach((s) => {
      db.showtimeSeats = db.showtimeSeats.filter((x) => !(x.SeatID === s.SeatID && x.Status !== 'Booked'));
    });
    db.seats = db.seats.filter((s) => s.RoomID !== id || keys.has(`${s.Row}${s.Column}`));
    seats.forEach((s) => {
      if (existingKeys.has(`${s.Row}${s.Column}`)) return;
      const seat = { SeatID: `${id}-${s.Row}${s.Column}`, Row: s.Row, Column: String(s.Column), RoomID: id };
      db.seats.push(seat); seatById.set(seat.SeatID, seat);
      upcoming.forEach((st) => {
        const ss = { Showtime_SeatID: `SSN${Math.random().toString(36).slice(2, 9)}`, Status: 'Available', Price: st.BasePrice, ShowtimeID: st.ShowtimeID, SeatID: seat.SeatID };
        db.showtimeSeats.push(ss); ssById.set(ss.Showtime_SeatID, ss);
      });
    });
    return { ok: true };
  }),
};
function validateRoom(p, id) {
  const f = {};
  if (!p.RoomName?.trim()) f.RoomName = 'Nhập tên phòng.';
  else if (db.rooms.some((r) => r.RoomID !== id && r.RoomName.toLowerCase() === p.RoomName.trim().toLowerCase())) f.RoomName = 'Tên phòng đã tồn tại.';
  if (!['Active', 'Maintenance'].includes(p.Status)) f.Status = 'Chọn trạng thái.';
  if (Object.keys(f).length) fail(f);
}

// ---------- showtimes ----------
function validateShowtime(p, excludeId) {
  const f = {};
  const movie = movieOf(p.MovieID);
  const room = roomOf(p.RoomID);
  if (!movie) f.MovieID = 'Chọn phim.'; else if (movie.Status === 'Hidden') f.MovieID = 'Phim đã ngừng hiển thị.';
  if (!room) f.RoomID = 'Chọn phòng chiếu.'; else if (room.Status !== 'Active') f.RoomID = 'Phòng đang bảo trì.';
  if (!p.Date) f.Date = 'Chọn ngày chiếu.';
  if (!p.Start_hour) f.Start_hour = 'Chọn giờ bắt đầu.';
  const price = Number(p.BasePrice);
  if (!Number.isFinite(price) || price < 10000) f.BasePrice = 'Giá vé tối thiểu 10.000đ.';
  if (Object.keys(f).length) fail(f);
  const conflicts = findConflicts(p, excludeId);
  if (conflicts.length) throw new ApiError('Suất chiếu bị trùng giờ với suất khác trong cùng phòng.', { code: 'CONFLICT', data: { conflicts } });
}
const showtimes = {
  list: ({ from, to, roomId, movieId, status } = {}) => call(() => {
    const counts = showtimeCounts();
    const now = new Date();
    return db.showtimes
      .filter((s) => (!from || s.Date >= from) && (!to || s.Date <= to) && (!roomId || s.RoomID === roomId) && (!movieId || s.MovieID === movieId))
      .map((s) => joinShowtime(s, counts))
      .filter((s) => {
        if (!status) return true;
        if (s.Status === 'Cancelled') return status === 'Cancelled';
        const st = showtimeStart(s); const en = addMinutesToDate(st, s.Duration);
        const state = now < st ? 'Upcoming' : now < en ? 'Playing' : 'Ended';
        return state === status;
      })
      .sort((a, b) => (a.Date + a.Start_hour).localeCompare(b.Date + b.Start_hour));
  }),
  conflicts: (p, excludeId) => call(() => findConflicts(p, excludeId)),
  seatMap: (id) => call(() => {
    const st = stById(id);
    if (!st) throw new ApiError('Không tìm thấy suất chiếu.', { status: 404 });
    return db.showtimeSeats.filter((x) => x.ShowtimeID === id).map((x) => ({ ...x, Seat: seatById.get(x.SeatID) })).sort((a, b) => sortSeats(a.Seat, b.Seat));
  }),
  create: (p) => call(() => {
    validateShowtime(p);
    if (new Date(`${p.Date}T${p.Start_hour}:00`) <= new Date()) fail({ Date: 'Thời gian chiếu phải ở tương lai.' });
    const st = { ShowtimeID: `ST${String(db.showtimes.length + 1).padStart(4, '0')}`, Start_hour: p.Start_hour, Date: p.Date, BasePrice: Number(p.BasePrice), Status: 'Scheduled', MovieID: p.MovieID, RoomID: p.RoomID };
    db.showtimes.push(st);
    db.seats.filter((s) => s.RoomID === p.RoomID).forEach((seat) => {
      const ss = { Showtime_SeatID: `SSN${Math.random().toString(36).slice(2, 9)}`, Status: 'Available', Price: st.BasePrice, ShowtimeID: st.ShowtimeID, SeatID: seat.SeatID };
      db.showtimeSeats.push(ss); ssById.set(ss.Showtime_SeatID, ss);
    });
    return st;
  }),
  update: (id, p) => call(() => {
    const st = stById(id);
    if (!st) throw new ApiError('Không tìm thấy suất chiếu.', { status: 404 });
    if (st.Status === 'Cancelled') throw new ApiError('Suất chiếu đã hủy, không thể chỉnh sửa.', { code: 'LOCKED' });
    if (hasStarted(st)) throw new ApiError('Suất chiếu đã bắt đầu hoặc đã chiếu xong, không thể chỉnh sửa.', { code: 'LOCKED' });
    const booked = activeTicketsOfShowtime(id).length;
    const changedCore = ['MovieID', 'RoomID', 'Date', 'Start_hour'].some((k) => p[k] !== st[k]);
    if (booked && changedCore) throw new ApiError(`Suất chiếu đã có ${booked} vé đặt nên chỉ được đổi giá vé. Muốn đổi phim, phòng hoặc giờ, hãy hủy vé trước.`, { code: 'LOCKED' });
    validateShowtime(p, id);
    if (changedCore && new Date(`${p.Date}T${p.Start_hour}:00`) <= new Date()) fail({ Date: 'Thời gian chiếu phải ở tương lai.' });
    const roomChanged = p.RoomID !== st.RoomID;
    Object.assign(st, { MovieID: p.MovieID, RoomID: p.RoomID, Date: p.Date, Start_hour: p.Start_hour, BasePrice: Number(p.BasePrice) });
    db.showtimeSeats.filter((x) => x.ShowtimeID === id && x.Status === 'Available').forEach((x) => { x.Price = st.BasePrice; });
    if (roomChanged) {
      db.showtimeSeats = db.showtimeSeats.filter((x) => x.ShowtimeID !== id);
      db.seats.filter((s) => s.RoomID === p.RoomID).forEach((seat) => {
        const ss = { Showtime_SeatID: `SSN${Math.random().toString(36).slice(2, 9)}`, Status: 'Available', Price: st.BasePrice, ShowtimeID: id, SeatID: seat.SeatID };
        db.showtimeSeats.push(ss); ssById.set(ss.Showtime_SeatID, ss);
      });
    }
    return st;
  }),
  cancel: (id) => call(() => {
    const st = stById(id);
    if (!st) throw new ApiError('Không tìm thấy suất chiếu.', { status: 404 });
    if (hasStarted(st)) throw new ApiError('Suất chiếu đã bắt đầu, không thể hủy.', { code: 'LOCKED' });
    const booked = activeTicketsOfShowtime(id).length;
    if (booked) throw new ApiError(`Suất chiếu đã có ${booked} vé đặt. Hãy hủy các vé trong mục Đặt vé trước khi hủy suất.`, { code: 'LOCKED' });
    st.Status = 'Cancelled';
    return st;
  }),
};

// ---------- bookings ----------
const bookings = {
  list: ({ search = '', movieId = '', from = '', to = '', status = '', page = 1, pageSize = 10 } = {}) => call(() => {
    const q = search.trim().toLowerCase();
    let items = db.invoices.map(joinInvoice).filter((inv) => {
      if (status && inv.Status !== status) return false;
      if (movieId && inv.MovieID !== movieId) return false;
      if (from && (!inv.Showtime || inv.Showtime.Date < from)) return false;
      if (to && (!inv.Showtime || inv.Showtime.Date > to)) return false;
      if (q) {
        const hay = `${inv.InvoiceID} ${inv.Customer.FullName} ${inv.Customer.Phone} ${inv.Customer.Email}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    }).sort((a, b) => b.CreateDate.localeCompare(a.CreateDate));
    const total = items.length;
    items = items.slice((page - 1) * pageSize, page * pageSize);
    return { total, items };
  }),
  get: (id) => call(() => {
    const inv = db.invoices.find((i) => i.InvoiceID === id);
    if (!inv) throw new ApiError('Không tìm thấy lượt đặt vé.', { status: 404 });
    return joinInvoice(inv);
  }),
  cancelTickets: (invoiceId, ticketIds) => call(() => {
    const inv = db.invoices.find((i) => i.InvoiceID === invoiceId);
    if (!inv) throw new ApiError('Lượt đặt vé không còn tồn tại.', { status: 404 });
    if (inv.Status === 'Cancelled') throw new ApiError('Lượt đặt vé này đã bị hủy trước đó.', { code: 'LOCKED' });
    const st = showtimeOfInvoice(invoiceId);
    if (hasStarted(st)) throw new ApiError('Suất chiếu đã bắt đầu nên không thể hủy vé.', { code: 'LOCKED' });
    const targets = db.tickets.filter((t) => t.InvoiceID === invoiceId && t.Status === 'Booked' && ticketIds.includes(t.TicketID));
    if (!targets.length) throw new ApiError('Chưa chọn vé hợp lệ để hủy.', { code: 'VALIDATION' });
    targets.forEach((t) => { t.Status = 'Cancelled'; ssById.get(t.Showtime_SeatID).Status = 'Available'; });
    const remaining = db.tickets.filter((t) => t.InvoiceID === invoiceId && t.Status === 'Booked');
    const pay = db.payments.find((p) => p.InvoiceID === invoiceId);
    if (!remaining.length) inv.Status = 'Cancelled';
    else if (pay && pay.PaymentStatus === 'Success') pay.Amount = remaining.reduce((s, t) => s + t.Price, 0);
    return joinInvoice(inv);
  }),
  changeSeat: (invoiceId, ticketId, showtimeSeatId) => call(() => {
    const inv = db.invoices.find((i) => i.InvoiceID === invoiceId);
    const ticket = db.tickets.find((t) => t.TicketID === ticketId && t.InvoiceID === invoiceId);
    if (!inv || !ticket) throw new ApiError('Không tìm thấy vé.', { status: 404 });
    if (inv.Status === 'Cancelled' || ticket.Status !== 'Booked') throw new ApiError('Vé này đã bị hủy nên không thể đổi ghế.', { code: 'LOCKED' });
    const st = showtimeOfInvoice(invoiceId);
    if (hasStarted(st)) throw new ApiError('Suất chiếu đã bắt đầu nên không thể đổi ghế.', { code: 'LOCKED' });
    const target = ssById.get(showtimeSeatId);
    if (!target || target.ShowtimeID !== st.ShowtimeID) throw new ApiError('Ghế mới không thuộc suất chiếu này.', { code: 'VALIDATION' });
    if (target.Status !== 'Available') throw new ApiError('Ghế này vừa có người đặt hoặc đang được giữ. Hãy chọn ghế khác.', { code: 'SEAT_TAKEN' });
    ssById.get(ticket.Showtime_SeatID).Status = 'Available';
    target.Status = 'Booked';
    ticket.Showtime_SeatID = target.Showtime_SeatID;
    return joinInvoice(inv);
  }),
};

// ---------- payments ----------
const joinPayment = (p) => {
  const inv = db.invoices.find((i) => i.InvoiceID === p.InvoiceID);
  const st = showtimeOfInvoice(p.InvoiceID);
  return {
    ...p, InvoiceStatus: inv.Status, CustomerName: userOf(inv.UserID).FullName,
    MovieTitle: st ? movieOf(st.MovieID).Title : '',
    NeedsRefund: p.PaymentStatus === 'Success' && inv.Status === 'Cancelled',
  };
};
const payments = {
  list: ({ search = '', status = '', method = '', needsRefund = false, page = 1, pageSize = 10 } = {}) => call(() => {
    const q = search.trim().toLowerCase();
    let items = db.payments.map(joinPayment).filter((p) =>
      (!status || p.PaymentStatus === status) && (!method || p.PaymentMethod === method) && (!needsRefund || p.NeedsRefund) &&
      (!q || `${p.PaymentID} ${p.TransactionNo} ${p.InvoiceID} ${p.CustomerName}`.toLowerCase().includes(q)),
    ).sort((a, b) => b.PaymentDate.localeCompare(a.PaymentDate));
    const total = items.length;
    items = items.slice((page - 1) * pageSize, page * pageSize);
    return { total, items, needsRefundCount: db.payments.map(joinPayment).filter((p) => p.NeedsRefund).length };
  }),
  // Chuyển trạng thái hợp lệ: Pending → Success | Failed ; Success → Refunded (chỉ khi hóa đơn đã hủy)
  updateStatus: (id, status) => call(() => {
    const p = db.payments.find((x) => x.PaymentID === id);
    if (!p) throw new ApiError('Không tìm thấy giao dịch.', { status: 404 });
    const inv = db.invoices.find((i) => i.InvoiceID === p.InvoiceID);
    if (p.PaymentStatus === 'Pending' && status === 'Success') {
      if (inv.Status === 'Cancelled') throw new ApiError('Hóa đơn đã hủy nên không thể xác nhận thanh toán.', { code: 'LOCKED' });
      p.PaymentStatus = 'Success'; inv.Status = 'Paid';
    } else if (p.PaymentStatus === 'Pending' && status === 'Failed') {
      p.PaymentStatus = 'Failed';
    } else if (p.PaymentStatus === 'Success' && status === 'Refunded') {
      if (inv.Status !== 'Cancelled') throw new ApiError('Chỉ hoàn tiền cho hóa đơn đã hủy. Hãy hủy vé trước.', { code: 'LOCKED' });
      p.PaymentStatus = 'Refunded';
    } else {
      throw new ApiError('Không thể chuyển giao dịch sang trạng thái này.', { code: 'LOCKED' });
    }
    return joinPayment(p);
  }),
};

// ---------- reports ----------
const reports = {
  revenue: ({ from, to }) => call(() => {
    const f = from || addDaysISO(todayISO(), -29); const t = to || todayISO();
    const dayOf = (iso) => { const d = new Date(iso); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`; };
    const inRangeLocal = (iso) => { const d = dayOf(iso); return d >= f && d <= t; };
    const days = {};
    for (let d = f; d <= t; d = addDaysISO(d, 1)) days[d] = { date: d, revenue: 0, tickets: 0 };
    const byMovie = {}; const byMethod = {};
    let total = 0, invoices = 0, tickets = 0, refunded = 0;
    db.payments.forEach((p) => {
      if (!inRangeLocal(p.PaymentDate)) return;
      if (p.PaymentStatus === 'Refunded') { refunded += p.Amount; return; }
      if (p.PaymentStatus !== 'Success') return;
      const live = db.tickets.filter((x) => x.InvoiceID === p.InvoiceID && x.Status === 'Booked');
      const day = days[dayOf(p.PaymentDate)];
      total += p.Amount; invoices++; tickets += live.length;
      day.revenue += p.Amount; day.tickets += live.length;
      byMethod[p.PaymentMethod] = (byMethod[p.PaymentMethod] || 0) + p.Amount;
      const st = showtimeOfInvoice(p.InvoiceID);
      const title = movieOf(st.MovieID).Title;
      const row = (byMovie[title] ||= { title, revenue: 0, tickets: 0 });
      row.revenue += live.reduce((s, x) => s + x.Price, 0); row.tickets += live.length;
    });
    const needsRefund = db.payments.map(joinPayment).filter((p) => p.NeedsRefund);
    return {
      from: f, to: t, total, invoices, tickets, refunded,
      needsRefundCount: needsRefund.length, needsRefundAmount: needsRefund.reduce((s, p) => s + p.Amount, 0),
      byDay: Object.values(days),
      byMovie: Object.values(byMovie).sort((a, b) => b.revenue - a.revenue).slice(0, 5),
      byMethod: Object.entries(byMethod).map(([method, revenue]) => ({ method, revenue })).sort((a, b) => b.revenue - a.revenue),
    };
  }),
};

export const mockApi = { auth, movies, genres, rooms: roomsApi, showtimes, bookings, payments, reports };
