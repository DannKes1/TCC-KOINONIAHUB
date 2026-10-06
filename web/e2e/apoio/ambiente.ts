// Endereços dos servidores usados pelos fluxos E2E (Plano 7.3).
// O front de desenvolvimento é http://localhost:5173 (Vite) e encaminha /api para a
// API em https://localhost:7054; a semente fala direto com a API.
export const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:5173";
export const apiURL = process.env.E2E_API_URL ?? "https://localhost:7054";

// Opcional: caminho de um Chromium/Chrome já instalado, para não baixar o navegador
// do Playwright (E2E_CHROMIUM=C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe).
export const executavelChromium = process.env.E2E_CHROMIUM || undefined;
