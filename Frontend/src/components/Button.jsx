import React from 'react';

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
    "inline-flex items-center justify-center font-semibold rounded-xl transition-all cursor-pointer select-none disabled:opacity-50 disabled:cursor-not-allowed";

  const sizeClasses = {
    sm: "px-3 py-1.5 text-xs gap-1.5",
    md: "px-4 py-2.5 text-sm gap-2",
    lg: "px-6 py-3.5 text-base gap-2.5 font-bold",
  };

  const variantClasses = {
    primary:
      "bg-[#e50914] text-white hover:brightness-110 shadow-[0_4px_15px_rgba(229,9,20,0.35)] active:scale-95",
    secondary:
      "bg-[#1c1f2a] text-[#dfe2f1] hover:bg-[#262a35] border border-white/5",
    outline:
      "bg-transparent text-[#dfe2f1] hover:bg-[#262a35] border border-white/15",
    danger:
      "bg-red-950/40 text-red-400 hover:bg-red-900/50 border border-red-900/30",
  };

  return (
    <button
      type={type}
      disabled={disabled}
      onClick={onClick}
      className={`${baseClasses} ${sizeClasses[size] || sizeClasses.md} ${
        variantClasses[variant] || variantClasses.primary
      } ${className}`}
    >
      {icon && <span className="material-symbols-outlined text-[18px]">{icon}</span>}
      {children}
    </button>
  );
};
