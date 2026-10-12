import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

// Auth Pages
import Login from './pages/auth/Login';
import Register from './pages/auth/Register';

// Customer Pages
import Home from './pages/customer/Home';
import MovieDetail from './pages/customer/MovieDetail';
import Showtime from './pages/customer/Showtime';
import SeatSelection from './pages/customer/SeatSelection';
import Checkout from './pages/customer/Checkout';
import Profile from './pages/customer/Profile';
import NowShowing from './pages/customer/NowShowing';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Customer & Public Routes */}
        <Route path="/" element={<Home />} />
        <Route path="/now-showing" element={<NowShowing />} />
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="/movie/:id" element={<MovieDetail />} />
        <Route path="/movie/:id/showtime" element={<Showtime />} />
        <Route path="/showtime/:id/seats" element={<SeatSelection />} />
        <Route path="/checkout" element={<Checkout />} />
        <Route path="/profile" element={<Profile />} />

        {/* Fallback route */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;