import { expect, test } from "@playwright/test";

import { apiURL, baseURL } from "./apoio/ambiente";
import { criarUsuario, login, novoClienteApi } from "./apoio/api";
import { SENHA_PADRAO, lerSemente } from "./apoio/semente";

// E2E-03 — Convite e ativação (CSU22 + CSU23; RF39, RF40, RNFs 40.1, 40.5, 42.1,
// 42.7). O Admin gera o convite pela API (RF39 está coberto por xUnit e teste manual);
// o que a banca vê é a tela pública de primeiro acesso com o termo da igreja do convite.
test("E2E-03 convite: tela pública mostra o termo da igreja, exige o aceite para definir a senha e o login entra sem pendência", async ({ page, browser }) => {
  const semente = lerSemente();
  const { convidado, admin, igreja } = semente;

  // Admin gera o convite para a pessoa sem usuário (token em claro só nesta resposta).
  const api = await novoClienteApi(apiURL);
  let token: string | null;
  try {
    await login(api, admin.email, admin.senha);
    ({ conviteToken: token } = await criarUsuario(api, {
      pessoaId: convidado.pessoaId,
      email: convidado.email,
      perfil: "Usuario",
    }));
  } finally {
    await api.dispose();
  }
  expect(token, "a API deve devolver o token do convite na criação sem senha").toBeTruthy();

  // Tela pública, sem sessão.
  await page.goto(`/primeiro-acesso?token=${encodeURIComponent(token!)}`);
  await expect(page.getByText(convidado.nome)).toBeVisible();

  // O termo vem com a igreja do convite (GET vigente?token=), antes de qualquer login.
  const termo = page.getByTestId("termo-uso-sigilo");
  await expect(termo.getByTestId("termo-igreja-nome")).toHaveText(igreja.nome);
  await expect(termo.getByTestId("termo-igreja-contato")).toContainText(igreja.email);

  await page.locator("#primeiro-acesso-senha").fill(SENHA_PADRAO);
  await page.locator("#primeiro-acesso-confirmar").fill(SENHA_PADRAO);

  // RNF 40.5 / 42.1: sem o aceite não conclui.
  const concluir = page.getByTestId("primeiro-acesso-concluir");
  await expect(concluir).toBeDisabled();
  await page.getByText("Li e aceito o Termo de Uso e Sigilo do KoinoniaHub.").click();
  await expect(concluir).toBeEnabled();
  await concluir.click();

  await expect(page.getByTestId("primeiro-acesso-sucesso")).toBeVisible({ timeout: 20_000 });

  // RNF 40.1: o mesmo link não serve de novo.
  const outroContexto = await browser.newContext({ baseURL, ignoreHTTPSErrors: true });
  try {
    const outraAba = await outroContexto.newPage();
    await outraAba.goto(`/primeiro-acesso?token=${encodeURIComponent(token!)}`);
    await expect(outraAba.locator(".login-erro")).toContainText(/inválido|utilizado|expirado/i);
  } finally {
    await outroContexto.close();
  }

  // Login com a senha recém-definida: entra direto, sem /termo (aceite já registrado).
  await page.getByTestId("primeiro-acesso-ir-login").click();
  await page.waitForURL((url) => url.pathname === "/login");
  await page.getByTestId("login-email").fill(convidado.email);
  await page.locator("#login-senha").fill(SENHA_PADRAO);
  await page.getByTestId("login-entrar").click();

  await page.waitForURL((url) => url.pathname === "/", { timeout: 20_000 });
  await expect(page.locator("h2.page-header-titulo")).toContainText("Painel");

  // RF43 / RNF 42.5: Meio = PrimeiroAcesso.
  await page.goto("/meus-dados");
  await expect(page.getByTestId("meus-dados-termo")).toContainText("no primeiro acesso");
});
