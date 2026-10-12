import React from 'react';
import { Header } from '../components/Header';
import { Footer } from '../components/Footer';

export const MainLayout = ({ children }) => {
  return (
    <div className="min-h-screen flex flex-col bg-[#0f131d] text-[#dfe2f1] selection:bg-[#e50914] selection:text-[#fff7f6]">
      <Header />
      <main className="flex-1 pt-20 md:pt-20">
        {children}
      </main>
      <Footer />
    </div>
  );
};
