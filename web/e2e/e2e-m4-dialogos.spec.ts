import { expect, test } from "@playwright/test";
import type { Locator, Page } from "@playwright/test";

import { apiURL } from "./apoio/ambiente";
import { criarPessoa, criarUsuario, login, novoClienteApi } from "./apoio/api";
import { dialogoCabeNaTela } from "./apoio/celular";
import { ARQUIVO_ESTADO_ADMIN, emailE2E, lerSemente } from "./apoio/semente";

// E2E-M4 — Diálogos dentro da tela (Corretiva G5; RNFs "a interface deve ser
// responsiva"). Roda só no projeto "mobile". A 360, 430 e 1024 px, abre cada diálogo
// do sistema (Pessoas: nova, editar, parentescos com um vínculo, histórico de presenças
// e importar CSV; Usuários: novo, editar e convite; Turma: nova e editar; Matéria; Aula;
// Atribuição: nova e editar; Matrícula: matricular, responsáveis e histórico) e confere
// com `dialogoCabeNaTela`: a página sem rolagem horizontal, o diálogo inteiro na janela
// e nada dentro dele passando das bordas ou rolando para o lado. Igreja e Meus Dados são
// páginas, conferidas pelo critério do documento. Até a G5 o diálogo de Pessoas tinha
// quatro colunas fixas e, a 360 px, o conteúdo de 794 px rolava dentro do diálogo sem
// alargar a página: a conferência só do documento não pegava o defeito.
test.describe("E2E-M4 celular — diálogos dentro da tela", () => {
  test.use({ storageState: ARQUIVO_ESTADO_ADMIN });
  test.setTimeout(240_000);

  test("todos os diálogos a 360, 430 e 1024 px", async ({ page }) => {
    const { turma, admin, id } = lerSemente();
    const emailConvite = emailE2E("convite.m4", id);

    // Um vínculo para a tabela de Parentescos e uma conta com convite pendente.
    const api = await novoClienteApi(apiURL);
    try {
      await login(api, admin.email, admin.senha);
      const pessoa = await criarPessoa(api, `Convite M4 ${id}`, emailConvite);
      await criarUsuario(api, { pessoaId: pessoa.id, email: emailConvite, perfil: "Usuario" });
      const pessoas = (await (await api.get("/api/pessoas")).json()) as { id: number; nome: string }[];
      const pessoaAdmin = pessoas.find((p) => p.nome === admin.nome)!;
      const vinculo = await api.post(`/api/pessoas/${pessoaAdmin.id}/parentescos`, {
        data: { ParenteId: pessoa.id, TipoRelacionamento: "Responsável" },
      });
      expect(vinculo.ok(), "vínculo de parentesco do E2E-M4").toBeTruthy();
    } finally {
      await api.dispose();
    }

    for (const largura of [360, 430, 1024]) {
      await page.setViewportSize({ width: largura, height: largura === 1024 ? 768 : 640 });
      const compacta = largura <= 768;

      await page.goto("/pessoas");
      await carregado(page);
      const linhaAdmin = page.locator("tr", { hasText: admin.nome });
      await abrirEConferir(page, page.getByTestId("pessoa-nova"), "Nova pessoa");
      await abrirEConferir(page, linhaAdmin.getByTestId("pessoa-editar"), "Editar pessoa");
      await abrirEConferir(page, linhaAdmin.getByTestId("pessoa-parentescos"), "Parentescos");
      await abrirEConferir(page, linhaAdmin.getByTestId("pessoa-historico"), "Histórico de presenças");
      await abrirEConferir(page, page.getByTestId("pessoa-importar"), "Importar CSV");

      await page.goto("/usuarios");
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("usuario-novo"), "Novo usuário");
      await abrirEConferir(page, page.getByTestId("usuario-editar").first(), "Editar usuário");
      await abrirEConferir(
        page,
        page.locator("tr", { hasText: emailConvite }).getByTestId("usuario-convite"),
        "Convite de primeiro acesso",
      );

      await page.goto("/departamentos");
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("turma-nova"), "Nova turma");
      await abrirEConferir(page, page.getByTestId("turma-editar").first(), "Editar turma");

      await page.goto(`/departamentos/${turma.id}/materias`);
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("materia-nova"), "Matéria");

      await page.goto(`/departamentos/${turma.id}/aulas`);
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("aulas-nova"), "Aula");

      await page.goto(`/departamentos/${turma.id}/atribuicoes`);
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("atribuicao-nova"), "Nova atribuição");
      await abrirEConferir(page, page.getByTestId("atribuicao-editar").first(), "Editar atribuição");

      await page.goto(`/departamentos/${turma.id}/matriculas`);
      await carregado(page);
      await abrirEConferir(page, page.getByTestId("matricula-nova"), "Matricular aluno");
      await abrirEConferir(page, page.getByTestId("aluno-responsaveis").first(), "Responsáveis");
      if (compacta) {
        await page.getByTestId("menu-acoes").first().click();
        await abrirEConferir(
          page,
          page.getByRole("menuitem", { name: "Histórico de presenças" }),
          "Histórico da matrícula",
        );
      } else {
        await abrirEConferir(page, page.getByTestId("aluno-historico").first(), "Histórico da matrícula");
      }

      for (const rota of ["/igreja", "/meus-dados"]) {
        await page.goto(rota);
        await carregado(page);
        const medida = await page.evaluate(() => ({
          rolagem: document.documentElement.scrollWidth,
          visivel: document.documentElement.clientWidth,
        }));
        expect(medida.rolagem, `${rota} a ${largura} px`).toBeLessThanOrEqual(medida.visivel);
      }
    }
  });
});

async function carregado(page: Page) {
  await expect(page.locator(".pi-spin.pi-spinner")).toHaveCount(0);
}

async function abrirEConferir(page: Page, botao: Locator, nome: string) {
  await botao.click();
  await dialogoCabeNaTela(page, nome);
  await page.keyboard.press("Escape");
  const fechou = await expect(page.locator(".p-dialog"))
    .toHaveCount(0, { timeout: 1500 })
    .then(
      () => true,
      () => false,
    );
  if (!fechou) await page.locator(".p-dialog .p-dialog-close-button").last().click();
  await expect(page.locator(".p-dialog")).toHaveCount(0);
}
