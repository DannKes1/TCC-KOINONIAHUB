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
//
// Projetos (Etapa 6.3, responsividade — RNFs "a interface deve ser responsiva"):
//   chromium  desktop; todos os fluxos E2E-NN.
//   mobile    emulação de celular (Pixel 7: toque, user agent móvel) com a viewport
//             reduzida a 360×640, o menor tamanho do critério da Etapa 6.3; roda os
//             fluxos E2E-01 e E2E-02 e os específicos de celular (e2e-mN-*). O E2E-02 usa
//             uma conta pendente própria por projeto, porque cada conta só pode
//             aceitar o termo uma vez por semente; o E2E-03 fica fora porque o
//             convite da pessoa semeada só pode ser gerado uma vez.
//   `npx playwright test --project=mobile` roda só o celular.

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
  projects: [
    {
      name: "chromium",
      testMatch: /e2e-\d+-.*\.spec\.ts/,
      use: { ...devices["Desktop Chrome"] },
    },
    {
      name: "mobile",
      testMatch: /(e2e-0[12]-|e2e-m\d+-).*\.spec\.ts/,
      use: { ...devices["Pixel 7"], viewport: { width: 360, height: 640 } },
    },
  ],
  webServer: servidores,
});
