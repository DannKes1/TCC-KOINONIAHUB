import { expect, test } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { login, novoClienteApi, obterChamada } from "./apoio/api";
import { ARQUIVO_ESTADO_ADMIN, lerSemente } from "./apoio/semente";

// E2E-06 — Reabertura pelo Admin (Plano 7.3; CSU11 FA3; RF33, RNFs 33.5/33.6). Desktop.
// A semente deixa uma aula Consolidada de uma semana atrás com A e B presentes e C
// ausente. O Admin reabre pela aba Aulas: a aula volta para Em aberto (e, com a data
// já ocorrida, pendente de fechamento), os registros continuam lá; ele corrige C e
// consolida de novo.

test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

test("E2E-06 reabertura: Admin reabre aula consolidada, presenças preservadas, corrige uma e consolida de novo", async ({ page }) => {
  const { admin, turmaReabertura: turma } = lerSemente();
  const [alunoA, alunoB, alunoC] = turma.alunos;

  await page.goto(`/departamentos/${turma.id}/aulas`);
  const linhaAula = page.locator("tr", { hasText: turma.tema });
  await expect(linhaAula.getByTestId("tag-situacao")).toHaveText("Consolidada");
  await expect(linhaAula.getByTestId("aula-chamada")).toHaveText(/Ver chamada/);

  // ⋮ → Reabrir aula → confirmação que promete preservar os registros.
  await linhaAula.getByTestId("menu-acoes").click();
  await page.getByRole("menuitem", { name: "Reabrir aula" }).click();
  const confirmacao = page.locator(".p-confirmdialog");
  await expect(confirmacao).toContainText("os registros de presença serão preservados");
  await confirmacao.getByRole("button", { name: "Reabrir" }).click();
  await expect(page.getByText("Aula reaberta. A situação voltou para Em aberto.")).toBeVisible();
  await expect(linhaAula.getByTestId("tag-situacao")).toHaveText("Em aberto");
  // Data já ocorrida: Em aberto e pendente de fechamento.
  await expect(linhaAula.getByTestId("tag-pendente")).toBeVisible();

  // Chamada reaberta: A e B presentes, C ausente, ninguém "sem registro".
  await linhaAula.getByTestId("aula-chamada").click();
  await page.waitForURL((url) => url.pathname === `/aulas/${turma.aulaId}/chamada`);
  const linhas = page.getByTestId("chamada-lista").getByTestId("chamada-linha");
  await expect(linhas).toHaveCount(3);
  const caixa = (nome: string) => linhas.filter({ hasText: nome }).getByTestId("chamada-presente").locator("input");
  await expect(caixa(alunoA!.nome)).toBeChecked();
  await expect(caixa(alunoB!.nome)).toBeChecked();
  await expect(caixa(alunoC!.nome)).not.toBeChecked();
  await expect(page.getByTestId("chamada-contador")).toHaveText("Presentes: 2 de 3");
  await expect(page.locator(".linha-chamada-sem-registro")).toHaveCount(0);
  await expect(caixa(alunoC!.nome)).toBeEnabled();

  // Correção: C passa a presente; Consolidar salva e consolida.
  await linhas.filter({ hasText: alunoC!.nome }).getByTestId("chamada-nome").click();
  await expect(page.getByTestId("chamada-contador")).toHaveText("Presentes: 3 de 3");
  await page.getByTestId("chamada-consolidar").click();
  await expect(confirmacao).toContainText("A chamada será salva e consolidada");
  await confirmacao.getByRole("button", { name: "Salvar e consolidar" }).click();
  await expect(page.getByText("Aula consolidada com sucesso.")).toBeVisible();
  await expect(page.getByTestId("tag-situacao").first()).toHaveText("Consolidada");
  await expect(caixa(alunoC!.nome)).toBeDisabled();
  await expect(caixa(alunoC!.nome)).toBeChecked();

  // Na API: Consolidada, os três registrados e presentes.
  const api = await novoClienteApi(apiURL);
  try {
    await login(api, admin.email, admin.senha);
    const aula = await (await api.get(`/api/aulas/${turma.aulaId}`)).json();
    expect(aula.situacao).toBe("Consolidada");
    const chamada = (await obterChamada(api, turma.aulaId)).sort((a, b) => a.nomeAluno.localeCompare(b.nomeAluno, "pt-BR"));
    expect(chamada.map((l) => [l.nomeAluno, l.registrado, l.presente])).toEqual([
      [alunoA!.nome, true, true],
      [alunoB!.nome, true, true],
      [alunoC!.nome, true, true],
    ]);
  } finally {
    await api.dispose();
  }
});
