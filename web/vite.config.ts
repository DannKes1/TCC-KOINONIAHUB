import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:7054",
        changeOrigin: true,
        secure: false, // aceita o certificado de dev do Kestrel
      },
    },
  },
});