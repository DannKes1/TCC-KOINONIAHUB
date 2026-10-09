import { expect } from "@playwright/test";
import type { Page, TestInfo } from "@playwright/test";
import { mkdirSync } from "node:fs";
import { dirname, resolve } from "node:path";

// Apoio do critério de responsividade (Etapa 6.3; RNFs "a interface deve ser
// responsiva"): nenhuma tela do fluxo do professor pode exigir rolagem horizontal
// da página a 360 px. Tabelas largas rolam dentro do próprio container do
// PrimeVue (.p-datatable-table-container) — isso é permitido e não conta aqui.

export async function semRolagemHorizontal(page: Page, tela: string) {
  // Referência: a largura configurada da janela. Na emulação de celular o Chromium
  // alarga window.innerWidth quando o conteúdo transborda, o que esconderia o defeito.
  const largura = page.viewportSize()?.width ?? 360;

  const problemas = await page.evaluate((larguraJanela) => {
    const encontrados: string[] = [];
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
    // Elementos que passam da borda direita (fora das tabelas, que rolam por conta
    // própria, e das camadas flutuantes do PrimeVue: toasts, diálogos, tooltips).
    for (const el of Array.from(document.querySelectorAll<HTMLElement>("body *"))) {
      const caixa = el.getBoundingClientRect();
      if (caixa.width === 0 || caixa.right <= larguraJanela + 1) continue;
      if (el.closest(".p-datatable-table-container, .p-toast, .p-dialog-mask, .p-tooltip, .p-drawer-mask, .p-confirmdialog")) continue;
      encontrados.push(`${el.tagName.toLowerCase()}.${Array.from(el.classList).slice(0, 3).join(".")} termina em ${Math.round(caixa.right)}px`);
      if (encontrados.length >= 6) break;
    }
    return encontrados;
  }, largura);

  expect(problemas, `${tela}: rolagem horizontal indevida`).toEqual([]);
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
