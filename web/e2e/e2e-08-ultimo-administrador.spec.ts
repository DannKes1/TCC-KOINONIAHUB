import { expect, test } from "@playwright/test";
import type { Locator, Page } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { login, novoClienteApi } from "./apoio/api";
import { ARQUIVO_ESTADO_ADMIN, lerSemente } from "./apoio/semente";

// E2E-08 — Último administrador (Plano 7.3; CSU19; RF14, RNF 14.3). Desktop. O Admin
// da semente é o único Admin ativo da igreja. Em Usuários ele tenta inativar a
// própria conta e trocar o próprio perfil: as duas tentativas são bloqueadas e a conta
// continua Admin e Ativa.
//
// Ordem das regras (observação da 7.2): na inativação da PRÓPRIA conta vale primeiro a
// regra "não desativar o próprio usuário" (front e API), antes da 14.3; a mensagem da
// 14.3 aparece na troca de perfil. Este fluxo roda por último no projeto chromium; se
// um bloqueio falhar, o Admin semeado fica alterado e os fluxos seguintes caem juntos.

const MENSAGEM_PROPRIA_CONTA = "Você não pode desativar o seu próprio usuário.";
const MENSAGEM_ULTIMO_ADMIN = "Não é possível inativar ou alterar o perfil do único administrador ativo da igreja.";

test.use({ storageState: ARQUIVO_ESTADO_ADMIN });

async function escolher(page: Page, campo: Locator, opcao: string) {
  await campo.click();
  await page.getByRole("option", { name: opcao, exact: true }).click();
  await expect(campo).toContainText(opcao);
}

test("E2E-08 último administrador: inativar a própria conta e trocar o próprio perfil são bloqueados", async ({ page }) => {
  const { admin } = lerSemente();

  await page.goto("/usuarios");
  const linhaAdmin = page.locator("tr", { hasText: admin.email });
  await expect(linhaAdmin).toContainText("Admin");
  await expect(linhaAdmin).toContainText("Ativo");

  await linhaAdmin.getByTestId("usuario-editar").click();
  const dialogo = page.getByRole("dialog", { name: "Editar usuário" });
  await expect(dialogo).toBeVisible();
  const perfil = dialogo.getByTestId("usuario-perfil");
  const status = dialogo.getByTestId("usuario-status");
  const salvar = dialogo.getByTestId("usuario-salvar");

  // 1) Inativar a própria conta: aviso no diálogo e bloqueio ao salvar.
  await escolher(page, status, "Inativo");
  await expect(dialogo).toContainText("O próprio usuário logado não pode ser desativado.");
  await salvar.click();
  await expect(page.getByText(MENSAGEM_PROPRIA_CONTA)).toBeVisible();
  await expect(dialogo).toBeVisible();

  // 2) Trocar o próprio perfil (conta ativa): a API recusa pela RNF 14.3.
  await escolher(page, status, "Ativo");
  await escolher(page, perfil, "Professor");
  await salvar.click();
  await expect(page.getByText(MENSAGEM_ULTIMO_ADMIN)).toBeVisible();
  await expect(dialogo).toBeVisible();

  await dialogo.getByRole("button", { name: "Cancelar" }).click();
  await expect(dialogo).toBeHidden();
  await expect(linhaAdmin).toContainText("Admin");
  await expect(linhaAdmin).toContainText("Ativo");

  // Na API: as duas tentativas diretas também recusam, e a conta segue Admin e Ativa.
  const api = await novoClienteApi(apiURL);
  try {
    await login(api, admin.email, admin.senha);
    const usuarios = (await (await api.get("/api/usuarios")).json()) as { id: number; email: string; perfil: string; ativo: boolean }[];
    const conta = usuarios.find((u) => u.email === admin.email)!;
    expect(usuarios.filter((u) => u.perfil === "Admin" && u.ativo), "um único Admin ativo").toHaveLength(1);

    const inativar = await api.patch(`/api/usuarios/${conta.id}`, { data: { Ativo: false } });
    expect(inativar.status()).toBe(400);
    expect((await inativar.json()).mensagem).toBe(MENSAGEM_PROPRIA_CONTA);

    const trocarPerfil = await api.patch(`/api/usuarios/${conta.id}`, { data: { Perfil: "Professor" } });
    expect(trocarPerfil.status()).toBe(400);
    expect((await trocarPerfil.json()).mensagem).toBe(MENSAGEM_ULTIMO_ADMIN);

    const depois = await (await api.get(`/api/usuarios/${conta.id}`)).json();
    expect([depois.perfil, depois.ativo]).toEqual(["Admin", true]);
  } finally {
    await api.dispose();
  }
});
