import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

import { evidenciaCelular, larguraDoDocumento, semRolagemHorizontal } from "./apoio/celular";
import { ARQUIVO_ESTADO_PROFESSOR, lerSemente, turmaChamadaPara } from "./apoio/semente";

// E2E-04 — Chamada até o relatório (Plano 7.3; CSU09, CSU10, CSU11, CSU15). Roda no
// desktop e no celular, cada projeto com a sua turma (semente, Etapa 7.2). O professor
// cria a aula pela tela, lança uma chamada incompleta e aciona Consolidar: a
// confirmação nomeia os alunos sem registro e oferece "Salvar e consolidar" (CSU11
// passos 2–4, comportamento desde a 6.4-C). Ele cancela, completa e salva a chamada,
// consolida, e a aula entra na Frequência da turma (só Consolidadas contam).
// No celular: critério estrito nas telas operacionais (Aulas e Chamada) e o das
// administrativas em Relatórios (Plano 6.12, larguraDoDocumento), com os prints
// RNF-responsividade-e2e04-*.png.

test.use({ storageState: ARQUIVO_ESTADO_PROFESSOR });

// Data de hoje no fuso dos testes (playwright.config.ts), como o Calendar mostra.
function hojeNoFusoDosTestes() {
  return new Date().toLocaleDateString("pt-BR", { timeZone: "America/Cuiaba" });
}

async function escolherOpcao(page: Page, campo: string, opcao: string) {
  await page.getByTestId(campo).click();
  await page.getByRole("option", { name: opcao, exact: true }).click();
}

test("E2E-04 chamada até relatório: consolidar com aluno sem registro nomeia quem falta; depois de completar, consolida e a aula entra na Frequência da turma", async ({ page }, testInfo) => {
  const semente = lerSemente();
  const { professor } = semente;
  const turma = turmaChamadaPara(testInfo.project.name, semente);
  const [alunoA, alunoB, alunoC] = turma.alunos;
  const celular = testInfo.project.name === "mobile";
  const tema = `Chamada até relatório (${testInfo.project.name})`;

  // Minhas Turmas → Abrir turma → Página da Turma, aba Aulas (sem nenhuma aula).
  await page.goto("/minhas-turmas");
  const linhaTurma = celular
    ? page.getByTestId("minhas-turmas-cartao").filter({ hasText: turma.nome })
    : page.locator("tr", { hasText: turma.nome });
  await linhaTurma.getByTestId("minhas-turmas-abrir").click();
  await page.waitForURL((url) => url.pathname === `/departamentos/${turma.id}/aulas`);
  await expect(page.locator("h2.page-header-titulo")).toHaveText(turma.nome);
  await expect(page.getByTestId("aula-chamada")).toHaveCount(0);

  // CSU09: Nova Aula com data, matéria, professor e tema.
  await page.getByTestId("aulas-nova").click();
  const dialogo = page.getByTestId("aula-dialogo");
  await expect(dialogo).toBeVisible();
  // Data pelo próprio calendário: o dia de hoje (selecionar fecha o painel, que no
  // desktop cobre os campos de baixo).
  await page.locator("#aula-data").click();
  const painelData = page.locator(".p-datepicker-panel");
  await painelData.locator(".p-datepicker-today .p-datepicker-day").click();
  await expect(painelData).toBeHidden();
  await expect(page.locator("#aula-data")).toHaveValue(hojeNoFusoDosTestes());
  await page.getByTestId("aula-tema").fill(tema);
  await escolherOpcao(page, "aula-materia", `Matéria ${celular ? "Chamada Celular" : "Chamada"} E2E`);
  await escolherOpcao(page, "aula-professor", professor.nome);
  await evidenciaCelular(page, testInfo, "e2e04-aula-nova");
  await page.getByTestId("aula-salvar").click();
  await expect(page.getByText("Aula criada com sucesso.")).toBeVisible();
  await expect(dialogo).toBeHidden();

  const fazerChamada = page.getByTestId("aula-chamada");
  await expect(fazerChamada).toHaveCount(1);
  await expect(fazerChamada).toHaveText(/Fazer chamada/);
  await expect(page.getByText(tema)).toBeVisible();
  if (celular) {
    await semRolagemHorizontal(page, "E2E-04 — Aulas da turma");
    await evidenciaCelular(page, testInfo, "e2e04-aulas-turma");
  }

  // CSU10: chamada da aula recém-criada, todos sem registro.
  await fazerChamada.click();
  await page.waitForURL((url) => /^\/aulas\/\d+\/chamada$/.test(url.pathname));
  const lista = page.getByTestId("chamada-lista");
  const linhas = lista.getByTestId("chamada-linha");
  await expect(linhas).toHaveCount(3);
  await expect(page.getByTestId("chamada-contador")).toHaveText("Presentes: 0 de 3");
  await expect(page.getByTestId("chamada-mensagem")).toContainText("3 alunos sem registro");

  // Chamada incompleta: só o aluno A presente.
  await linhas.filter({ hasText: alunoA!.nome }).getByTestId("chamada-nome").click();
  await expect(page.getByTestId("chamada-contador")).toHaveText("Presentes: 1 de 3");
  await expect(page.getByTestId("chamada-mensagem")).toContainText("2 alunos sem registro");
  if (celular) await semRolagemHorizontal(page, "E2E-04 — Chamada incompleta");

  // CSU11 passos 2–4: a confirmação nomeia B e C (serão gravados como ausentes) e
  // oferece "Salvar e consolidar". O professor cancela.
  await page.getByTestId("chamada-consolidar").click();
  const confirmacao = page.locator(".p-confirmdialog");
  await expect(confirmacao).toContainText("Os 2 alunos abaixo estão sem registro e serão gravados como ausentes");
  const nomes = confirmacao.getByTestId("confirm-lista");
  await expect(nomes.locator("li")).toHaveCount(2);
  await expect(nomes).toContainText(alunoB!.nome);
  await expect(nomes).toContainText(alunoC!.nome);
  await expect(nomes).not.toContainText(alunoA!.nome);
  await expect(confirmacao.getByRole("button", { name: "Salvar e consolidar" })).toBeVisible();
  await evidenciaCelular(page, testInfo, "e2e04-chamada-incompleta-confirmacao");
  await confirmacao.getByRole("button", { name: "Cancelar" }).click();
  await expect(confirmacao).toBeHidden();
  await expect(page.getByTestId("tag-situacao").first()).toHaveText("Em aberto");

  // Completa: B presente; ao salvar, C fica registrado como ausente (registro explícito).
  await linhas.filter({ hasText: alunoB!.nome }).getByTestId("chamada-nome").click();
  await page.getByTestId("chamada-salvar").click();
  await expect(page.getByText("Chamada salva com sucesso.")).toBeVisible();
  await expect(page.getByTestId("chamada-contador")).toHaveText("Presentes: 2 de 3");
  await expect(lista.locator(".linha-chamada-sem-registro")).toHaveCount(0);
  await expect(page.getByTestId("chamada-mensagem")).toHaveCount(0);
  if (celular) {
    await semRolagemHorizontal(page, "E2E-04 — Chamada completa");
    await evidenciaCelular(page, testInfo, "e2e04-chamada-completa");
  }

  // Consolidar com a chamada completa e salva: confirmação simples, sem lista.
  await page.getByTestId("chamada-consolidar").click();
  await expect(confirmacao).toContainText("Ao consolidar, a chamada não poderá mais ser alterada");
  await expect(confirmacao.getByTestId("confirm-lista")).toHaveCount(0);
  await confirmacao.getByRole("button", { name: "Consolidar", exact: true }).click();
  await expect(page.getByText("Aula consolidada com sucesso.")).toBeVisible();
  await expect(page.getByTestId("tag-situacao").first()).toHaveText("Consolidada");
  await expect(page.getByTestId("barra-acoes")).toHaveCount(0);
  await expect(page.getByTestId("chamada-mensagem")).toContainText("somente leitura");
  if (celular) {
    await semRolagemHorizontal(page, "E2E-04 — Chamada consolidada");
    await evidenciaCelular(page, testInfo, "e2e04-chamada-consolidada");
  }

  // CSU15: Frequência da turma conta a aula consolidada (1 aula, 2 presenças).
  await page.goto("/relatorios/ebd");
  await expect(page.locator("h2.page-header-titulo")).toContainText("Relatórios");
  // Espera a carga da turma pré-selecionada antes de trocar, para a resposta dela
  // não chegar depois e sobrescrever a da turma escolhida.
  await expect(page.getByTestId("relatorio-total-aulas")).toBeVisible();
  await escolherOpcao(page, "relatorio-turma", turma.nome);
  await expect(page.getByTestId("relatorio-turma")).toContainText(turma.nome);
  await expect(page.getByTestId("relatorio-total-aulas").locator(".card-indicador-valor")).toHaveText("1");
  await expect(page.getByTestId("relatorio-total-presentes").locator(".card-indicador-valor")).toHaveText("2");
  await expect(page.getByTestId("relatorio-frequencia-aulas")).toContainText(tema);
  if (celular) {
    await expect(page.locator(".p-progress-spinner")).toHaveCount(0);
    await larguraDoDocumento(page, "E2E-04 — Relatórios (Frequência da turma)");
    await evidenciaCelular(page, testInfo, "e2e04-relatorio-frequencia");
  }
});
