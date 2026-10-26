import React from 'react';
import { Header } from '../components/Header';
import { Footer } from '../components/Footer';
import { cn } from '../lib/cn';

export const MainLayout = ({ children, className = '' }) => {
  return (
    <div className={cn('min-h-screen flex flex-col bg-surface text-on-surface selection:bg-primary selection:text-white', className)}>
      <Header />
      <main className="flex-1 pt-20">
        {children}
      </main>
      <Footer />
    </div>
  );
};
