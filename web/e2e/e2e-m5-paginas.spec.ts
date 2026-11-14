import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

import { problemasDeLargura } from "./apoio/celular";
import {
  ARQUIVO_ESTADO_ADMIN,
  ARQUIVO_ESTADO_ALUNO,
  ARQUIVO_ESTADO_PROFESSOR,
  lerSemente,
} from "./apoio/semente";

// E2E-M5 — Páginas dentro da tela a 360 × 640, sem diálogo aberto (Corretiva G5; RNFs "a
// interface deve ser responsiva"). Roda só no projeto "mobile". Em cada página confere
// `problemasDeLargura` (documento, deslocamento lateral, <main>, nada passando da borda;
// só o cartão da tabela e a faixa de abas rolam por dentro) com a página parada e com a
// lista de cada Select visível aberta, um de cada vez. Motivo: em Pessoas o filtro
// Situação passava da borda e, ao abrir a lista, a página inteira rolava para o lado.
// Páginas: Painel do Admin, do Professor e do aluno (Usuario); Pessoas; Turmas; Matérias,
// Aulas, Matrículas e Atribuições da turma; Usuários; Relatórios nas quatro abas; Igreja;
// Meus Dados; Chamada (aula semeada da turma de reabertura; a chamada da turma principal é
// conferida no modo estrito pelo E2E-M1).

async function carregado(page: Page) {
  await expect(page.locator(".pi-spin.pi-spinner")).toHaveCount(0);
  await page.waitForTimeout(300);
}

async function conferirPagina(page: Page, tela: string) {
  await carregado(page);
  expect.soft(await problemasDeLargura(page), `${tela}`).toEqual([]);

  // Cada Select visível da página (fora de diálogo), com a lista aberta.
  const selects = page.locator("main .p-select:not(.p-disabled)");
  const total = await selects.count();
  for (let i = 0; i < total; i++) {
    const select = selects.nth(i);
    if (!(await select.isVisible())) continue;
    const rotulo = `select ${i + 1}: ${(await select.innerText()).trim()}`;
    await select.click();
    await expect(page.locator(".p-select-overlay")).toBeVisible();
    await page.waitForTimeout(250);
    expect.soft(await problemasDeLargura(page), `${tela}, lista aberta (${rotulo})`).toEqual([]);
    await page.keyboard.press("Escape");
    const fechou = await expect(page.locator(".p-select-overlay"))
      .toHaveCount(0, { timeout: 1500 })
      .then(
        () => true,
        () => false,
      );
    if (!fechou) await select.click();
    await expect(page.locator(".p-select-overlay")).toHaveCount(0);
  }
}

test.describe("E2E-M5 celular — páginas dentro da tela", () => {
  test.describe("Admin", () => {
    test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

    test("Painel, cadastros, turma, Usuários, Relatórios, Igreja, Meus Dados e Chamada a 360 px", async ({ page }) => {
      test.setTimeout(180_000);
      const { turma, turmaReabertura } = lerSemente();

      await page.goto("/");
      await expect(page.getByTestId("painel-gestao-tabelas")).toBeVisible();
      await conferirPagina(page, "Painel do Admin");

      for (const [rota, tela] of [
        ["/pessoas", "Pessoas"],
        ["/departamentos", "Turmas"],
        [`/departamentos/${turma.id}/materias`, "Matérias"],
        [`/departamentos/${turma.id}/aulas`, "Aulas"],
        [`/departamentos/${turma.id}/matriculas`, "Matrículas"],
        [`/departamentos/${turma.id}/atribuicoes`, "Atribuições"],
        ["/usuarios", "Usuários"],
        ["/igreja", "Igreja"],
        ["/meus-dados", "Meus Dados"],
        [`/aulas/${turmaReabertura.aulaId}/chamada`, "Chamada"],
      ]) {
        await page.goto(rota);
        await conferirPagina(page, tela);
      }

      // Ajuste A4: a 360 px, o cartão da aula consolidada do Admin tem o botão "Ver
      // presenças" em uma linha e o ⋮ (Reabrir aula) ao lado, na mesma altura.
      await page.goto(`/departamentos/${turmaReabertura.id}/aulas`);
      const cartao = page.getByTestId("aula-cartao").filter({ hasText: turmaReabertura.tema });
      const botao = cartao.getByTestId("aula-chamada");
      await expect(botao).toHaveText(/^\s*Ver presenças\s*$/);
      const caixaBotao = (await botao.boundingBox())!;
      const caixaMenu = (await cartao.getByTestId("menu-acoes").boundingBox())!;
      expect(caixaBotao.height, "botão em uma linha a 360 px").toBeLessThanOrEqual(44);
      // Botão e ⋮ centrados na mesma linha. Antes do ajuste A4, a diferença de centros
      // medida era 0 px a 1366, 1280 e 360 px; 1 px de folga para arredondamento.
      const diferencaCentros = Math.abs(
        caixaBotao.y + caixaBotao.height / 2 - (caixaMenu.y + caixaMenu.height / 2),
      );
      expect(diferencaCentros, "⋮ na mesma linha do botão a 360 px").toBeLessThanOrEqual(1);
      await conferirPagina(page, "Aulas com aula consolidada");

      await page.goto(`/relatorios/ebd?departamentoId=${turma.id}`);
      for (const aba of ["Frequência da turma", "Acompanhamento", "Ranking de faltas", "Resumo do dia"]) {
        await page.getByRole("tab", { name: aba }).click();
        await conferirPagina(page, `Relatórios, ${aba}`);
      }
    });
  });

  test.describe("Professor", () => {
    test.use({ storageState: ARQUIVO_ESTADO_PROFESSOR });

    test("Painel do Professor a 360 px", async ({ page }) => {
      await page.goto("/");
      await expect(page.locator("h2.page-header-titulo")).toContainText("Painel");
      await conferirPagina(page, "Painel do Professor");
    });
  });

  test.describe("Usuario (aluno)", () => {
    test.use({ storageState: ARQUIVO_ESTADO_ALUNO });

    test("Painel do aluno a 360 px", async ({ page }) => {
      await page.goto("/");
      await expect(page.locator("h2.page-header-titulo")).toContainText("Painel");
      await conferirPagina(page, "Painel do aluno");
    });
  });
});
