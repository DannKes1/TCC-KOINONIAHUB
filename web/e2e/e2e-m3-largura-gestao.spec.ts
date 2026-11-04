import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

import { semRolagemHorizontal } from "./apoio/celular";
import { ARQUIVO_ESTADO_ADMIN, lerSemente } from "./apoio/semente";

// E2E-M3 — Ausência de rolagem horizontal nas telas de gestão (Entrega G; RNFs "a
// interface deve ser responsiva"). Roda só no projeto "mobile". Critério: a 360 px,
// em /termo, no Painel de gestão (tabelas Últimas aulas e Resumo por turma), em
// Usuários e em Atribuições, nem a página nem o <main> nem qualquer container interno
// rola para o lado — a exceção é a tabela, que rola dentro do próprio cartão
// (`estrito: false` libera só `.p-datatable-table-container`). O segundo caso repete
// Usuários e Atribuições com a janela em 1024 e 1200 px, onde os filtros ficavam
// espremidos: campo Buscar com largura útil e os dois botões inteiros dentro da janela.
// A espera pelo fim do carregamento usa o spinner que existe de fato (`pi-spin
// pi-spinner`, do LoadingOverlay e dos botões com `:loading`) e a primeira linha da
// tabela, que a semente garante (aulas e turmas no Painel, o Admin em Usuários, o
// professor em Atribuições).
test.describe("E2E-M3 celular — telas de gestão sem rolagem horizontal", () => {
  test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

  test("/termo, Painel, Usuários e Atribuições a 360 px", async ({ page }) => {
    const { turma } = lerSemente();

    // /termo com o aceite já registrado: texto para consulta, Voltar e Sair.
    await page.goto("/termo");
    await expect(page.getByTestId("termo-texto")).toBeVisible();
    await expect(page.getByRole("button", { name: "Voltar" })).toBeVisible();
    await semRolagemHorizontal(page, "/termo", { estrito: false });

    await page.goto("/");
    await expect(page.locator("h2.page-header-titulo")).toContainText("Painel");
    await expect(page.getByTestId("painel-gestao-tabelas")).toBeVisible();
    await carregado(page);
    await semRolagemHorizontal(page, "Painel de gestão", { estrito: false });

    await page.goto("/usuarios");
    await expect(page.locator("h2.page-header-titulo")).toContainText("Usuários");
    await carregado(page);
    await semRolagemHorizontal(page, "Usuários", { estrito: false });

    await page.goto(`/departamentos/${turma.id}/atribuicoes`);
    await expect(page.getByTestId("turma-aba-atribuicoes")).toBeVisible();
    await expect(page.getByTestId("atribuicoes-aplicar-filtros")).toBeVisible();
    await carregado(page);
    await semRolagemHorizontal(page, "Atribuições", { estrito: false });
  });

  test("Usuários e Atribuições a 1024 e 1200 px: filtros inteiros na janela", async ({ page }) => {
    const { turma } = lerSemente();

    for (const largura of [1024, 1200]) {
      await page.setViewportSize({ width: largura, height: 768 });

      await page.goto("/usuarios");
      await expect(page.locator("h2.page-header-titulo")).toContainText("Usuários");
      await carregado(page);
      await semRolagemHorizontal(page, `Usuários a ${largura} px`, { estrito: false });
      expect((await page.locator("#usuario-filtro-busca").boundingBox())!.width).toBeGreaterThanOrEqual(200);

      await page.goto(`/departamentos/${turma.id}/atribuicoes`);
      await expect(page.getByTestId("turma-aba-atribuicoes")).toBeVisible();
      await carregado(page);
      await semRolagemHorizontal(page, `Atribuições a ${largura} px`, { estrito: false });

      // O campo Buscar com largura útil (com o CSS anterior: 26 px a 1024 px e ~90 px a
      // 1200 px) e os dois botões dos filtros inteiros, lado a lado e dentro da janela.
      expect((await page.locator("#atribuicao-filtro-busca").boundingBox())!.width).toBeGreaterThanOrEqual(200);
      const aplicar = await page.getByTestId("atribuicoes-aplicar-filtros").boundingBox();
      const limpar = await page.getByTestId("atribuicoes-limpar-filtros").boundingBox();
      expect(aplicar).not.toBeNull();
      expect(limpar).not.toBeNull();
      expect(aplicar!.x + aplicar!.width).toBeLessThanOrEqual(largura);
      expect(limpar!.x + limpar!.width).toBeLessThanOrEqual(largura);
      expect(limpar!.x).toBeGreaterThanOrEqual(aplicar!.x + aplicar!.width);
      expect(Math.abs(limpar!.y - aplicar!.y)).toBeLessThan(2);
    }
  });
});

// Fim do carregamento: nenhum spinner (overlay ou botão) e a primeira linha da tabela.
async function carregado(page: Page) {
  await expect(page.locator(".pi-spin.pi-spinner")).toHaveCount(0);
  await expect(page.locator(".p-datatable-tbody tr").first()).toBeVisible();
}
