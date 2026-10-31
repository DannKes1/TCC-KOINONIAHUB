import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Dropdown from "primevue/dropdown";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import { obterAtribuicao } from "../../../aplicacao/servicos/atribuicoesServico";
import PaginaAtribuicoesDepartamento from "./PaginaAtribuicoesDepartamento.vue";

// Etapa 7.1 (RNF 21.5): na lista de atribuições da turma, a pessoa sem conta de
// acesso e a pessoa com conta de perfil Usuário recebem uma tag ao lado do nome;
// quem tem conta Professor ou administrativa não recebe nada.
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: vi.fn(), close: vi.fn() }),
}));

vi.mock("../../../aplicacao/armazenamentos/autenticacaoStore", () => ({
  usarAutenticacaoStore: () => ({ perfil: "Superintendente" }),
}));

vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  obterDepartamento: vi.fn(async (id: number) => ({ id, nome: "Jovens", tipo: "EBD", ativo: true })),
}));

vi.mock("../../../aplicacao/servicos/pessoasServico", () => ({
  listarPessoas: vi.fn(async () => []),
}));

function atribuicao(id: number, pessoaNome: string, perfilConta: string | null) {
  return {
    id,
    pessoaId: id,
    pessoaNome,
    departamentoId: 7,
    departamentoNome: "Jovens",
    funcao: "Auxiliar",
    dataInicio: "2026-02-01T12:00:00Z",
    dataFim: null,
    ativo: true,
    perfilConta,
  };
}

vi.mock("../../../aplicacao/servicos/atribuicoesServico", () => ({
  listarAtribuicoesPorDepartamento: vi.fn(async () => [
    atribuicao(1, "Paula Lima", "Professor"),
    atribuicao(2, "Marcos Silva", "Usuario"),
    atribuicao(3, "Ana Souza", null),
  ]),
  obterAtribuicao: vi.fn(),
  criarAtribuicao: vi.fn(),
  atualizarAtribuicao: vi.fn(),
  encerrarAtribuicao: vi.fn(),
}));

async function montar() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/departamentos/:departamentoId/atribuicoes", component: PaginaAtribuicoesDepartamento }],
  });
  await router.push("/departamentos/7/atribuicoes");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return wrapper;
}

describe("PaginaAtribuicoesDepartamento — aviso da conta (RNF 21.5)", () => {
  // O Dropdown (Select) do PrimeVue consulta matchMedia; o jsdom não o implementa
  // (mesma simulação de PaginaAulasTurma.spec).
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
  });

  beforeEach(() => {
    document.body.innerHTML = "";
  });

  it("tag só para quem não acessa a turma: sem conta e conta de perfil Usuário", async () => {
    const wrapper = await montar();

    const linhas = wrapper.findAll("tbody tr");
    const porNome = Object.fromEntries(
      linhas.map((l) => [l.find(".atribuicao-pessoa span").text(), l]),
    );

    expect(porNome["Paula Lima"]!.find('[data-testid="atribuicao-aviso-conta"]').exists()).toBe(false);
    expect(porNome["Marcos Silva"]!.find('[data-testid="atribuicao-aviso-conta"]').text())
      .toBe("Conta com perfil Usuário — não acessa a turma");
    expect(porNome["Ana Souza"]!.find('[data-testid="atribuicao-aviso-conta"]').text()).toBe("Sem conta");
    expect(wrapper.findAll('[data-testid="atribuicao-aviso-conta"]')).toHaveLength(2);

    wrapper.unmount();
  });
});

// RF21: a função da atribuição é lista fechada (Professor ou Auxiliar). O campo não
// aceita digitação nem no diálogo "Nova atribuição" nem no de edição, e a tela não
// anuncia outras funções.
describe("PaginaAtribuicoesDepartamento — função em lista fechada (RF21)", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
  });

  function conferirDropdownFechado(
    wrapper: Awaited<ReturnType<typeof montar>>,
    labelId: string,
    valorExibido: string,
  ) {
    const dropdowns = wrapper.findAllComponents(Dropdown).filter((d) => d.props("labelId") === labelId);
    expect(dropdowns, labelId).toHaveLength(1);

    const dropdown = dropdowns[0]!;
    expect(dropdown.props("editable")).toBe(false);
    expect((dropdown.props("options") as Array<{ label: string }>).map((o) => o.label)).toEqual([
      "Professor",
      "Auxiliar",
    ]);

    // Sem `editable`, a etiqueta é um span: não há campo de texto para digitar outra função.
    const etiqueta = document.getElementById(labelId);
    expect(etiqueta?.tagName).toBe("SPAN");
    expect(etiqueta?.textContent?.trim()).toBe(valorExibido);
  }

  it("Nova atribuição: Dropdown fechado só com Professor e Auxiliar; subtítulo sem líderes", async () => {
    const wrapper = await montar();

    expect(wrapper.text()).toContain("Gestão de professores e auxiliares da turma");
    expect(wrapper.text()).not.toContain("líderes");

    await wrapper.find("button[aria-label='Nova Atribuição']").trigger("click");
    await flushPromises();

    conferirDropdownFechado(wrapper, "atribuicao-criacao-funcao", "Professor");

    wrapper.unmount();
  });

  it("Editar atribuição: Dropdown fechado só com Professor e Auxiliar", async () => {
    const wrapper = await montar();
    vi.mocked(obterAtribuicao).mockResolvedValueOnce(atribuicao(1, "Paula Lima", "Professor"));

    await wrapper.find("tbody tr .pi-pencil").trigger("click");
    await flushPromises();

    // A atribuição carregada é Auxiliar: o campo mostra o valor gravado.
    conferirDropdownFechado(wrapper, "atribuicao-edicao-funcao", "Auxiliar");

    wrapper.unmount();
  });
});
