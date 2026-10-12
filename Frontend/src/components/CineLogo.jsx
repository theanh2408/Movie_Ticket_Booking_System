import React from 'react';
import logoImg from '../assets/logo.jpg';

export const CineLogo = ({ className = '', size = 'md' }) => {
  const sizeMap = {
    sm: { icon: 'w-7 h-7', textCine: 'text-lg', textSub: 'text-[9px]' },
    md: { icon: 'w-9 h-9', textCine: 'text-xl', textSub: 'text-[10px]' },
    lg: { icon: 'w-12 h-12', textCine: 'text-2xl', textSub: 'text-xs' },
  };

  const currentSize = sizeMap[size] || sizeMap.md;

  return (
    <div className={`flex items-center gap-2.5 select-none ${className}`}>
      <div className={`${currentSize.icon} shrink-0 overflow-hidden rounded-full`}>
        <img
          src={logoImg}
          alt="Vibe Cinema Logo"
          className="w-full h-full object-cover"
        />
      </div>
      <div className="flex flex-col leading-none">
        <span className={`font-['Space_Grotesk'] font-black tracking-tight text-white ${currentSize.textCine}`}>
          VIBE<span className="text-[#e50914]">CINEMA</span>
        </span>
        <span className={`font-['Space_Grotesk'] font-medium tracking-[0.25em] text-[#af8782] uppercase ${currentSize.textSub}`}>
          CINEMA SYSTEM
        </span>
      </div>
    </div>
  );
};
