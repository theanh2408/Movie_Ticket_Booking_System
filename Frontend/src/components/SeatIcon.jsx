import React from 'react';

export const SeatIcon = ({
  seatId,
  status = 'available', // 'available' | 'selected' | 'booked'
  price = 95000,
  onClick,
}) => {
  let styleClass = 'bg-[#1f2937] border border-white/20 text-[#dfe2f1] hover:border-white hover:bg-[#262a35]';

  if (status === 'booked') {
    styleClass = 'bg-[#111827] text-white/20 opacity-30 cursor-not-allowed border-transparent';
  } else if (status === 'selected') {
    styleClass = 'bg-[#e50914] text-white font-bold border-[#e50914] shadow-[0_0_12px_rgba(229,9,20,0.7)] scale-105';
  }

  return (
    <button
      type="button"
      disabled={status === 'booked'}
      onClick={onClick}
      className={`w-7 sm:w-8 h-7 sm:h-8 rounded-t-md rounded-b-xs text-[10px] font-['Space_Grotesk'] flex items-center justify-center transition-all cursor-pointer ${styleClass}`}
      title={
        status === 'booked'
          ? `Ghế ${seatId} (Đã được đặt)`
          : `Ghế ${seatId} - ${price.toLocaleString('vi-VN')}đ`
      }
    >
      {status === 'booked' ? '✕' : seatId.slice(1)}
    </button>
  );
};
