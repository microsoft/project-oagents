import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
  },
  define: {
    'process.env.VITE_OAGENT_BASE_URL': JSON.stringify(process.env.VITE_OAGENT_BASE_URL),
  },
});