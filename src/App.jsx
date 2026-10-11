import { Navigate, Route, Routes } from 'react-router-dom';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import MoviesPage from './pages/MoviesPage';
import RoomsPage from './pages/RoomsPage';
import ShowtimesPage from './pages/ShowtimesPage';
import BookingsPage from './pages/BookingsPage';
import BookingDetailPage from './pages/BookingDetailPage';
import PaymentsPage from './pages/PaymentsPage';
import NotFoundPage from './pages/NotFoundPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="movies" element={<MoviesPage />} />
          <Route path="rooms" element={<RoomsPage />} />
          <Route path="showtimes" element={<ShowtimesPage />} />
          <Route path="bookings" element={<BookingsPage />} />
          <Route path="bookings/:id" element={<BookingDetailPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
