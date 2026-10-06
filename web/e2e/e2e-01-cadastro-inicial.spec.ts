import { expect, test } from "@playwright/test";

import { SENHA_PADRAO, emailE2E } from "./apoio/semente";

// E2E-01 — Cadastro inicial com termo (CSU01 + CSU23; RF1, RNF 1.5, RNF 42.1, 42.7).
// Sem sessão, sem semente: a tela cria uma igreja nova a cada execução.
test("E2E-01 cadastro inicial: botão desabilitado até o aceite, termo com a igreja digitada, entra sem passar pelo /termo", async ({ page }) => {
  const id = `${Date.now().toString(36)}c`;
  const nomeIgreja = `E2E Cadastro ${id}`;
  const emailAdmin = emailE2E("admin.cadastro", id);

  await page.goto("/cadastro-inicial");

  // O termo vigente carrega antes de qualquer preenchimento (RNF 42.1).
  const termo = page.getByTestId("termo-uso-sigilo");
  await expect(termo.getByTestId("termo-texto")).toContainText("TERMO DE USO E SIGILO DO KOINONIAHUB");
  await expect(termo.getByTestId("termo-versao")).toContainText("Versão");
  await expect(termo.getByTestId("termo-igreja-nome")).toHaveText("[igreja em cadastro]");

  // Dados da igreja: o cabeçalho do termo acompanha o formulário (RNF 42.7).
  await page.getByTestId("cadastro-nome-igreja").fill(nomeIgreja);
  await page.getByTestId("cadastro-cidade").fill("Ji-Paraná");
  await page.getByTestId("cadastro-estado").fill("RO");
  await page.getByTestId("cadastro-email-igreja").fill(emailE2E("igreja.cadastro", id));
  await expect(termo.getByTestId("termo-igreja-nome")).toHaveText(nomeIgreja);
  await expect(termo.getByTestId("termo-igreja-contato")).toHaveText(emailE2E("igreja.cadastro", id));

  // Dados do administrador.
  await page.getByTestId("cadastro-nome-admin").fill("Admin do Cadastro E2E");
  await page.getByTestId("cadastro-email-admin").fill(emailAdmin);
  await page.getByTestId("cadastro-senha-admin").fill(SENHA_PADRAO);

  // Fluxo alternativo 2 do CSU01: sem o aceite, o botão de conclusão fica desabilitado.
  const concluir = page.getByTestId("cadastro-concluir");
  await expect(concluir).toBeDisabled();

  await page.getByText("Li e aceito o Termo de Uso e Sigilo do KoinoniaHub.").click();
  await expect(concluir).toBeEnabled();

  await concluir.click();

  // A API grava igreja, Admin e aceite (Meio = CadastroInicial) e abre a sessão: o
  // front entra direto no painel, sem a tela /termo (não há pendência).
  await page.waitForURL((url) => url.pathname === "/", { timeout: 20_000 });
  await expect(page.locator("h2.page-header-titulo")).toHaveText("Painel");
  await expect(page.locator(".header-usuario-nome")).toHaveText(emailAdmin);
  await expect(page.locator("a.sidebar-link")).not.toHaveCount(0);

  // RF43: Meus Dados mostra o aceite feito no cadastro inicial.
  await page.goto("/meus-dados");
  await expect(page.getByTestId("meus-dados-termo")).toContainText("Versão");
  await expect(page.getByTestId("meus-dados-termo")).toContainText("no cadastro inicial");
});
