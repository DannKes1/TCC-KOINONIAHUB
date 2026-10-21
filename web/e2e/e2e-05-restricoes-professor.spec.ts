import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { login, novoClienteApi } from "./apoio/api";
import { ARQUIVO_ESTADO_PROFESSOR, lerSemente } from "./apoio/semente";

// E2E-05 — Restrições do Professor (Plano 7.3; CSU16, CSU15, CSU18; RNFs 21.4, 35.4,
// 36.4, 37.4). Desktop. O menu é só conveniência: as negativas também são conferidas
// direto na API (403), como a banca pode pedir.

const linksDoMenu = (page: Page) => page.locator("aside.sidebar-desktop a.sidebar-link");

test.describe("E2E-05 professor", () => {
  test.use({ storageState: ARQUIVO_ESTADO_PROFESSOR });

  test("menu sem Pessoas, Usuários e Igreja; GET /api/pessoas/{id} de pessoa fora das suas turmas → 403", async ({ page }) => {
    const { professor, foraDaTurma, turmasChamada } = lerSemente();

    await page.goto("/");
    await expect(page.locator("h2.page-header-titulo")).toHaveText("Meu Painel");
    await expect(linksDoMenu(page)).toHaveText(["Painel", "Minhas Turmas", "Meus Dados", "Relatórios EBD", "Turmas EBD"]);

    // A rota administrativa digitada à mão não abre.
    await page.goto("/pessoas");
    await page.waitForURL((url) => url.pathname === "/");
    await page.goto("/usuarios");
    await page.waitForURL((url) => url.pathname === "/");

    // RNF 21.x: o professor consulta só alunos das turmas em que tem atribuição ativa.
    const api = await novoClienteApi(apiURL);
    try {
      await login(api, professor.email, professor.senha);

      const fora = await api.get(`/api/pessoas/${foraDaTurma.pessoaId}`);
      expect(fora.status(), "pessoa fora das turmas do professor").toBe(403);
      expect((await fora.json()).mensagem).toContain("turmas em que possui atribuição ativa");

      // Controle: aluno de uma turma do professor → 200 (visão reduzida).
      const daTurma = await api.get(`/api/pessoas/${turmasChamada.desktop.alunos[0]!.pessoaId}`);
      expect(daTurma.status(), "aluno de turma do professor").toBe(200);

      // A listagem geral de Pessoas é administrativa.
      expect((await api.get("/api/pessoas")).status(), "GET /api/pessoas").toBe(403);
    } finally {
      await api.dispose();
    }
  });

  test("Relatórios sem CSV e sem Imprimir em Frequência, Acompanhamento e Ranking", async ({ page }) => {
    const { turmaReabertura } = lerSemente();

    await page.goto("/relatorios/ebd");
    await expect(page.locator("h2.page-header-titulo")).toContainText("Relatórios");
    await expect(page.getByTestId("relatorio-total-aulas")).toBeVisible();

    // Turma com aula consolidada (A e B presentes, C ausente): as três abas têm tabela.
    await page.getByTestId("relatorio-turma").click();
    await page.getByRole("option", { name: turmaReabertura.nome, exact: true }).click();
    await expect(page.getByTestId("relatorio-total-aulas").locator(".card-indicador-valor")).toHaveText("1");
    await expect(page.getByTestId("relatorio-frequencia-aulas")).toContainText(turmaReabertura.tema);

    // RNFs 35.4/36.4/37.4: nenhum botão de exportação, nem no cabeçalho (Imprimir).
    await expect(page.getByTestId("botoes-exportacao")).toHaveCount(0);
    await expect(page.getByTestId("botao-csv")).toHaveCount(0);
    await expect(page.getByTestId("botao-imprimir")).toHaveCount(0);

    await page.getByRole("tab", { name: "Acompanhamento" }).click();
    await expect(page.getByText("Alunos em atenção")).toBeVisible();
    await expect(page.getByTestId("botoes-exportacao")).toHaveCount(0);

    await page.getByRole("tab", { name: "Ranking de faltas" }).click();
    await expect(page.getByText("Detalhamento")).toBeVisible();
    await expect(page.getByTestId("botoes-exportacao")).toHaveCount(0);

    // Resumo do dia é só da gestão.
    await expect(page.getByRole("tab", { name: "Resumo do dia" })).toHaveCount(0);
  });
});

test("E2E-05 conta de perfil Usuario com atribuição ativa: 403 na API e Minhas Turmas sem Abrir turma (RNF 21.4)", async ({ page }) => {
  const { usuarioComAtribuicao: conta, turmaSemProfessor } = lerSemente();

  // API: a atribuição (Auxiliar, ativa) não dá acesso à operação da turma.
  const api = await novoClienteApi(apiURL);
  try {
    await login(api, conta.email, conta.senha);

    const aulas = await api.get(`/api/aulas?departamentoId=${turmaSemProfessor.id}`);
    expect(aulas.status(), "GET /api/aulas da turma com atribuição").toBe(403);
    expect((await aulas.json()).mensagem).toBe("Esta operação exige perfil Professor ou administrativo.");

    const vinculos = await (await api.get("/api/meus-dados/minhas-turmas")).json();
    expect(vinculos, "a turma aparece só como Aluno").toEqual([
      expect.objectContaining({ departamentoId: turmaSemProfessor.id, vinculo: "Aluno" }),
    ]);
  } finally {
    await api.dispose();
  }

  // Tela: login da própria conta (termo já aceito pela semente).
  await page.goto("/login");
  await page.getByTestId("login-email").fill(conta.email);
  await page.locator("#login-senha").fill(conta.senha);
  await page.getByTestId("login-entrar").click();
  await page.waitForURL((url) => url.pathname === "/", { timeout: 20_000 });

  await expect(linksDoMenu(page)).toHaveText(["Painel", "Minhas Turmas", "Meus Dados"]);

  await page.goto("/minhas-turmas");
  const linha = page.locator("tr", { hasText: turmaSemProfessor.nome });
  await expect(linha).toContainText("Aluno");
  await expect(linha.getByTestId("minhas-turmas-frequencia")).toBeVisible();
  await expect(page.getByTestId("minhas-turmas-abrir")).toHaveCount(0);

  // A Página da Turma digitada à mão volta para Minhas Turmas com o aviso (7.1).
  await page.goto(`/departamentos/${turmaSemProfessor.id}/aulas`);
  await page.waitForURL((url) => url.pathname === "/minhas-turmas");
  await expect(page.getByText("Sua conta não tem perfil para operar turmas.", { exact: false })).toBeVisible();
});
