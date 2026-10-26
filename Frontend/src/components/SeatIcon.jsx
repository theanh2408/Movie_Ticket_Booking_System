import React from 'react';
import { cn } from '../lib/cn';

export const SeatIcon = ({
  seatId,
  status = 'available', // 'available' | 'selected' | 'booked'
  price = 95000,
  onClick,
  className = '',
}) => {
  const statusVariants = {
    available:
      'bg-seat-available border-white/20 text-on-surface hover:border-white hover:bg-surface-high',
    selected:
      'bg-seat-selected text-white font-bold border-seat-selected shadow-glow-seat scale-105',
    booked:
      'bg-seat-booked text-white/70 cursor-not-allowed border-transparent',
  };

  return (
    <button
      type="button"
      disabled={status === 'booked'}
      onClick={onClick}
      className={cn(
        'size-7 sm:size-8 border rounded-t-md rounded-b-xs text-[10px] font-headline flex items-center justify-center transition-all cursor-pointer select-none',
        statusVariants[status] || statusVariants.available,
        className
      )}
      title={
        status === 'booked'
          ? `Ghế ${seatId} (Đã được đặt)`
          : `Ghế ${seatId} - ${price.toLocaleString('vi-VN')}đ`
      }
    >
      {seatId.slice(1)}
    </button>
  );
};
