import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaAulasTurma from "./PaginaAulasTurma.vue";
import MenuAcoes from "../../../components/ui/MenuAcoes.vue";
import type { OpcoesConfirmacao } from "../../../aplicacao/servicos/notificacoes";
import {
  atualizarAula,
  criarAula,
  excluirAula,
} from "../../../aplicacao/servicos/aulasServico";

// Etapa 6.5 (RF45 / CSU09 FA1–FA3): no ⋮ da aba Aulas, "Editar aula" só em aula
// Em aberto e "Excluir aula" só em aula Em aberto sem registro de presença; o
// diálogo de nova aula serve à edição e a exclusão passa pela confirmação comum.
vi.mock("../../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => ref(true),
}));

const requireConfirmacao = vi.fn<(opcoes: OpcoesConfirmacao) => void>();
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: requireConfirmacao, close: vi.fn() }),
}));

vi.mock("../../../aplicacao/armazenamentos/autenticacaoStore", () => ({
  usarAutenticacaoStore: () => ({ isAdmin: false }),
}));

vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  obterDepartamento: vi.fn(async (id: number) => ({ id, nome: "Jovens", tipo: "EBD", ativo: true })),
}));

vi.mock("../../../aplicacao/servicos/materiasServico", () => ({
  listarMaterias: vi.fn(async () => [
    { id: 1, nome: "Samuel", ativo: true, ordemExibicao: 1, departamentoId: 7, nomeDepartamento: "Jovens", criadoEm: "", atualizadoEm: null },
    { id: 2, nome: "Reis", ativo: true, ordemExibicao: 2, departamentoId: 7, nomeDepartamento: "Jovens", criadoEm: "", atualizadoEm: null },
  ]),
}));

vi.mock("../../../aplicacao/servicos/atribuicoesServico", () => ({
  listarAtribuicoesPorDepartamento: vi.fn(async () => [
    { id: 1, pessoaId: 10, pessoaNome: "Paula", funcao: "Professor", ativo: true, departamentoId: 7 },
  ]),
}));

function aula(id: number, extra: Record<string, unknown>) {
  return {
    id,
    data: `2026-10-${String(id).padStart(2, "0")}T12:00:00Z`,
    tema: `Aula ${id}`,
    conteudo: null,
    situacao: "EmAberto",
    pendenteFechamento: false,
    possuiPresencas: false,
    quantidadeVisitantes: 0,
    materiaId: 1,
    nomeMateria: "Samuel",
    departamentoId: 7,
    nomeDepartamento: "Jovens",
    professorId: 10,
    nomeProfessor: "Paula",
    criadoEm: "",
    ...extra,
  };
}

vi.mock("../../../aplicacao/servicos/aulasServico", () => ({
  listarAulasPorDepartamento: vi.fn(async () => [
    // Em aberto sem presenças, Em aberto com presenças e Consolidada.
    aula(18, { tema: "Sem presencas", conteudo: "Conteudo da 18" }),
    aula(11, { tema: "Com presencas", possuiPresencas: true }),
    aula(4, { tema: "Fechada", situacao: "Consolidada", possuiPresencas: true }),
  ]),
  criarAula: vi.fn(async () => undefined),
  atualizarAula: vi.fn(async () => undefined),
  excluirAula: vi.fn(async () => undefined),
  consolidarAula: vi.fn(async () => undefined),
  marcarAulaNaoRealizada: vi.fn(async () => undefined),
  reabrirAula: vi.fn(async () => undefined),
  extrairAlunosSemRegistro: vi.fn(() => []),
}));

async function montar() {
  const recarregarResumo = vi.fn(async () => undefined);
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/departamentos/:departamentoId/aulas", component: PaginaAulasTurma },
      { path: "/aulas/:aulaId/chamada", component: { template: "<div />" } },
      { path: "/aulas/:aulaId/presencas", component: { template: "<div />" } },
    ],
  });
  await router.push("/departamentos/7/aulas");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: {
      plugins: [router, PrimeVue],
      directives: { tooltip: Tooltip },
      provide: { turmaRecarregarResumo: recarregarResumo },
    },
  });
  await flushPromises();
  return { wrapper, recarregarResumo };
}

// Rótulos que o ⋮ de um cartão oferece (o MenuAcoes já filtra `visible`).
function rotulosDoMenu(wrapper: ReturnType<typeof mount>, indice: number): string[] {
  const menu = wrapper.findAllComponents(MenuAcoes)[indice]!;
  return menu
    .props("itens")
    .filter((item) => (typeof item.visible === "function" ? item.visible() : item.visible !== false))
    .map((item) => String(item.label));
}

// Abre o ⋮ do primeiro cartão e aciona o item pelo rótulo, no menu popup.
async function acionarNoMenu(wrapper: ReturnType<typeof mount>, rotulo: string) {
  await wrapper.findAll('[data-testid="aula-cartao"]')[0]!.get('[data-testid="menu-acoes"]').trigger("click");
  await flushPromises();
  const item = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
    .find((el) => el.textContent?.includes(rotulo));
  expect(item, `item "${rotulo}" no ⋮`).toBeDefined();
  item!.click();
  await flushPromises();
}

describe("PaginaAulasTurma — Editar e Excluir aula Em aberto (RF45, 6.5)", () => {
  let wrapperAtual: ReturnType<typeof mount> | null = null;

  // O Dropdown (Select) do PrimeVue consulta matchMedia e o Textarea com autoResize
  // usa ResizeObserver; o jsdom não implementa nenhum dos dois.
  beforeAll(() => {
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      writable: true,
      value: () => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
      }),
    });
    Object.defineProperty(window, "ResizeObserver", {
      configurable: true,
      writable: true,
      value: class {
        observe() {}
        unobserve() {}
        disconnect() {}
      },
    });
  });

  beforeEach(() => {
    requireConfirmacao.mockClear();
    vi.mocked(criarAula).mockClear();
    vi.mocked(atualizarAula).mockClear();
    vi.mocked(excluirAula).mockClear();
  });

  afterEach(() => {
    wrapperAtual?.unmount();
    wrapperAtual = null;
    document.body.innerHTML = "";
  });

  it("Editar só em aula Em aberto; Excluir e Não realizada só em Em aberto sem presenças (CSU09 FA3, CSU11 FA2)", async () => {
    const { wrapper } = await montar();
    wrapperAtual = wrapper;

    // Cartões da mais recente para a mais antiga: 18 (sem presenças), 11 (com), 4 (Consolidada).
    expect(wrapper.findAll('[data-testid="aula-cartao"]')).toHaveLength(3);
    expect(wrapper.find(".p-datatable").exists()).toBe(false);

    const semPresencas = rotulosDoMenu(wrapper, 0);
    expect(semPresencas).toContain("Editar aula");
    expect(semPresencas).toContain("Excluir aula");
    expect(semPresencas).toContain("Marcar como Não realizada");

    // Com presenças: a API recusaria (RNF 33.7), então o ⋮ não oferece (CSU11 FA2).
    const comPresencas = rotulosDoMenu(wrapper, 1);
    expect(comPresencas).toContain("Editar aula");
    expect(comPresencas).not.toContain("Excluir aula");
    expect(comPresencas).not.toContain("Marcar como Não realizada");

    const consolidada = rotulosDoMenu(wrapper, 2);
    expect(consolidada).not.toContain("Editar aula");
    expect(consolidada).not.toContain("Excluir aula");
    expect(consolidada).not.toContain("Marcar como Não realizada");
  });

  it("Editar abre o diálogo preenchido e Salvar chama atualizarAula e reconta o cabeçalho (FA1)", async () => {
    const { wrapper, recarregarResumo } = await montar();
    wrapperAtual = wrapper;

    await acionarNoMenu(wrapper, "Editar aula");

    const dialogo = document.querySelector<HTMLElement>('[data-testid="aula-dialogo"]');
    expect(dialogo).not.toBeNull();
    expect(dialogo!.textContent).toContain("Editar aula");

    const campoTema = dialogo!.querySelector<HTMLInputElement>("input.p-inputtext:not(.p-datepicker-input)");
    expect(campoTema!.value).toBe("Sem presencas");
    const campoConteudo = dialogo!.querySelector<HTMLTextAreaElement>('[data-testid="aula-conteudo"]');
    expect(campoConteudo!.value).toBe("Conteudo da 18");

    campoTema!.value = "Tema editado";
    campoTema!.dispatchEvent(new Event("input"));
    campoConteudo!.value = "Conteudo editado";
    campoConteudo!.dispatchEvent(new Event("input"));
    await flushPromises();

    dialogo!.querySelector<HTMLButtonElement>('[data-testid="aula-salvar"]')!.click();
    await flushPromises();

    expect(criarAula).not.toHaveBeenCalled();
    expect(atualizarAula).toHaveBeenCalledTimes(1);
    expect(vi.mocked(atualizarAula).mock.calls[0]![0]).toBe(18);
    expect(vi.mocked(atualizarAula).mock.calls[0]![1]).toMatchObject({
      Tema: "Tema editado",
      Conteudo: "Conteudo editado",
      MateriaId: 1,
      ProfessorId: 10,
    });
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
  });

  it("Excluir pede a confirmação comum e, aceita, chama excluirAula e reconta o cabeçalho (FA2)", async () => {
    const { wrapper, recarregarResumo } = await montar();
    wrapperAtual = wrapper;

    await acionarNoMenu(wrapper, "Excluir aula");

    expect(excluirAula).not.toHaveBeenCalled();
    const opcoes = requireConfirmacao.mock.calls[requireConfirmacao.mock.calls.length - 1]![0];
    expect(opcoes.header).toBe("Excluir aula");
    expect(opcoes.acceptLabel).toBe("Excluir");
    expect((opcoes as Record<string, unknown>).acceptClass).toBeUndefined();

    await opcoes.accept!();
    await flushPromises();

    expect(excluirAula).toHaveBeenCalledWith(18);
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
  });

  // SonarCloud Web:S6853: cada rótulo do diálogo aponta (for) para o campo que
  // descreve, e os Dropdowns de Matéria e Professor recebem o nome do rótulo por
  // aria-labelledby. No jsdom o PrimeVue não liga o clique do rótulo ao Dropdown
  // (offsetParent é sempre null), por isso o caso confere só os atributos.
  it("Nova aula: todo rótulo do diálogo aponta para um campo e os Dropdowns são nomeados pelo rótulo (S6853)", async () => {
    const { wrapper } = await montar();
    wrapperAtual = wrapper;

    await wrapper.get('[data-testid="aulas-nova"]').trigger("click");
    await flushPromises();

    const dialogo = document.querySelector<HTMLElement>('[data-testid="aula-dialogo"]');
    expect(dialogo).not.toBeNull();
    expect(dialogo!.textContent).toContain("Nova aula");

    const rotulos = Array.from(dialogo!.querySelectorAll("label"));
    expect(rotulos).toHaveLength(5);
    for (const rotulo of rotulos) {
      const alvo = rotulo.htmlFor;
      expect(alvo, `rótulo "${rotulo.textContent}" sem for`).not.toBe("");
      expect(document.querySelectorAll(`[id="${alvo}"]`), `id "${alvo}" único no documento`).toHaveLength(1);
      const campo = document.getElementById(alvo);
      expect(campo!.matches("input, textarea, [role='combobox']"), `#${alvo} é um campo`).toBe(true);
    }

    // Cada rótulo aponta para o seu campo, não só para algum campo.
    const alvoDe = (texto: string) =>
      document.getElementById(rotulos.find((r) => r.textContent!.trim() === texto)!.htmlFor);
    expect(alvoDe("Data")?.closest(".p-datepicker")).not.toBeNull();
    expect(alvoDe("Tema (opcional)")?.getAttribute("data-testid")).toBe("aula-tema");
    expect(alvoDe("Conteúdo (opcional)")?.getAttribute("data-testid")).toBe("aula-conteudo");

    for (const [testid, texto] of [
      ["aula-materia", "Matéria"],
      ["aula-professor", "Professor"],
    ] as const) {
      const combobox = dialogo!.querySelector<HTMLElement>(`[data-testid="${testid}"] [role="combobox"]`);
      expect(combobox, `combobox de ${texto}`).not.toBeNull();
      const idRotulo = combobox!.getAttribute("aria-labelledby");
      expect(idRotulo, `aria-labelledby de ${texto}`).toBeTruthy();
      const rotulo = document.getElementById(idRotulo!) as HTMLLabelElement | null;
      expect(rotulo?.tagName).toBe("LABEL");
      expect(rotulo!.textContent!.trim()).toBe(texto);
      expect(rotulo!.htmlFor).toBe(combobox!.id);
    }
  });
});
