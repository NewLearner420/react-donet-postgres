import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  
  server: {
    // CRITICAL: Listen on all network interfaces for Docker
    host: '0.0.0.0',
    port: 3000,
    strictPort: true,
    
    // Enable hot reload in Docker
    watch: {
      usePolling: true,
      interval: 1000,
    },
    
    // Allow connections from Codespaces forwarded URLs
    hmr: {
      clientPort: 3000,
    },
  },
  
  preview: {
    host: '0.0.0.0',
    port: 3000,
    strictPort: true,
  },
  
  build: {
    outDir: 'dist',
    sourcemap: false,
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom'],
        },
      },
    },
  },
})