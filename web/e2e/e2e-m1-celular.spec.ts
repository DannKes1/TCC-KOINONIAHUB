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

      // Chamada (CSU10, Etapa 6.4-B): abre pelo cartão da aula; voltar leva à turma;
      // tocar na linha marca a presença; "+ observação" abre o campo com a
      // orientação da RNF 32.5; a barra fixa traz Salvar e Consolidar.
      await fazerChamada.click();
      await page.waitForURL((url) => url.pathname === `/aulas/${aulaId}/chamada`);
      await expect(page.locator(".page-header-voltar")).toContainText(turma.nome);
      const lista = page.getByTestId("chamada-lista");
      await expect(lista.getByTestId("chamada-linha")).toHaveCount(alunos.length);
      await expect(page.getByTestId("chamada-contador")).toHaveText(`Presentes: 0 de ${alunos.length}`);

      const primeiraLinha = lista.getByTestId("chamada-linha").first();
      await primeiraLinha.scrollIntoViewIfNeeded();
      await expect(primeiraLinha).toBeInViewport();
      await alvoDeToque(await primeiraLinha.getByTestId("chamada-presente").boundingBox(), "caixa Presente");
      // Toque no nome (não na caixa): a linha inteira é o alvo.
      await primeiraLinha.getByTestId("chamada-nome").click();
      await expect(primeiraLinha.getByTestId("chamada-presente").locator("input")).toBeChecked();
      await expect(page.getByTestId("chamada-contador")).toHaveText(`Presentes: 1 de ${alunos.length}`);

      // Observação só quando pedida, com a orientação da RNF 32.5.
      await expect(page.getByTestId("dica-observacao")).toHaveCount(0);
      const segundaLinha = lista.getByTestId("chamada-linha").nth(1);
      await segundaLinha.getByTestId("chamada-abrir-observacao").click();
      await expect(segundaLinha.getByTestId("dica-observacao")).toContainText("informações de saúde");
      await segundaLinha.getByTestId("chamada-observacao").fill("chegou após o início");
      // Abrir a observação não alterou a presença da linha.
      await expect(segundaLinha.getByTestId("chamada-presente").locator("input")).not.toBeChecked();

      // Barra fixa: visível sem rolar até o fim, com alvos de 40 px.
      const barra = page.getByTestId("barra-acoes");
      await expect(barra).toBeInViewport();
      const salvar = page.getByTestId("chamada-salvar");
      const consolidar = page.getByTestId("chamada-consolidar");
      await alvoDeToque(await salvar.boundingBox(), "botão Salvar chamada");
      await alvoDeToque(await consolidar.boundingBox(), "botão Consolidar");
      await semRolagemHorizontal(page, "Chamada");
      await evidenciaCelular(page, testInfo, "chamada");

      // Toque duplo em Salvar (ajuste da corretiva da 6.4-B): dois toques "crus",
      // sem esperar o botão entre eles, como um dedo apressado — uma gravação e um
      // único aviso "Chamada salva". (O segundo toque chegava com a primeira
      // gravação já concluída e gerava o segundo aviso.)
      const caixaSalvar = (await salvar.boundingBox())!;
      const [xSalvar, ySalvar] = [caixaSalvar.x + caixaSalvar.width / 2, caixaSalvar.y + caixaSalvar.height / 2];
      let gravacoes = 0;
      const contarGravacao = (r: { method(): string; url(): string }) => {
        if (r.method() === "POST" && /\/api\/aulas\/\d+\/presencas$/.test(r.url())) gravacoes++;
      };
      page.on("request", contarGravacao);
      await page.touchscreen.tap(xSalvar, ySalvar);
      await page.touchscreen.tap(xSalvar, ySalvar);
      await expect(page.getByText("Chamada salva com sucesso.")).toBeVisible();
      await page.waitForTimeout(700); // janela de espera do botão + tempo de um segundo aviso aparecer
      page.off("request", contarGravacao);
      expect(gravacoes, "toque duplo em Salvar deve gravar uma vez").toBe(1);
      await expect(page.getByText("Chamada salva com sucesso.")).toHaveCount(1);
      await expect(salvar).toBeEnabled();

      // Consolidar com alteração pendente (decisão 3): marca mais um aluno sem salvar;
      // a confirmação avisa que vai salvar e consolidar; depois a aula fica fechada,
      // a barra some e a lista fica somente leitura (CSU11).
      // Toque no meio da linha (fora do nome e da caixa): a linha inteira é o alvo.
      await lista.getByTestId("chamada-linha").nth(2).click();
      await expect(page.getByTestId("chamada-contador")).toHaveText(`Presentes: 2 de ${alunos.length}`);
      await consolidar.click();
      const confirmacao = page.locator(".p-confirmdialog");
      await expect(confirmacao).toContainText("A chamada será salva e consolidada");
      await confirmacao.getByRole("button", { name: "Salvar e consolidar" }).click();
      await expect(page.getByText("Aula consolidada com sucesso.")).toBeVisible();
      await expect(page.getByTestId("tag-situacao").first()).toHaveText("Consolidada");
      await expect(barra).toHaveCount(0);
      await expect(page.getByTestId("chamada-mensagem")).toContainText("somente leitura");
      await expect(primeiraLinha.getByTestId("chamada-presente").locator("input")).toBeDisabled();
      await evidenciaCelular(page, testInfo, "chamada-consolidada");

      // Presenças da Aula (cartões): três registros, a observação salva e o voltar
      // para a turma.
      await page.goto(`/aulas/${aulaId}/presencas`);
      await expect(page.locator("h2.page-header-titulo")).toContainText("Presenças");
      await expect(page.locator(".page-header-voltar")).toContainText(turma.nome);
      const cartoes = page.getByTestId("presenca-cartao");
      await expect(cartoes).toHaveCount(alunos.length);
      await expect(cartoes.filter({ hasText: "Presente" })).toHaveCount(2);
      await expect(cartoes.filter({ hasText: "chegou após o início" })).toHaveCount(1);
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
      await expect(page.locator(".page-header-voltar")).toContainText("Minhas Turmas");
      await semRolagemHorizontal(page, "Minha Frequência");
      await evidenciaCelular(page, testInfo, "minha-frequencia");
    });
  });
});
