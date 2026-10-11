// Dữ liệu giả sinh ngẫu nhiên (cố định theo seed) để làm giao diện khi chưa có backend.
// Cấu trúc bám theo ERD: Account, Cinema, Room, Seat, Movie, Genre, Movie_Genre, Showtime, Showtime_Seat, Invoice, Payment, Ticket.
import { addDaysISO, rowLabel, toISODate } from '../lib/format';

function mulberry32(a) {
  return () => {
    a |= 0; a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const rnd = mulberry32(2026);
const pick = (arr) => arr[Math.floor(rnd() * arr.length)];
const int = (min, max) => min + Math.floor(rnd() * (max - min + 1));

export const db = {
  accounts: [], cinema: null, rooms: [], seats: [], movies: [], genres: [], movieGenres: [],
  showtimes: [], showtimeSeats: [], invoices: [], payments: [], tickets: [],
};

const counters = {};
export const nextId = (prefix, pad = 4) => {
  counters[prefix] = (counters[prefix] || 0) + 1;
  return `${prefix}${String(counters[prefix]).padStart(pad, '0')}`;
};
const setCounter = (prefix, n) => { counters[prefix] = n; };

// ---------- Tài khoản ----------
const customers = [
  ['Nguyễn Minh Anh', '0901234567'], ['Trần Quốc Bảo', '0912345678'], ['Lê Thu Hà', '0923456789'],
  ['Phạm Gia Huy', '0934567890'], ['Võ Ngọc Lan', '0945678901'], ['Đặng Hoàng Nam', '0956789012'],
  ['Bùi Khánh Linh', '0967890123'], ['Hoàng Đức Thịnh', '0978901234'],
];
db.accounts.push({ UserID: 'U001', FullName: 'Quản trị viên', Phone: '0900000000', Email: 'admin@cinema.vn', DOB: '1995-01-01', Gender: 'Other', Role: 'Admin', _pw: 'admin123' });
customers.forEach(([name, phone], i) => {
  const slug = ['minhanh', 'quocbao', 'thuha', 'giahuy', 'ngoclan', 'hoangnam', 'khanhlinh', 'ducthinh'][i];
  db.accounts.push({ UserID: `U${String(i + 2).padStart(3, '0')}`, FullName: name, Phone: phone, Email: `${slug}@mail.com`, DOB: '2000-05-12', Gender: i % 2 ? 'Male' : 'Female', Role: 'Customer', _pw: 'customer123' });
});

db.cinema = { CinemaID: 'C01', CinemaName: 'Rạp Chiếu Phim Trung Tâm', Address: '12 Lê Lợi, Quận 1, TP.HCM', Status: 'Active' };

// ---------- Thể loại & phim ----------
['Hành động', 'Tình cảm', 'Hài', 'Kinh dị', 'Hoạt hình', 'Khoa học viễn tưởng', 'Tâm lý', 'Gia đình']
  .forEach((g, i) => db.genres.push({ Genre_ID: `G${i + 1}`, GenreName: g }));

const movieSeed = [
  ['Đêm Cuối Ở Sài Gòn', 118, 'T16', 'Showing', [1, 7], -30, 'Một đêm định mệnh của hai người xa lạ giữa lòng thành phố không ngủ.'],
  ['Hành Tinh Cuối Cùng', 142, 'T13', 'Showing', [6, 1], -21, 'Nhóm nhà khoa học lên đường tìm ngôi nhà mới cho nhân loại.'],
  ['Mùa Hè Của Bố', 98, 'P', 'Showing', [8, 3], -14, 'Chuyến đi biển đầu tiên của hai bố con sau nhiều năm xa cách.'],
  ['Bóng Ma Trên Phố Cổ', 104, 'T18', 'Showing', [4], -10, 'Những chuyện lạ xảy ra khi một căn nhà cổ được mở cửa trở lại.'],
  ['Siêu Sao Nhí', 95, 'P', 'Showing', [5, 8], -7, 'Cô bé 10 tuổi bất ngờ trở thành ngôi sao âm nhạc.'],
  ['Chuyến Tàu Đêm', 126, 'T16', 'ComingSoon', [7, 4], 12, 'Bí ẩn trên chuyến tàu xuyên Việt chạy suốt đêm.'],
  ['Biển Gọi', 110, 'K', 'ComingSoon', [2, 7], 20, 'Câu chuyện tình bên bờ biển miền Trung.'],
  ['Mật Mã Đỏ', 132, 'T13', 'Hidden', [1], -90, 'Đặc vụ cuối cùng giải mã âm mưu từ bên trong.'],
];
const today = toISODate(new Date());
movieSeed.forEach(([title, dur, age, status, gens, off, desc], i) => {
  const id = `M${String(i + 1).padStart(3, '0')}`;
  db.movies.push({ MovieID: id, Title: title, Description: desc, Duration: dur, PosterURL: '', TrailerURL: '', AgeRating: age, ReleaseDate: addDaysISO(today, off), Status: status });
  gens.forEach((g) => db.movieGenres.push({ MovieID: id, Genre_ID: `G${g}` }));
});

// ---------- Phòng & ghế ----------
const roomSeed = [['R01', 'Phòng 1', 8, 10], ['R02', 'Phòng 2', 7, 10], ['R03', 'Phòng 3', 5, 8]];
roomSeed.forEach(([id, name, rows, cols]) => {
  db.rooms.push({ RoomID: id, RoomName: name, Status: 'Active', CinemaID: 'C01' });
  for (let r = 0; r < rows; r++) for (let c = 1; c <= cols; c++) {
    db.seats.push({ SeatID: `${id}-${rowLabel(r)}${c}`, Row: rowLabel(r), Column: String(c), RoomID: id });
  }
});

// ---------- Suất chiếu ----------
const slots = [['09:30', 70000], ['12:45', 85000], ['16:00', 95000], ['19:15', 110000], ['22:15', 100000]];
const showingMovies = db.movies.filter((m) => m.Status === 'Showing');
let stN = 0;
for (let off = -10; off <= 6; off++) {
  const date = addDaysISO(today, off);
  db.rooms.forEach((room) => {
    const skip = int(0, 4);
    slots.forEach(([hour, price], si) => {
      if (si === skip) return;
      stN++;
      db.showtimes.push({ ShowtimeID: `ST${String(stN).padStart(4, '0')}`, Start_hour: hour, Date: date, BasePrice: price, Status: 'Scheduled', MovieID: pick(showingMovies).MovieID, RoomID: room.RoomID });
    });
  });
}
setCounter('ST', stN);
const seatsByRoom = {};
db.seats.forEach((s) => { (seatsByRoom[s.RoomID] ||= []).push(s); });
let ssN = 0;
db.showtimes.forEach((st) => {
  seatsByRoom[st.RoomID].forEach((seat) => {
    ssN++;
    db.showtimeSeats.push({ Showtime_SeatID: `SS${String(ssN).padStart(6, '0')}`, Status: 'Available', Price: st.BasePrice, ShowtimeID: st.ShowtimeID, SeatID: seat.SeatID });
  });
});
setCounter('SS', ssN);

// ---------- Hóa đơn, vé, thanh toán ----------
const ssByShowtime = {};
db.showtimeSeats.forEach((x) => { (ssByShowtime[x.ShowtimeID] ||= []).push(x); });
const userIds = db.accounts.filter((a) => a.Role === 'Customer').map((a) => a.UserID);
const candidates = db.showtimes.filter((st) => st.Date >= addDaysISO(today, -9));
const nowMs = Date.now();
let ticketN = 0, payN = 0;
for (let i = 0; i < 72; i++) {
  const st = pick(candidates);
  const free = ssByShowtime[st.ShowtimeID].filter((x) => x.Status === 'Available');
  const n = Math.min(int(1, 4), free.length);
  const chosen = [];
  for (let k = 0; k < n; k++) chosen.push(free.splice(int(0, free.length - 1), 1)[0]);
  const startMs = new Date(`${st.Date}T${st.Start_hour}:00`).getTime();
  const created = new Date(Math.min(startMs - int(60, 2880) * 60000, nowMs - int(5, 600) * 60000));
  const roll = rnd();
  const future = startMs > nowMs;
  const status = roll < 0.12 ? 'Cancelled' : future && roll < 0.22 ? 'Pending' : 'Paid';
  const invoiceId = `HD${1000 + i}`;
  db.invoices.push({ InvoiceID: invoiceId, CreateDate: created.toISOString(), Status: status, UserID: pick(userIds) });
  let total = 0;
  chosen.forEach((ss) => {
    ticketN++;
    total += ss.Price;
    db.tickets.push({ TicketID: `TK${String(ticketN).padStart(5, '0')}`, Price: ss.Price, Status: status === 'Cancelled' ? 'Cancelled' : 'Booked', InvoiceID: invoiceId, Showtime_SeatID: ss.Showtime_SeatID });
    ss.Status = status === 'Cancelled' ? 'Available' : 'Booked';
  });
  payN++;
  const payStatus = status === 'Paid' ? 'Success' : status === 'Pending' ? 'Pending' : rnd() < 0.5 ? 'Success' : 'Refunded';
  db.payments.push({
    PaymentID: `PAY${String(payN).padStart(4, '0')}`,
    TransactionNo: `TXN${int(100000000, 999999999)}`,
    Amount: total, PaymentMethod: pick(['MoMo', 'VNPay', 'ZaloPay', 'Smartlink']), PaymentStatus: payStatus,
    PaymentDate: new Date(created.getTime() + int(1, 5) * 60000).toISOString(), InvoiceID: invoiceId,
  });
}
setCounter('TK', ticketN);
setCounter('PAY', payN);
setCounter('HD', 1000 + 72 - 1000);
