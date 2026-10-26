import React, { useState, useEffect, useRef } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { CineLogo } from './CineLogo';
import { THEATERS } from '../assets/mockData';
import { cn } from '../lib/cn';

export const Header = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const [showTheaters, setShowTheaters] = useState(false);
  const [selectedTheater, setSelectedTheater] = useState(THEATERS[0]);
  const [accountMenuOpen, setAccountMenuOpen] = useState(false);
  const accountMenuRef = useRef(null);

  // Click outside to close account dropdown
  useEffect(() => {
    const handleClickOutside = (e) => {
      if (accountMenuRef.current && !accountMenuRef.current.contains(e.target)) {
        setAccountMenuOpen(false);
      }
    };
    if (accountMenuOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [accountMenuOpen]);

  // Read current user from localStorage
  const getUser = () => {
    try {
      const stored = localStorage.getItem('Vibe Cinema_user');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  };

  const [currentUser, setCurrentUser] = useState(getUser);

  useEffect(() => {
    const handleAuthChange = () => {
      setCurrentUser(getUser());
    };
    window.addEventListener('authChange', handleAuthChange);
    window.addEventListener('storage', handleAuthChange);
    return () => {
      window.removeEventListener('authChange', handleAuthChange);
      window.removeEventListener('storage', handleAuthChange);
    };
  }, []);

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

  const getNavLinkClass = (active) =>
    cn(
      'px-4 py-2 text-sm font-medium rounded-xl transition-all',
      active
        ? 'bg-primary text-white shadow-glow-nav'
        : 'text-on-surface-variant hover:text-white hover:bg-white/5'
    );

  const getMobileNavLinkClass = (active) =>
    cn(
      'px-3 py-1.5 rounded-lg transition-colors',
      active ? 'bg-primary text-white font-bold' : 'text-outline hover:text-on-surface'
    );

  return (
    <header className="fixed top-0 left-0 w-full z-50 bg-surface/90 backdrop-blur-xl border-b border-white/5 shadow-[0_1px_8px_rgba(0,0,0,0.4)]">
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
              className="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-surface-high hover:bg-surface-highest text-on-surface-variant hover:text-on-surface transition-all text-xs font-semibold cursor-pointer"
              type="button"
            >
              <span className="material-symbols-outlined text-secondary text-[18px]">location_on</span>
              <span className="text-on-surface font-medium">{selectedTheater.shortName}</span>
              <span className="material-symbols-outlined text-[18px] text-outline">expand_more</span>
            </button>

            {showTheaters && (
              <div className="absolute left-0 mt-2 w-72 bg-surface-container border border-white/10 rounded-2xl p-2 shadow-2xl z-50">
                <div className="px-3 py-2 text-xs font-semibold text-outline uppercase tracking-wider">
                  Danh sách rạp hiện có
                </div>
                <div className="space-y-1">
                  {THEATERS.map((t) => (
                    <button
                      key={t.id}
                      onClick={() => {
                        setSelectedTheater(t);
                        setShowTheaters(false);
                      }}
                      className={cn(
                        'w-full text-left px-3 py-2.5 rounded-xl text-xs transition-colors flex flex-col cursor-pointer',
                        selectedTheater.id === t.id
                          ? 'bg-primary/15 text-primary-light font-semibold border border-primary/30'
                          : 'text-on-surface hover:bg-surface-high'
                      )}
                    >
                      <span className="font-medium text-sm">{t.name}</span>
                      <span className="text-[11px] text-outline line-clamp-1">{t.address}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Navigation Links */}
        <nav className="hidden md:flex items-center gap-1.5">
          <Link to="/" className={getNavLinkClass(isActive('/'))}>
            Trang chủ
          </Link>
          <Link to="/now-showing" className={getNavLinkClass(isActive('/now-showing'))}>
            Phim Đang Chiếu
          </Link>
          <Link
            to="/movie/dat-rung-phuong-nam/showtime"
            className={getNavLinkClass(location.pathname.includes('/showtime'))}
          >
            Lịch chiếu
          </Link>
          <Link
            to="/my-tickets"
            className={cn(getNavLinkClass(isActive('/my-tickets')), 'flex items-center gap-1.5')}
          >
            <span>Vé của tôi</span>
            {ticketCount > 0 && (
              <span className="px-1.5 py-0.2 text-[10px] bg-secondary-container text-black font-bold rounded-full">
                {ticketCount}
              </span>
            )}
          </Link>
        </nav>

        {/* User Account Controls */}
        <div className="flex items-center gap-3">
          {currentUser ? (
            <div
              ref={accountMenuRef}
              className="relative group py-1"
              onMouseEnter={() => setAccountMenuOpen(true)}
              onMouseLeave={() => setAccountMenuOpen(false)}
            >
              {/* Trigger button */}
              <button
                type="button"
                onClick={() => setAccountMenuOpen((prev) => !prev)}
                className="flex items-center gap-2.5 pl-2 py-1.5 pr-2.5 rounded-xl bg-surface-low hover:bg-surface-high border border-white/10 text-left transition-all cursor-pointer select-none group"
              >
                {currentUser.avatar ? (
                  <img
                    src={currentUser.avatar}
                    alt={currentUser.name}
                    className="size-8 rounded-full object-cover ring-1 ring-white/15 shrink-0 shadow-md"
                  />
                ) : (
                  <div className="size-8 rounded-full bg-gradient-to-br from-primary to-primary-dark text-white flex items-center justify-center text-xs font-bold font-headline ring-1 ring-white/15 shrink-0 shadow-md">
                    {currentUser.name ? currentUser.name.charAt(0).toUpperCase() : 'U'}
                  </div>
                )}
                <div className="hidden sm:flex flex-col">
                  <span className="text-xs font-semibold text-on-surface leading-tight line-clamp-1 max-w-[120px]">
                    {currentUser.name}
                  </span>
                  <span className="text-[10px] text-outline leading-tight font-medium">
                    Khách hàng
                  </span>
                </div>
                <span
                  className={cn(
                    'material-symbols-outlined text-[18px] text-outline transition-transform duration-200',
                    accountMenuOpen ? 'rotate-180 text-secondary' : 'group-hover:text-on-surface'
                  )}
                >
                  expand_more
                </span>
              </button>

              {/* Dropdown Menu on hover/click */}
              <div
                className={cn(
                  'absolute right-0 top-full pt-1.5 w-56 z-50 transition-all duration-150',
                  accountMenuOpen ? 'block' : 'hidden group-hover:block'
                )}
              >
                <div className="bg-surface-container border border-white/10 rounded-2xl p-2 shadow-2xl backdrop-blur-xl">
                  {/* Account Header Info */}
                  <div className="px-3.5 py-2.5 border-b border-white/5 mb-1 flex items-center gap-3">
                    {currentUser.avatar ? (
                      <img
                        src={currentUser.avatar}
                        alt={currentUser.name}
                        className="size-9 rounded-full object-cover ring-1 ring-white/10 shrink-0"
                      />
                    ) : (
                      <div className="size-9 rounded-full bg-gradient-to-br from-primary to-primary-dark text-white flex items-center justify-center text-xs font-bold font-headline ring-1 ring-white/10 shrink-0 shadow-sm">
                        {currentUser.name ? currentUser.name.charAt(0).toUpperCase() : 'U'}
                      </div>
                    )}
                    <div className="overflow-hidden">
                      <div className="text-xs font-bold text-on-surface truncate">
                        {currentUser.name}
                      </div>
                      <div className="text-[11px] text-outline truncate">
                        {currentUser.email || 'Khách hàng'}
                      </div>
                    </div>
                  </div>

                  {/* Menu Options */}
                  <div className="space-y-0.5">
                    <Link
                      to="/profile"
                      onClick={() => setAccountMenuOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-on-surface hover:text-white hover:bg-surface-high transition-colors"
                    >
                      <span className="material-symbols-outlined text-[18px] text-secondary">
                        person
                      </span>
                      <span>Hồ sơ của tôi</span>
                    </Link>

                    <Link
                      to="/profile?tab=transactions"
                      onClick={() => setAccountMenuOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-on-surface hover:text-white hover:bg-surface-high transition-colors"
                    >
                      <span className="material-symbols-outlined text-[18px] text-emerald-400">
                        receipt_long
                      </span>
                      <span>Lịch sử giao dịch</span>
                    </Link>

                    <Link
                      to="/my-tickets"
                      onClick={() => setAccountMenuOpen(false)}
                      className="flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-on-surface hover:text-white hover:bg-surface-high transition-colors"
                    >
                      <span className="material-symbols-outlined text-[18px] text-tertiary">
                        confirmation_number
                      </span>
                      <span>Vé của tôi</span>
                    </Link>

                    <div className="my-1 border-t border-white/5" />

                    <button
                      type="button"
                      onClick={() => {
                        setAccountMenuOpen(false);
                        handleLogout();
                      }}
                      className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-medium text-primary hover:text-white hover:bg-primary/20 transition-colors cursor-pointer text-left"
                    >
                      <span className="material-symbols-outlined text-[18px]">logout</span>
                      <span>Đăng xuất</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Link
                to="/login"
                className="px-3.5 py-2 rounded-xl bg-surface-low hover:bg-surface-high text-xs font-semibold text-on-surface border border-white/10 transition-colors"
              >
                Đăng Nhập
              </Link>
              <Link
                to="/register"
                className="px-3.5 py-2 rounded-xl bg-primary hover:brightness-110 text-xs font-bold text-white shadow-glow-primary transition-all hidden sm:block"
              >
                Đăng Ký
              </Link>
            </div>
          )}
        </div>
      </div>

      {/* Mobile Nav Sub-Bar */}
      <div className="flex md:hidden items-center justify-around px-2 py-2 bg-surface-lowest border-t border-white/5 text-xs">
        <Link to="/" className={getMobileNavLinkClass(isActive('/'))}>
          Trang chủ
        </Link>
        <Link to="/now-showing" className={getMobileNavLinkClass(isActive('/now-showing'))}>
          Đang Chiếu
        </Link>
        <Link
          to="/movie/dat-rung-phuong-nam/showtime"
          className={getMobileNavLinkClass(location.pathname.includes('/showtime'))}
        >
          Lịch chiếu
        </Link>
        <Link
          to="/my-tickets"
          className={cn(getMobileNavLinkClass(isActive('/my-tickets')), 'flex items-center gap-1')}
        >
          <span>Vé của tôi</span>
          {ticketCount > 0 && (
            <span className="px-1 py-0 text-[9px] bg-secondary-container text-black font-bold rounded-full">
              {ticketCount}
            </span>
          )}
        </Link>
      </div>
    </header>
  );
};
