/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  // Vitest (Plano de Desenvolvimento, 7.2): specs ao lado do código testado.
  test: {
    environment: "jsdom",
    globals: true,
    include: ["src/**/*.spec.ts"],
  },
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
