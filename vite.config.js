import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Khi chạy với backend thật (VITE_USE_MOCK=false), mọi request /api sẽ được chuyển tới backend.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': { target: process.env.VITE_PROXY_TARGET || 'http://localhost:8080', changeOrigin: true },
    },
  },
});
