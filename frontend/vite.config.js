import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Backend ASP.NET Core Web API (.NET 10) chạy ở cổng 5080 khi dev.
// Frontend gọi /api/... và Vite chuyển tiếp sang backend để tránh CORS khi phát triển.
const API_TARGET = process.env.VITE_API_PROXY_TARGET || 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: true
      }
    }
  }
});