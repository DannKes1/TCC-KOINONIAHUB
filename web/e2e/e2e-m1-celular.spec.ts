import { expect, test } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { criarAula, login, novoClienteApi } from "./apoio/api";
import { alvoDeToque, evidenciaCelular, semRolagemHorizontal } from "./apoio/celular";
import {
  ARQUIVO_ESTADO_ALUNO,
  ARQUIVO_ESTADO_PROFESSOR,
  lerSemente,
} from "./apoio/semente";

// E2E-M1 — Fluxo do professor e do aluno no celular (Etapa 6.3; RNFs "a interface
// deve ser responsiva"; CSU10, CSU11, CSU07). Roda só no projeto "mobile" (360×640,
// toque). Critério: as telas do fluxo do professor — Login, Painel, Minhas Turmas,
// Aulas da turma, Chamada, Presenças da Aula, Meus Dados — e as do aluno — Painel,
// Minha Frequência — abrem sem rolagem horizontal da página, com o menu no Drawer e
// alvos de toque de pelo menos 40 px; na Chamada, Aluno e Presente ficam visíveis sem
// rolar. Login/Termo com termo pendente estão no E2E-02, que também roda no celular.

test.describe("E2E-M1 celular", () => {
  test("tela de login cabe em 360 px", async ({ page }, testInfo) => {
    await page.goto("/login");
    await expect(page.getByTestId("login-entrar")).toBeVisible();
    await alvoDeToque(await page.getByTestId("login-entrar").boundingBox(), "botão Entrar");
    await semRolagemHorizontal(page, "Login");
    await evidenciaCelular(page, testInfo, "login");
  });

  test.describe("professor", () => {
    test.use({ storageState: ARQUIVO_ESTADO_PROFESSOR });

    test("painel, menu em Drawer, turmas, aulas, chamada, presenças e meus dados", async ({ page }, testInfo) => {
      const semente = lerSemente();
      const { professor, turma, materiaId, alunos } = semente;

      // Painel do professor: barra lateral oculta, botão de menu visível.
      await page.goto("/");
      await expect(page.locator("h2.page-header-titulo")).toHaveText("Meu Painel");
      await expect(page.locator("aside.sidebar-desktop")).toBeHidden();
      const botaoMenu = page.getByTestId("botao-menu");
      await expect(botaoMenu).toBeVisible();
      await alvoDeToque(await botaoMenu.boundingBox(), "botão do menu");
      await semRolagemHorizontal(page, "Painel do professor");
      await evidenciaCelular(page, testInfo, "painel-professor");

      // O Drawer abre com os itens do perfil e fecha ao navegar.
      await botaoMenu.click();
      const drawer = page.getByTestId("menu-drawer");
      await expect(drawer).toBeVisible();
      // Espera a animação de entrada (o Drawer desliza da esquerda).
      await expect.poll(async () => (await drawer.boundingBox())?.x ?? -1).toBe(0);
      const linksDoDrawer = drawer.locator("a.sidebar-link");
      await expect(linksDoDrawer).toContainText(["Painel", "Minhas Turmas", "Meus Dados"]);
      await alvoDeToque(await linksDoDrawer.first().boundingBox(), "item do menu");
      await evidenciaCelular(page, testInfo, "menu-drawer");
      await drawer.locator("a.sidebar-link", { hasText: "Minhas Turmas" }).click();
      await page.waitForURL((url) => url.pathname === "/minhas-turmas");
      await expect(drawer).toBeHidden();

      // Minhas Turmas (cartões, Etapa 6.4) → Abrir turma → Página da Turma (RF20),
      // que abre na aba Aulas.
      const cartaoTurma = page.getByTestId("minhas-turmas-cartao").filter({ hasText: turma.nome });
      await expect(cartaoTurma).toBeVisible();
      await semRolagemHorizontal(page, "Minhas Turmas");
      await evidenciaCelular(page, testInfo, "minhas-turmas");
      const abrirTurma = cartaoTurma.getByTestId("minhas-turmas-abrir");
      await alvoDeToque(await abrirTurma.boundingBox(), "botão Abrir turma");
      await abrirTurma.click();
      await page.waitForURL((url) => url.pathname === `/departamentos/${turma.id}/aulas`);

      // Cabeçalho da turma: nome, contadores, equipe; abas Alunos/Matérias/Aulas
      // (Atribuições só para a gestão).
      await expect(page.locator("h2.page-header-titulo")).toHaveText(turma.nome);
      await expect(page.getByTestId("turma-total-alunos")).toHaveText(String(alunos.length));
      await expect(page.getByTestId("turma-equipe")).toContainText(professor.nome);
      await expect(page.getByTestId("turma-aba-matriculas")).toBeVisible();
      await expect(page.getByTestId("turma-aba-materias")).toBeVisible();
      await expect(page.getByTestId("turma-aba-aulas")).toBeVisible();
      await expect(page.getByTestId("turma-aba-atribuicoes")).toHaveCount(0);
      await alvoDeToque(await page.getByTestId("turma-aba-aulas").boundingBox(), "aba Aulas");

      // Aula Em aberto criada pela API como o próprio professor (RF31); a tela é o
      // objeto do teste, não o cadastro da aula.
      const api = await novoClienteApi(apiURL);
      let aulaId: number;
      try {
        await login(api, professor.email, professor.senha);
        ({ id: aulaId } = await criarAula(api, {
          materiaId,
          professorId: professor.pessoaId,
          data: new Date(),
          tema: "Aula E2E celular",
        }));
      } finally {
        await api.dispose();
      }

      await page.reload();
      await expect(page.locator("h2.page-header-titulo")).toHaveText(turma.nome);
      await expect(page.getByTestId("turma-total-aulas-abertas")).toHaveText("1");
      // Aba Aulas em cartões: a única aula, com a ação principal nomeada
      // ("Fazer chamada") visível sem rolar e o ⋮ com as secundárias.
      const cartaoAula = page.getByTestId("aula-cartao");
      await expect(cartaoAula).toHaveCount(1);
      await expect(cartaoAula).toContainText("Aula E2E celular");
      const fazerChamada = cartaoAula.getByTestId("aula-chamada");
      await expect(fazerChamada).toHaveText(/Fazer chamada/);
      await fazerChamada.scrollIntoViewIfNeeded();
      await alvoDeToque(await fazerChamada.boundingBox(), "botão Fazer chamada");
      await expect(cartaoAula.getByTestId("menu-acoes")).toBeVisible();
      await semRolagemHorizontal(page, "Aulas da turma");
      await evidenciaCelular(page, testInfo, "aulas-turma");

      // Chamada (CSU10): Aluno e Presente visíveis sem rolar; caixa com alvo de 40 px.
      await page.goto(`/aulas/${aulaId}/chamada`);
      const tabela = page.getByTestId("chamada-tabela");
      await expect(tabela).toBeVisible();
      await expect(tabela.locator("tbody tr")).toHaveCount(alunos.length);
      // A tabela fica abaixo da dobra; rolar na vertical é esperado. O que não pode
      // é precisar rolar na horizontal para ver o nome e marcar a presença.
      const celulaAluno = tabela.locator("td", { hasText: alunos[0]!.nome });
      await celulaAluno.scrollIntoViewIfNeeded();
      await expect(celulaAluno).toBeInViewport();
      const primeiraCaixa = tabela.getByTestId("chamada-presente").first();
      await expect(primeiraCaixa).toBeInViewport();
      await alvoDeToque(await primeiraCaixa.boundingBox(), "caixa Presente");
      await primeiraCaixa.click();
      await expect(primeiraCaixa.locator("input")).toBeChecked();
      await semRolagemHorizontal(page, "Chamada");
      await evidenciaCelular(page, testInfo, "chamada");

      const salvar = page.getByTestId("chamada-salvar");
      await alvoDeToque(await salvar.boundingBox(), "botão Salvar");
      await salvar.click();
      await expect(page.getByText("Chamada salva com sucesso.")).toBeVisible();

      // Presenças da Aula: todos os alunos com registro explícito (presente/ausente).
      await page.goto(`/aulas/${aulaId}/presencas`);
      await expect(page.locator("h2.page-header-titulo")).toContainText("Presenças");
      await expect(page.locator(".p-datatable tbody tr")).toHaveCount(alunos.length);
      await semRolagemHorizontal(page, "Presenças da Aula");
      await evidenciaCelular(page, testInfo, "presencas-aula");

      // Meus Dados.
      await page.goto("/meus-dados");
      await expect(page.getByTestId("meus-dados-termo")).toBeVisible();
      await semRolagemHorizontal(page, "Meus Dados");
      await evidenciaCelular(page, testInfo, "meus-dados");
    });
  });

  test.describe("aluno", () => {
    test.use({ storageState: ARQUIVO_ESTADO_ALUNO });

    test("painel do aluno e Minha Frequência", async ({ page }, testInfo) => {
      const { turma } = lerSemente();

      await page.goto("/");
      await expect(page.getByTestId("painel-aluno-indicadores")).toBeVisible();
      await semRolagemHorizontal(page, "Painel do aluno");
      await evidenciaCelular(page, testInfo, "painel-aluno");

      await page.goto("/minhas-turmas");
      const cartaoTurma = page.getByTestId("minhas-turmas-cartao").filter({ hasText: turma.nome });
      await expect(cartaoTurma).toBeVisible();
      // Aluno: a ação principal do cartão é Minha frequência (não há "Abrir turma").
      await expect(cartaoTurma.getByTestId("minhas-turmas-abrir")).toHaveCount(0);
      const minhaFrequencia = cartaoTurma.getByTestId("minhas-turmas-frequencia");
      await alvoDeToque(await minhaFrequencia.boundingBox(), "botão Minha frequência");
      await minhaFrequencia.click();
      await page.waitForURL(
        (url) => url.pathname === `/departamentos/${turma.id}/minha-frequencia`,
      );
      await expect(page.getByTestId("minha-frequencia-periodo")).toBeVisible();
      await semRolagemHorizontal(page, "Minha Frequência");
      await evidenciaCelular(page, testInfo, "minha-frequencia");
    });
  });
});
