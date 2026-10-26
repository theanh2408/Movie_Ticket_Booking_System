import React from 'react';
import logoImg from '../assets/logo.jpg';
import { cn } from '../lib/cn';

export const CineLogo = ({ className = '', size = 'md' }) => {
  const sizeMap = {
    sm: { icon: 'size-7', textCine: 'text-lg', textSub: 'text-[9px]' },
    md: { icon: 'size-9', textCine: 'text-xl', textSub: 'text-[10px]' },
    lg: { icon: 'size-12', textCine: 'text-2xl', textSub: 'text-xs' },
  };

  const currentSize = sizeMap[size] || sizeMap.md;

  return (
    <div className={cn('flex items-center gap-2.5 select-none', className)}>
      <div className={cn(currentSize.icon, 'shrink-0 overflow-hidden rounded-full')}>
        <img
          src={logoImg}
          alt="Vibe Cinema Logo"
          className="size-full object-cover"
        />
      </div>
      <div className="flex flex-col leading-none">
        <span className={cn('font-headline font-black tracking-tight text-white', currentSize.textCine)}>
          VIBE<span className="text-primary">CINEMA</span>
        </span>
        <span className={cn('font-headline font-medium tracking-[0.25em] text-outline uppercase', currentSize.textSub)}>
          CINEMA SYSTEM
        </span>
      </div>
    </div>
  );
};
