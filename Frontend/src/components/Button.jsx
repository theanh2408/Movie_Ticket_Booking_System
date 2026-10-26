import React from 'react';
import { cn } from '../lib/cn';

export const Button = ({
  children,
  variant = 'primary', // 'primary' | 'secondary' | 'outline' | 'danger'
  size = 'md', // 'sm' | 'md' | 'lg'
  icon,
  className = '',
  disabled = false,
  onClick,
  type = 'button',
}) => {
  const baseClasses =
    'inline-flex items-center justify-center font-semibold rounded-xl transition-all cursor-pointer select-none disabled:opacity-50 disabled:cursor-not-allowed';

  const sizeClasses = {
    sm: 'gap-1.5 px-3 py-1.5 text-xs',
    md: 'gap-2 px-4 py-2.5 text-sm',
    lg: 'gap-2.5 px-6 py-3.5 text-base font-bold',
  };

  const variantClasses = {
    primary:
      'bg-primary text-white hover:brightness-110 shadow-glow-primary active:scale-95',
    secondary:
      'bg-surface-container text-on-surface hover:bg-surface-high border border-white/5',
    outline:
      'bg-transparent text-on-surface hover:bg-surface-high border border-white/15',
    danger:
      'bg-red-950/40 text-red-400 hover:bg-red-900/50 border border-red-900/30',
  };

  return (
    <button
      type={type}
      disabled={disabled}
      onClick={onClick}
      className={cn(
        baseClasses,
        sizeClasses[size] || sizeClasses.md,
        variantClasses[variant] || variantClasses.primary,
        className
      )}
    >
      {icon && <span className="material-symbols-outlined text-[18px]">{icon}</span>}
      {children}
    </button>
  );
};
