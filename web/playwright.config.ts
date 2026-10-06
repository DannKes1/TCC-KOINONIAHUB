import { defineConfig, devices } from "@playwright/test";
import type { PlaywrightTestConfig } from "@playwright/test";

import { apiURL, baseURL, executavelChromium } from "./e2e/apoio/ambiente";

// Playwright — fluxos ponta a ponta (Plano de Desenvolvimento, 7.3).
//
// O front de desenvolvimento roda em http://localhost:5173 (Vite, sem certificado
// próprio) e encaminha /api para a API em https://localhost:7054 (vite.config.ts).
// A semente e as chamadas diretas dos testes falam com a API nesse endereço, com o
// header Origin da SPA (RNF 2.6) e ignorando o certificado de desenvolvimento.
//
// Variáveis opcionais:
//   E2E_BASE_URL     front (padrão http://localhost:5173)
//   E2E_API_URL      API   (padrão https://localhost:7054)
//   E2E_INICIAR_API  "1" para o Playwright subir a API (perfil "e2e" do launchSettings,
//                    ambiente E2E); sem ela, a API deve estar rodando antes.

type ServidorWeb = Exclude<
  NonNullable<PlaywrightTestConfig["webServer"]>,
  unknown[]
>;

const servidores: ServidorWeb[] = [
  {
    name: "front (Vite)",
    command: "npm run dev",
    url: baseURL,
    reuseExistingServer: true,
    timeout: 120_000,
  },
];

if (process.env.E2E_INICIAR_API === "1") {
  servidores.unshift({
    name: "API (ambiente E2E)",
    command: "dotnet run --project ../api/KoinoniaHub.API --launch-profile e2e",
    url: `${apiURL}/api/termo/vigente`,
    ignoreHTTPSErrors: true,
    reuseExistingServer: true,
    timeout: 180_000,
  });
}

export default defineConfig({
  testDir: "./e2e",
  testMatch: /e2e-.*\.spec\.ts/,
  // Os fluxos compartilham a mesma igreja semeada e o mesmo banco: sequenciais.
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [
    ["list"],
    ["html", { open: "never", outputFolder: "playwright-report" }],
  ],
  outputDir: "test-results",
  globalSetup: "./e2e/global-setup.ts",
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    locale: "pt-BR",
    timezoneId: "America/Cuiaba",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    launchOptions: executavelChromium
      ? { executablePath: executavelChromium }
      : {},
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: servidores,
});
