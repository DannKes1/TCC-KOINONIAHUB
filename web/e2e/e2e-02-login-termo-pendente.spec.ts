import { expect, test } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { evidenciaCelular, semRolagemHorizontal } from "./apoio/celular";
import { contaPendentePara, lerSemente } from "./apoio/semente";

// E2E-02 — Login com termo pendente (CSU02 fluxo alternativo + CSU23; RF2, RNFs 2.5,
// 13.4, 42.1). A semente criou a conta com senha definida pelo Admin e sem aceite —
// uma por projeto do Playwright (desktop e celular), pois cada conta só aceita uma vez.
test("E2E-02 login com termo pendente: só a tela /termo abre, o aceite libera a sessão e fica registrado em Meus Dados", async ({ page }, testInfo) => {
  const semente = lerSemente();
  const { igreja } = semente;
  const pendente = contaPendentePara(testInfo.project.name, semente);

  await page.goto("/login");
  await page.getByTestId("login-email").fill(pendente.email);
  await page.locator("#login-senha").fill(pendente.senha);
  await page.getByTestId("login-entrar").click();

  // A sessão abre, mas a guarda de rota leva para /termo antes de qualquer tela.
  await page.waitForURL((url) => url.pathname === "/termo", { timeout: 20_000 });
  await expect(page.locator("a.sidebar-link")).toHaveCount(0);
  await expect(page.getByTestId("termo-igreja-nome")).toHaveText(igreja.nome);
  await expect(page.getByTestId("termo-igreja-contato")).toContainText(igreja.email);
  // Etapa 6.3: a tela do termo também cabe na janela (no celular vira evidência).
  await semRolagemHorizontal(page, "Termo");
  await evidenciaCelular(page, testInfo, "termo");

  // Tentar outra rota pela barra de endereço volta para /termo (RNF 2.5).
  await page.goto("/meus-dados");
  await page.waitForURL((url) => url.pathname === "/termo");
  await expect(page.locator("a.sidebar-link")).toHaveCount(0);

  // A API também recusa: 403 com termoPendente (filtro global, decisão da 5.1).
  // Chamada direta à API com o cookie da sessão do navegador.
  const bloqueio = await page.request.get(`${apiURL}/api/meus-dados`);
  expect(bloqueio.status()).toBe(403);
  expect(await bloqueio.json()).toMatchObject({ termoPendente: true });

  // Botão desabilitado até marcar (RNF 42.1); o aceite libera na mesma sessão.
  const aceitar = page.getByTestId("botao-aceitar-termo");
  await expect(aceitar).toBeDisabled();
  await page.getByText("Li e aceito o Termo de Uso e Sigilo do KoinoniaHub.").click();
  await expect(aceitar).toBeEnabled();
  await aceitar.click();

  // Volta para o destino que estava na query (/meus-dados), já com o menu.
  await page.waitForURL((url) => url.pathname === "/meus-dados", { timeout: 20_000 });
  await expect(page.locator("a.sidebar-link")).not.toHaveCount(0);

  // RF43 / RNF 42.5: aceite registrado com Meio = Login.
  await expect(page.getByTestId("meus-dados-termo")).toContainText("após o login");

  // O painel abre normalmente e a API responde 200 na mesma sessão.
  await page.goto("/");
  await expect(page.locator("h2.page-header-titulo")).toContainText("Painel");
  expect((await page.request.get(`${apiURL}/api/meus-dados`)).status()).toBe(200);
});
