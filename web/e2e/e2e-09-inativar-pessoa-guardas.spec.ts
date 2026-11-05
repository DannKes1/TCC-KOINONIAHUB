import { expect, test } from "@playwright/test";
import type { Page } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { aceitarTermo, criarPessoa, criarUsuario, login, novoClienteApi, obterTermoVigente } from "./apoio/api";
import { ARQUIVO_ESTADO_ADMIN, SENHA_PADRAO, emailE2E, lerSemente } from "./apoio/semente";

// E2E-09 — Guardas da inativação da pessoa pela tela (Corretiva G5; RF9, RNF 9.3 proposta;
// CSU16 FA2). Desktop. O Admin da semente é o único Admin ativo da igreja:
//   1. um Superintendente edita a pessoa do Admin, escolhe Inativo e clica Atualizar: a
//      mensagem do único administrador aparece no topo do diálogo, sem erro no campo CEP;
//   2. o Admin edita o próprio cadastro e escolhe Inativo: a mensagem do próprio cadastro
//      aparece no topo do diálogo.
// Nos dois casos a pessoa e a conta do Admin continuam ativas. A pessoa do Admin recebe um
// CEP antes, para o campo com máscara participar do envio (o CEP gravado antes da máscara
// é coberto no spec de PaginaPessoasLista). Como no E2E-08, se uma guarda falhar o Admin
// semeado fica inativo e os fluxos seguintes caem juntos.

const MENSAGEM_UNICO_ADMIN = "Não é possível inativar a pessoa do único administrador ativo da igreja.";
const MENSAGEM_PROPRIO_CADASTRO = "Você não pode inativar o seu próprio cadastro.";

async function tentarInativarPessoa(page: Page, nome: string) {
  await page.goto("/pessoas");
  await expect(page.locator(".pi-spin.pi-spinner")).toHaveCount(0);
  await page.locator("tr", { hasText: nome }).getByTestId("pessoa-editar").click();

  const dialogo = page.getByRole("dialog", { name: "Editar pessoa" });
  await expect(dialogo).toBeVisible();
  await expect(dialogo.locator("#pessoa-formulario-cep")).toHaveValue("76900-000");

  const situacao = dialogo.getByRole("combobox", { name: "Situação" });
  await situacao.click();
  await page.getByRole("option", { name: "Inativo", exact: true }).click();
  await expect(situacao).toContainText("Inativo");

  await dialogo.getByTestId("pessoa-dialogo-salvar").click();
  return dialogo;
}

test("E2E-09 inativar pessoa: guardas do único Admin e do próprio cadastro aparecem no diálogo", async ({ browser }) => {
  const { admin, id } = lerSemente();
  const emailSuper = emailE2E("super.e2e09", id);

  const api = await novoClienteApi(apiURL);
  let pessoaAdminId: number;
  try {
    await login(api, admin.email, admin.senha);
    const pessoas = (await (await api.get("/api/pessoas")).json()) as { id: number; nome: string }[];
    pessoaAdminId = pessoas.find((p) => p.nome === admin.nome)!.id;

    const completa = await (await api.get(`/api/pessoas/${pessoaAdminId}`)).json();
    const comCep = await api.put(`/api/pessoas/${pessoaAdminId}`, {
      data: { ...completa, Nome: completa.nome, Email: completa.email, Situacao: "Ativo", CEP: "76900000" },
    });
    expect(comCep.status(), "CEP na pessoa do Admin").toBe(204);

    const pessoaSuper = await criarPessoa(api, `Superintendente E2E ${id}`, emailSuper);
    await criarUsuario(api, { pessoaId: pessoaSuper.id, email: emailSuper, perfil: "Superintendente", senha: SENHA_PADRAO });
  } finally {
    await api.dispose();
  }

  // O Superintendente aceita o termo pela API e entra pela tela.
  const apiSuper = await novoClienteApi(apiURL);
  try {
    await login(apiSuper, emailSuper, SENHA_PADRAO);
    await aceitarTermo(apiSuper, (await obterTermoVigente(apiSuper)).versao);
  } finally {
    await apiSuper.dispose();
  }

  // 1) Superintendente: pessoa do único Admin.
  const contextoSuper = await browser.newContext();
  const paginaSuper = await contextoSuper.newPage();
  try {
    await paginaSuper.goto("/login");
    await paginaSuper.getByTestId("login-email").fill(emailSuper);
    await paginaSuper.locator("#login-senha").fill(SENHA_PADRAO);
    await paginaSuper.getByTestId("login-entrar").click();
    await paginaSuper.waitForURL((url) => url.pathname === "/");

    const dialogo = await tentarInativarPessoa(paginaSuper, admin.nome);
    await expect(dialogo.getByTestId("pessoa-dialogo-erro")).toContainText(MENSAGEM_UNICO_ADMIN);
    await expect(dialogo.getByTestId("pessoa-cep-erro")).toHaveCount(0);
    await expect(dialogo).toBeVisible();
  } finally {
    await contextoSuper.close();
  }

  // 2) Admin: o próprio cadastro.
  const contextoAdmin = await browser.newContext({ storageState: ARQUIVO_ESTADO_ADMIN });
  const paginaAdmin = await contextoAdmin.newPage();
  try {
    const dialogo = await tentarInativarPessoa(paginaAdmin, admin.nome);
    await expect(dialogo.getByTestId("pessoa-dialogo-erro")).toContainText(MENSAGEM_PROPRIO_CADASTRO);
    await expect(dialogo.getByTestId("pessoa-cep-erro")).toHaveCount(0);
  } finally {
    await contextoAdmin.close();
  }

  // Nada mudou: a pessoa e a conta do Admin continuam ativas.
  const conferencia = await novoClienteApi(apiURL);
  try {
    await login(conferencia, admin.email, admin.senha);
    const pessoa = await (await conferencia.get(`/api/pessoas/${pessoaAdminId}`)).json();
    expect(pessoa.situacao).toBe("Ativo");
    const usuarios = (await (await conferencia.get("/api/usuarios")).json()) as { email: string; ativo: boolean }[];
    expect(usuarios.find((u) => u.email === admin.email)?.ativo).toBe(true);
  } finally {
    await conferencia.dispose();
  }
});
