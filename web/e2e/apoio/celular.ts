import { expect } from "@playwright/test";
import type { Page, TestInfo } from "@playwright/test";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";

// Apoio do critério de responsividade (Etapa 6.3; RNFs "a interface deve ser
// responsiva"): nenhuma tela do fluxo do professor pode exigir rolagem horizontal
// da página a 360 px. Desde a 6.4-C o padrão é o MODO ESTRITO (registro do projeto, 6.12): além
// do documento e do <main>, nenhum container interno pode rolar para o lado — nem a
// tabela do PrimeVue (.p-datatable-table-container), que nas telas operacionais deu
// lugar aos cartões. Só a faixa de abas da Página da Turma (.p-tablist-content)
// rola de propósito. `estrito: false` volta ao critério da 6.3 (tabelas podem rolar
// dentro do próprio container).
export async function semRolagemHorizontal(
  page: Page,
  tela: string,
  opcoes: { estrito?: boolean } = {},
) {
  const estrito = opcoes.estrito ?? true;
  // Referência: a largura configurada da janela. Na emulação de celular o Chromium
  // alarga window.innerWidth quando o conteúdo transborda, o que esconderia o defeito.
  const largura = page.viewportSize()?.width ?? 360;

  const problemas = await page.evaluate(
    ({ larguraJanela, estrito }) => {
      const encontrados: string[] = [];
      const descrever = (el: Element) =>
        `${el.tagName.toLowerCase()}${el.id ? `#${el.id}` : ""}.${Array.from(el.classList).slice(0, 3).join(".")}`;
      const raiz = document.documentElement;
      if (raiz.scrollWidth > larguraJanela + 1) {
        encontrados.push(`documento com ${raiz.scrollWidth}px para uma janela de ${larguraJanela}px`);
      }
      // A área de conteúdo do layout tem overflow próprio; uma rolagem lateral nela
      // seria o mesmo defeito, só que escondido do documento.
      const conteudo = document.querySelector("main.conteudo");
      if (conteudo && conteudo.scrollWidth > conteudo.clientWidth + 1) {
        encontrados.push(`main.conteudo com ${conteudo.scrollWidth}px para ${conteudo.clientWidth}px visíveis`);
      }
      // Camadas flutuantes do PrimeVue (toasts, diálogos, tooltips) ficam fora da conta;
      // a faixa de abas rola de propósito; fora do modo estrito, a tabela também.
      const ignorados = [".p-tablist-content", ".p-toast", ".p-dialog-mask", ".p-tooltip", ".p-drawer-mask", ".p-confirmdialog"];
      if (!estrito) ignorados.push(".p-datatable-table-container");
      const seletorIgnorados = ignorados.join(", ");

      for (const el of Array.from(document.querySelectorAll<HTMLElement>("body *"))) {
        if (el.closest(seletorIgnorados)) continue;
        // Modo estrito: nenhum container interno pode rolar para o lado.
        if (estrito && el !== conteudo) {
          const overflowX = getComputedStyle(el).overflowX;
          if ((overflowX === "auto" || overflowX === "scroll") && el.scrollWidth > el.clientWidth + 1) {
            encontrados.push(`${descrever(el)} rola para o lado (${el.scrollWidth}px em ${el.clientWidth}px)`);
          }
        }
        // Elementos que passam da borda direita.
        const caixa = el.getBoundingClientRect();
        if (caixa.width === 0 || caixa.right <= larguraJanela + 1) continue;
        encontrados.push(`${descrever(el)} termina em ${Math.round(caixa.right)}px`);
        if (encontrados.length >= 6) break;
      }
      return encontrados;
    },
    { larguraJanela: largura, estrito },
  );

  expect(problemas, `${tela}: rolagem horizontal indevida`).toEqual([]);
}

// Critério mais estreito, para as telas administrativas (registro do projeto, seção 9: "utilizáveis
// com rolagem"): a LARGURA DO DOCUMENTO é a da janela — a página não desliza para o
// lado nem a barra do topo se mexe; uma tabela ou um filtro mais largo rola dentro
// do <main>. Ajuste da corretiva da 6.4-B (E2E-M2).
export async function larguraDoDocumento(page: Page, tela: string) {
  const largura = page.viewportSize()?.width ?? 360;
  const medida = await page.evaluate(() => ({
    documento: document.documentElement.scrollWidth,
    corpo: document.body.scrollWidth,
  }));
  expect(medida.documento, `${tela}: largura do documento`).toBe(largura);
  expect(medida.corpo, `${tela}: largura do body`).toBe(largura);
}

// Print da tela inteira em test-results/evidencias/RNF-responsividade-<nome>.png,
// só no projeto "mobile" (os desktop já têm prints manuais por RF). O autor copia
// os que quiser para docs/evidencias/.
export async function evidenciaCelular(page: Page, testInfo: TestInfo, nome: string) {
  if (testInfo.project.name !== "mobile") return;

  // Ao lado de playwright.config.ts (rootDir aponta para a pasta dos testes).
  const raiz = testInfo.config.configFile
    ? dirname(testInfo.config.configFile)
    : testInfo.config.rootDir;
  const pasta = resolve(raiz, "test-results", "evidencias");
  mkdirSync(pasta, { recursive: true });
  await page.screenshot({
    path: resolve(pasta, `RNF-responsividade-${nome}.png`),
    fullPage: true,
  });
}

// Alvo de toque: caixa delimitadora dentro da janela e com o tamanho mínimo.
export async function alvoDeToque(
  caixa: { x: number; y: number; width: number; height: number } | null,
  oQue: string,
  minimo = 40,
  larguraJanela = 360,
) {
  expect(caixa, `${oQue}: elemento sem caixa (invisível?)`).not.toBeNull();
  const { x, width, height } = caixa!;
  expect(x, `${oQue}: começa fora da janela`).toBeGreaterThanOrEqual(0);
  expect(x + width, `${oQue}: termina fora da janela`).toBeLessThanOrEqual(larguraJanela + 1);
  expect(width, `${oQue}: largura menor que ${minimo}px`).toBeGreaterThanOrEqual(minimo);
  expect(height, `${oQue}: altura menor que ${minimo}px`).toBeGreaterThanOrEqual(minimo);
}

// Corretiva G5 (E2E-M5): página inteira na janela, sem diálogo aberto. Mesmo critério do
// E2E-M4: o documento não passa da janela nem fica deslocado para o lado (scrollX; foi o
// que aconteceu em Pessoas ao abrir a lista do filtro Situação), o <main> não rola para o
// lado e nenhum elemento passa da borda direita. A exceção é o cartão da tabela das telas
// administrativas (.p-datatable-table-container), que rola por dentro, e a faixa de abas
// (.p-tablist-content). Listas abertas (overlay do Select, do DatePicker e do menu ⋮)
// entram na conta: ficam no body, fora de qualquer exceção.
export async function problemasDeLargura(page: Page): Promise<string[]> {
  const largura = page.viewportSize()?.width ?? 360;
  return page.evaluate((larguraJanela) => {
    const encontrados: string[] = [];
    const descrever = (el: Element) =>
      `${el.tagName.toLowerCase()}${el.id ? `#${el.id}` : ""}.${Array.from(el.classList).slice(0, 3).join(".")}`;
    const raiz = document.documentElement;
    if (raiz.scrollWidth > raiz.clientWidth) {
      encontrados.push(`documento com ${raiz.scrollWidth}px para ${raiz.clientWidth}px`);
    }
    if (window.scrollX !== 0 || raiz.scrollLeft !== 0) {
      encontrados.push(`página deslocada ${Math.round(window.scrollX || raiz.scrollLeft)}px para o lado`);
    }
    const conteudo = document.querySelector("main.conteudo");
    if (conteudo && (conteudo.scrollWidth > conteudo.clientWidth + 1 || conteudo.scrollLeft !== 0)) {
      encontrados.push(`main.conteudo com ${conteudo.scrollWidth}px para ${conteudo.clientWidth}px visíveis`);
    }
    const permitidos = ".p-datatable-table-container, .p-tablist-content, .p-toast, .p-tooltip";
    for (const el of Array.from(document.querySelectorAll<HTMLElement>("body *"))) {
      if (el.closest(permitidos)) continue;
      if (el !== conteudo) {
        const overflowX = getComputedStyle(el).overflowX;
        if ((overflowX === "auto" || overflowX === "scroll") && el.scrollWidth > el.clientWidth + 1) {
          encontrados.push(`${descrever(el)} rola para o lado (${el.scrollWidth}px em ${el.clientWidth}px)`);
        }
      }
      const caixa = el.getBoundingClientRect();
      if (caixa.width > 0 && caixa.height > 0 && (caixa.right > larguraJanela + 1 || caixa.left < -1)) {
        encontrados.push(`${descrever(el)} de ${Math.round(caixa.left)} a ${Math.round(caixa.right)}px`);
      }
      if (encontrados.length >= 6) break;
    }
    return encontrados;
  }, largura);
}

// Corretiva G5 (E2E-M4): diálogo aberto inteiro na janela. O diálogo do PrimeVue é uma
// camada fixa: um conteúdo mais largo que ele não alarga o documento, rola para o lado
// Ajuste A1: o foco pelo teclado continua visível nos diálogos. Ao abrir, o PrimeVue
// põe o foco no botão de fechar; Shift+Tab leva ao último elemento do diálogo (a
// armadilha de foco dá a volta) e Tab traz de volta ao botão de fechar, agora pelo
// teclado. O anel tem de aparecer: :focus-visible com contorno de 2 px ou mais.
export async function focoVisivelNoDialogo(page: Page, tela: string) {
  await page.keyboard.press("Shift+Tab");
  await page.keyboard.press("Tab");
  const foco = await page.evaluate(() => {
    const el = document.activeElement as HTMLElement | null;
    const dialogos = Array.from(document.querySelectorAll(".p-dialog"));
    const dl = dialogos[dialogos.length - 1];
    if (!el || !dl || !dl.contains(el)) return { dentro: false, botao: false, visivel: false, estilo: "", largura: 0 };
    const cs = getComputedStyle(el);
    return {
      dentro: true,
      botao: el.matches(".p-dialog-header .p-button, .p-dialog-footer .p-button"),
      visivel: el.matches(":focus-visible"),
      estilo: cs.outlineStyle,
      largura: Number.parseFloat(cs.outlineWidth),
    };
  });
  expect(foco.dentro, `${tela}: foco dentro do diálogo`).toBe(true);
  expect(foco.botao, `${tela}: foco num botão do cabeçalho ou do rodapé`).toBe(true);
  expect(foco.visivel, `${tela}: foco visível pelo teclado`).toBe(true);
  expect(foco.estilo, `${tela}: contorno do foco`).not.toBe("none");
  expect(foco.largura, `${tela}: largura do contorno do foco`).toBeGreaterThanOrEqual(2);
}

// por dentro. Por isso, além do critério do documento (scrollWidth <= clientWidth),
// confere a caixa do diálogo na janela, nenhum elemento do diálogo passando das bordas
// dele e nenhum container do diálogo rolando para o lado (nem a tabela). A rolagem
// vertical fica no conteúdo do diálogo, que não passa da altura da janela.
export async function dialogoCabeNaTela(page: Page, tela: string) {
  const janela = page.viewportSize() ?? { width: 360, height: 640 };
  const dialogo = page.locator(".p-dialog").last();
  await expect(dialogo, `${tela}: diálogo aberto`).toBeVisible();
  // Fim da animação de abertura (o diálogo entra com escala).
  await page.waitForTimeout(400);

  const problemas = await page.evaluate(
    ({ largura, altura }) => {
      const encontrados: string[] = [];
      const descrever = (el: Element) =>
        `${el.tagName.toLowerCase()}${el.id ? `#${el.id}` : ""}.${Array.from(el.classList).slice(0, 3).join(".")}`;
      const raiz = document.documentElement;
      if (raiz.scrollWidth > raiz.clientWidth) {
        encontrados.push(`documento com ${raiz.scrollWidth}px para ${raiz.clientWidth}px`);
      }
      const dialogos = Array.from(document.querySelectorAll<HTMLElement>(".p-dialog"));
      const dl = dialogos[dialogos.length - 1];
      if (!dl) return ["nenhum diálogo aberto"];
      const caixa = dl.getBoundingClientRect();
      if (caixa.left < 0 || caixa.right > largura + 1) {
        encontrados.push(`diálogo de ${Math.round(caixa.left)} a ${Math.round(caixa.right)}px numa janela de ${largura}px`);
      }
      if (caixa.top < 0 || caixa.bottom > altura + 1) {
        encontrados.push(`diálogo de ${Math.round(caixa.top)} a ${Math.round(caixa.bottom)}px numa janela de ${altura}px de altura`);
      }
      for (const el of [dl, ...Array.from(dl.querySelectorAll<HTMLElement>("*"))]) {
        const overflowX = getComputedStyle(el).overflowX;
        if ((overflowX === "auto" || overflowX === "scroll") && el.scrollWidth > el.clientWidth + 1) {
          encontrados.push(`${descrever(el)} rola para o lado (${el.scrollWidth}px em ${el.clientWidth}px)`);
        }
        const b = el.getBoundingClientRect();
        if (el !== dl && b.width > 0 && (b.right > caixa.right + 1 || b.left < caixa.left - 1)) {
          encontrados.push(`${descrever(el)} de ${Math.round(b.left)} a ${Math.round(b.right)}px, fora do diálogo`);
        }
        if (encontrados.length >= 6) break;
      }
      return encontrados;
    },
    { largura: janela.width, altura: janela.height },
  );

  expect(problemas, `${tela} a ${janela.width} px`).toEqual([]);
}
