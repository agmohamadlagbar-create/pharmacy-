import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../../backend/OnlineStore.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    port: 5175,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/uploads': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        ws: true,
      },
      '/media': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  }
})
