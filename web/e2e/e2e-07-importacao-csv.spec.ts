import { expect, test } from "@playwright/test";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { apiURL } from "./apoio/ambiente";
import { login, novoClienteApi } from "./apoio/api";
import { ARQUIVO_ESTADO_ADMIN, PESSOA_JA_CADASTRADA, lerSemente } from "./apoio/semente";

// E2E-07 — Importação CSV (Plano 7.3; CSU16; RF41, RNF 41.3). Desktop. A fixture tem
// quatro linhas: uma nova com e-mail (criada), uma com o e-mail da pessoa semeada
// (ignorada), uma com o nome da pessoa semeada e sem e-mail (ignorada e sinalizada
// para conferência) e uma nova sem e-mail (criada).

const ARQUIVO_CSV = resolve(dirname(fileURLToPath(import.meta.url)), "fixtures", "importacao-pessoas.csv");

test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

test("E2E-07 importação CSV: criadas, e-mail repetido ignorado e nome repetido sem e-mail sinalizado para conferência", async ({ page }) => {
  const { admin } = lerSemente();

  await page.goto("/pessoas");
  await page.getByRole("button", { name: "Importar CSV" }).click();
  const dialogo = page.getByRole("dialog", { name: "Importar pessoas (CSV)" });
  await expect(dialogo).toBeVisible();

  await dialogo.getByTestId("importacao-arquivo").setInputFiles(ARQUIVO_CSV);
  await dialogo.getByTestId("importacao-enviar").click();

  await expect(dialogo.getByTestId("importacao-criadas")).toHaveText("Criadas: 2");
  await expect(dialogo.getByTestId("importacao-ignoradas")).toHaveText("Ignoradas: 2");
  await expect(dialogo.getByTestId("importacao-erros")).toHaveText("Erros: 0");
  await expect(dialogo.getByTestId("importacao-para-conferencia")).toHaveText("Para conferência: 1");
  await expect(dialogo).toContainText("Linhas lidas: 4");
  await expect(page.getByText("2 pessoa(s) importada(s) com sucesso.")).toBeVisible();

  // RNF 41.3: a linha para conferência vem primeiro, destacada e com a tag Conferir.
  const linhas = dialogo.locator("tbody tr");
  await expect(linhas).toHaveCount(4);
  await expect(linhas.first()).toContainText(PESSOA_JA_CADASTRADA.nome);
  await expect(linhas.first()).toContainText("Conferir");
  await expect(linhas.first()).toHaveClass(/linha-conferencia/);
  await expect(linhas.filter({ hasText: "Outro Nome Mesmo Email E2E" })).toContainText("Já existe uma pessoa com este e-mail");
  await expect(linhas.filter({ hasText: "Importada Com Email E2E" })).toContainText("Criado");
  await expect(linhas.filter({ hasText: "Importada Sem Email E2E" })).toContainText("Criado");
  await expect(dialogo.locator("tr.linha-conferencia")).toHaveCount(1);

  // O mesmo arquivo de novo: nada é criado; as duas linhas sem e-mail agora pedem conferência.
  await dialogo.getByTestId("importacao-arquivo").setInputFiles([]);
  await dialogo.getByTestId("importacao-arquivo").setInputFiles(ARQUIVO_CSV);
  await dialogo.getByTestId("importacao-enviar").click();
  await expect(dialogo.getByTestId("importacao-criadas")).toHaveText("Criadas: 0");
  await expect(dialogo.getByTestId("importacao-ignoradas")).toHaveText("Ignoradas: 4");
  await expect(dialogo.getByTestId("importacao-para-conferencia")).toHaveText("Para conferência: 2");

  await dialogo.getByRole("button", { name: "Fechar" }).click();
  await expect(dialogo).toBeHidden();

  // Cadastro: as duas criadas uma vez só; a linha de e-mail repetido não virou pessoa.
  const api = await novoClienteApi(apiURL);
  try {
    await login(api, admin.email, admin.senha);
    const nomes = ((await (await api.get("/api/pessoas")).json()) as { nome: string }[]).map((p) => p.nome);
    const contar = (nome: string) => nomes.filter((n) => n === nome).length;
    expect(contar("Importada Com Email E2E")).toBe(1);
    expect(contar("Importada Sem Email E2E")).toBe(1);
    expect(contar("Outro Nome Mesmo Email E2E")).toBe(0);
    expect(contar(PESSOA_JA_CADASTRADA.nome)).toBe(1);
  } finally {
    await api.dispose();
  }
});
