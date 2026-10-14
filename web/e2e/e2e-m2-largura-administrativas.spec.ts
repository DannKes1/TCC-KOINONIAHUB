import { expect, test } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { criarAula, login, novoClienteApi } from "./apoio/api";
import { larguraDoDocumento } from "./apoio/celular";
import { ARQUIVO_ESTADO_ADMIN, lerSemente } from "./apoio/semente";

// E2E-M2 — Largura do documento a 360 px nas telas administrativas (ajuste da
// corretiva da 6.4-B; RNFs "a interface deve ser responsiva"; Plano, seção 9:
// administrativas "utilizáveis com rolagem"). Roda só no projeto "mobile". Critério:
// em Pessoas, Usuários, Igreja, Relatórios, Turmas EBD, Importação e nas quatro abas
// da turma, a largura do documento é a da janela (a página não desliza para o lado;
// o que for mais largo rola dentro do <main>). Chamada e Presenças entram com o mesmo
// critério, para garantir que a exceção da barra de ações não as alargou.
test.describe("E2E-M2 celular — largura do documento nas telas administrativas", () => {
  test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

  test("Pessoas, Usuários, Igreja, Relatórios, Turmas EBD e Importação", async ({ page }) => {
    const telas: [string, string, string][] = [
      ["Pessoas", "/pessoas", "Pessoas"],
      ["Usuários", "/usuarios", "Usuários"],
      ["Igreja", "/igreja", "Igreja"],
      ["Relatórios", "/relatorios/ebd", "Relatórios"],
      ["Turmas EBD", "/departamentos", "Turmas"],
    ];

    for (const [nome, rota, titulo] of telas) {
      await page.goto(rota);
      await expect(page.locator("h2.page-header-titulo")).toContainText(titulo);
      await expect(page.locator(".p-progress-spinner")).toHaveCount(0);
      await larguraDoDocumento(page, nome);
    }

    // Importação: diálogo aberto a partir de Pessoas.
    await page.goto("/pessoas");
    await page.getByRole("button", { name: "Importar CSV" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await larguraDoDocumento(page, "Importação (diálogo)");
  });

  test("abas da turma, Chamada e Presenças", async ({ page }) => {
    const { admin, turma, materiaId, professor } = lerSemente();

    for (const aba of ["matriculas", "materias", "aulas", "atribuicoes"]) {
      await page.goto(`/departamentos/${turma.id}/${aba}`);
      await expect(page.getByTestId(`turma-aba-${aba}`)).toBeVisible();
      await expect(page.locator(".p-progress-spinner")).toHaveCount(0);
      await larguraDoDocumento(page, `Turma — aba ${aba}`);
    }

    // Uma aula própria deste fluxo (não interfere na do E2E-M1).
    const api = await novoClienteApi(apiURL);
    await login(api, admin.email, admin.senha);
    const aula = await criarAula(api, {
      materiaId,
      professorId: professor.pessoaId,
      data: new Date("2026-02-01T12:00:00Z"),
      tema: "Largura E2E-M2",
    });

    await page.goto(`/aulas/${aula.id}/chamada`);
    await expect(page.getByTestId("chamada-linha").first()).toBeVisible();
    await larguraDoDocumento(page, "Chamada");

    await page.goto(`/aulas/${aula.id}/presencas`);
    await expect(page.getByTestId("presencas-indicadores")).toBeVisible();
    await expect(page.locator(".p-progress-spinner")).toHaveCount(0);
    await larguraDoDocumento(page, "Presenças");
  });
});
