import React, { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { CineLogo } from './CineLogo';
import { THEATERS } from '../assets/mockData';

export const Header = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [showTheaters, setShowTheaters] = useState(false);
  const [selectedTheater, setSelectedTheater] = useState(THEATERS[0]);

  // Read current user from localStorage
  const getUser = () => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_user');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  };

  const currentUser = getUser();

  // Read ticket count from localStorage
  const getTicketCount = () => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_tickets');
      if (stored) {
        const list = JSON.parse(stored);
        return list.filter((t) => t.status === 'confirmed').length;
      }
      return 1;
    } catch {
      return 0;
    }
  };

  const ticketCount = getTicketCount();

  const handleLogout = () => {
    localStorage.removeItem('Vibe Cinema_user');
    window.dispatchEvent(new Event('authChange'));
    navigate('/');
  };

  const isActive = (path) => location.pathname === path;

  return (
    <header className="fixed top-0 left-0 w-full z-50 bg-[#0f131d]/90 backdrop-blur-xl border-b border-white/5 shadow-[0_1px_8px_rgba(0,0,0,0.4)]">
      <div className="h-20 w-full px-4 sm:px-8 flex items-center justify-between gap-4 lg:gap-6">
        {/* Brand & Cinema Selector */}
        <div className="flex items-center gap-4 lg:gap-6 shrink-0">
          <Link to="/" className="flex items-center hover:opacity-90 transition-opacity">
            <CineLogo size="md" />
          </Link>

          {/* Cinema Branch Dropdown */}
          <div className="relative hidden xl:block">
            <button
              onClick={() => setShowTheaters(!showTheaters)}
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-[#262a35] hover:bg-[#353944] text-[#e9bcb6] hover:text-[#dfe2f1] transition-all text-xs font-semibold cursor-pointer"
              type="button"
            >
              <span className="material-symbols-outlined text-[#ffb95f] text-[18px]">location_on</span>
              <span className="text-[#dfe2f1] font-medium">{selectedTheater.shortName}</span>
              <span className="material-symbols-outlined text-[18px] text-[#af8782]">expand_more</span>
            </button>

            {showTheaters && (
              <div className="absolute left-0 mt-2 w-72 bg-[#1c1f2a] border border-white/10 rounded-2xl p-2 shadow-2xl z-50">
                <div className="px-3 py-2 text-xs font-semibold text-[#af8782] uppercase tracking-wider">
                  Chọn cụm rạp Vibe Cinema
                </div>
                <div className="space-y-1">
                  {THEATERS.map((t) => (
                    <button
                      key={t.id}
                      onClick={() => {
                        setSelectedTheater(t);
                        setShowTheaters(false);
                      }}
                      className={`w-full text-left px-3 py-2.5 rounded-xl text-xs transition-colors flex flex-col cursor-pointer ${
                        selectedTheater.id === t.id
                          ? 'bg-[#e50914]/15 text-[#ffb4aa] font-semibold border border-[#e50914]/30'
                          : 'text-[#dfe2f1] hover:bg-[#262a35]'
                      }`}
                    >
                      <span className="font-medium text-sm">{t.name}</span>
                      <span className="text-[11px] text-[#af8782] line-clamp-1">{t.address}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Navigation Links */}
        <nav className="hidden md:flex items-center gap-1.5">
          <Link
            to="/"
            className={`px-4 py-2 text-sm font-medium rounded-xl transition-all ${
              isActive('/')
                ? 'bg-[#e50914] text-white shadow-[0_0_12px_rgba(229,9,20,0.45)]'
                : 'text-[#e9bcb6] hover:text-white hover:bg-white/5'
            }`}
          >
            Trang chủ
          </Link>
          <Link
            to="/now-showing"
            className={`px-4 py-2 text-sm font-medium rounded-xl transition-all ${
              isActive('/now-showing')
                ? 'bg-[#e50914] text-white shadow-[0_0_12px_rgba(229,9,20,0.45)]'
                : 'text-[#e9bcb6] hover:text-white hover:bg-white/5'
            }`}
          >
            Phim Đang Chiếu
          </Link>
          <Link
            to="/movie/dat-rung-phuong-nam/showtime"
            className={`px-4 py-2 text-sm font-medium rounded-xl transition-all ${
              location.pathname.includes('/showtime')
                ? 'bg-[#e50914] text-white shadow-[0_0_12px_rgba(229,9,20,0.45)]'
                : 'text-[#e9bcb6] hover:text-white hover:bg-white/5'
            }`}
          >
            Lịch chiếu
          </Link>
          <Link
            to="/profile"
            className={`px-4 py-2 text-sm font-medium rounded-xl transition-all flex items-center gap-1.5 ${
              isActive('/profile')
                ? 'bg-[#e50914] text-white shadow-[0_0_12px_rgba(229,9,20,0.45)]'
                : 'text-[#e9bcb6] hover:text-white hover:bg-white/5'
            }`}
          >
            <span>Vé của tôi</span>
            {ticketCount > 0 && (
              <span className="px-1.5 py-0.2 text-[10px] bg-[#ee9800] text-black font-bold rounded-full">
                {ticketCount}
              </span>
            )}
          </Link>
        </nav>

        {/* User Account Controls */}
        <div className="flex items-center gap-3">
          {currentUser ? (
            <div className="flex items-center gap-2">
              <Link
                to="/profile"
                className="flex items-center gap-2 pl-2 py-1.5 pr-3 rounded-xl bg-[#171b26] hover:bg-[#262a35] border border-white/5 text-left transition-all"
              >
                <div className="w-8 h-8 rounded-full bg-gradient-to-br from-[#e50914] to-[#93000a] text-white flex items-center justify-center text-xs font-bold font-['Space_Grotesk'] ring-1 ring-white/15">
                  {currentUser.name ? currentUser.name.charAt(0).toUpperCase() : 'U'}
                </div>
                <div className="hidden sm:flex flex-col">
                  <span className="text-xs font-semibold text-[#dfe2f1] leading-tight line-clamp-1 max-w-[120px]">
                    {currentUser.name}
                  </span>
                  <span className="text-[10px] text-[#af8782] leading-tight font-medium">
                    Khách hàng
                  </span>
                </div>
              </Link>
              <button
                onClick={handleLogout}
                title="Đăng xuất"
                className="p-2 rounded-xl bg-[#171b26] hover:bg-red-950/40 text-[#af8782] hover:text-red-400 border border-white/5 transition-colors cursor-pointer"
              >
                <span className="material-symbols-outlined text-[18px]">logout</span>
              </button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link
                to="/login"
                className="px-3.5 py-2 rounded-xl bg-[#171b26] hover:bg-[#262a35] text-xs font-semibold text-[#dfe2f1] border border-white/10 transition-colors"
              >
                Đăng Nhập
              </Link>
              <Link
                to="/register"
                className="px-3.5 py-2 rounded-xl bg-[#e50914] hover:brightness-110 text-xs font-bold text-white shadow-[0_2px_10px_rgba(229,9,20,0.4)] transition-all hidden sm:block"
              >
                Đăng Ký
              </Link>
            </div>
          )}
        </div>
      </div>

      {/* Mobile Nav Sub-Bar */}
      <div className="flex md:hidden items-center justify-around px-2 py-2 bg-[#0a0e18] border-t border-white/5 text-xs">
        <Link
          to="/"
          className={`px-3 py-1.5 rounded-lg ${isActive('/') ? 'bg-[#e50914] text-white font-bold' : 'text-[#af8782]'}`}
        >
          Trang chủ
        </Link>
        <Link
          to="/now-showing"
          className={`px-3 py-1.5 rounded-lg ${isActive('/now-showing') ? 'bg-[#e50914] text-white font-bold' : 'text-[#af8782]'}`}
        >
          Đang Chiếu
        </Link>
        <Link
          to="/movie/dat-rung-phuong-nam/showtime"
          className={`px-3 py-1.5 rounded-lg ${location.pathname.includes('/showtime') ? 'bg-[#e50914] text-white font-bold' : 'text-[#af8782]'}`}
        >
          Lịch chiếu
        </Link>
        <Link
          to="/profile"
          className={`px-3 py-1.5 rounded-lg flex items-center gap-1 ${isActive('/profile') ? 'bg-[#e50914] text-white font-bold' : 'text-[#af8782]'}`}
        >
          <span>Vé của tôi</span>
          {ticketCount > 0 && (
            <span className="px-1 py-0 text-[9px] bg-[#ee9800] text-black font-bold rounded-full">
              {ticketCount}
            </span>
          )}
        </Link>
      </div>
    </header>
  );
};

