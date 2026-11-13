import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import Navbar from './components/Navbar';
import Footer from './components/Footer';

// Customer Pages
import Home from './pages/customer/Home';
import MovieDetail from './pages/customer/MovieDetail';
import SeatSelection from './pages/customer/SeatSelection';
import Checkout from './pages/customer/Checkout';
import Profile from './pages/customer/Profile';
import Showtime from './pages/customer/Showtime';

// Auth Pages
import Login from './pages/auth/Login';
import Register from './pages/auth/Register';

// Admin Pages
import AdminLayout from './pages/admin/AdminLayout';
import Dashboard from './pages/admin/Dashboard';
import ManageMovies from './pages/admin/ManageMovies';
import ManageRooms from './pages/admin/ManageRooms';
import ManageShowtimes from './pages/admin/ManageShowtimes';
import ManageBookings from './pages/admin/ManageBookings';

function CustomerLayout() {
  return (
    <div className="flex flex-col min-h-screen">
      <Navbar />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public & Customer Layout with Navbar & Footer */}
          <Route element={<CustomerLayout />}>
            <Route path="/" element={<Home />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/movie/:id" element={<MovieDetail />} />
            <Route path="/movie/:id/showtime" element={<Showtime />} />
            <Route path="/showtime/:id/seats" element={<SeatSelection />} />
            <Route path="/checkout" element={<Checkout />} />
            <Route path="/profile" element={<Profile />} />
          </Route>

          {/* Admin Nested Routes with AdminLayout Sidebar */}
          <Route path="/admin" element={<AdminLayout />}>
            <Route index element={<Dashboard />} />
            <Route path="movies" element={<ManageMovies />} />
            <Route path="rooms" element={<ManageRooms />} />
            <Route path="showtimes" element={<ManageShowtimes />} />
            <Route path="bookings" element={<ManageBookings />} />
          </Route>

          {/* Fallback */}
          <Route path="*" element={<Home />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;
