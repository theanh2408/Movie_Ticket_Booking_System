import { BrowserRouter, Routes, Route } from 'react-router-dom';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Customer & Public Routes */}
        <Route path="/" element={<Home />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/movie/:id" element={<MovieDetail />} />
        <Route path="/showtime/:id/seats" element={<SeatSelection />} />
        <Route path="/checkout" element={<Checkout />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="/movie/:id/showtime" element={<Showtime />} />

        {/* Admin Routes (Sử dụng Nested Routes) */}
        <Route path="/admin" element={<AdminLayout />}>
          <Route path="movies" element={<ManageMovies />} />
          <Route path="rooms" element={<ManageRooms />} />
          <Route path="showtimes" element={<ManageShowtimes />} />
          <Route path="bookings" element={<ManageBookings />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;