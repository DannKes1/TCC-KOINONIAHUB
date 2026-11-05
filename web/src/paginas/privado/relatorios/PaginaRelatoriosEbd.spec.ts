import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import InputNumber from "primevue/inputnumber";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaRelatoriosEbd from "./PaginaRelatoriosEbd.vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";
import { obterPainelAcompanhamento } from "../../../aplicacao/servicos/relatoriosServico";

// RF36: os limiares de atenção e crítico e as faltas consecutivas do crítico são
// ajustáveis na tela do Acompanhamento e chegam à API como parâmetros; os padrões
// são os da API (75 %, 50 % e 3).
vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  listarDepartamentos: vi.fn(async () => [
    { id: 7, nome: "Jovens", tipo: "EBD", ativo: true, criadoEm: "", atualizadoEm: null },
  ]),
}));

vi.mock("../../../aplicacao/servicos/relatoriosServico", () => ({
  obterFrequenciaTurma: vi.fn(async () => ({
    departamentoId: 7, nomeDepartamento: "Jovens", dataInicio: "", dataFim: "",
    totalAulas: 0, totalAlunos: 0, totalPresentes: 0, totalAusentesMarcados: 0,
    totalNaoRegistrado: 0, percentualPresencaGeral: 0, alunos: [], aulas: [],
    aulasPendentes: [],
  })),
  obterPainelAcompanhamento: vi.fn(async () => ({
    departamentoId: 7, nomeDepartamento: "Jovens", dataInicio: "", dataFim: "",
    totalAulas: 0, totalAlunos: 0, limiarAtencao: 75, limiarCritico: 50,
    faltasConsecutivasCritico: 3, totalCritico: 0, totalAtencao: 0, alunos: [],
    aulasPendentes: [],
  })),
  obterRankingFaltas: vi.fn(async () => ({
    departamentoId: 7, nomeDepartamento: "Jovens", dataInicio: "", dataFim: "", itens: [], aulasPendentes: [],
  })),
  obterResumoDia: vi.fn(async () => null),
}));

async function montar() {
  const pinia = createPinia();
  setActivePinia(pinia);
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: "admin@teste.com",
    Perfil: "Admin",
    IgrejaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/relatorios/ebd", component: PaginaRelatoriosEbd }],
  });
  await router.push("/relatorios/ebd");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: {
      plugins: [router, pinia, PrimeVue],
      directives: { tooltip: Tooltip },
      // O gráfico (Chart.js) precisa de canvas, que o jsdom não tem.
      stubs: { Chart: true },
    },
  });
  await flushPromises();
  return wrapper;
}

function campoNumerico(wrapper: ReturnType<typeof mount>, inputId: string) {
  const campo = wrapper
    .findAllComponents(InputNumber)
    .find((c) => c.props("inputId") === inputId);
  expect(campo, inputId).toBeDefined();
  return campo!;
}

function ultimaChamada() {
  const chamadas = vi.mocked(obterPainelAcompanhamento).mock.calls;
  return chamadas[chamadas.length - 1]![0];
}

describe("PaginaRelatoriosEbd — limiares do Acompanhamento (RF36)", () => {
  let wrapperAtual: ReturnType<typeof mount> | null = null;

  beforeAll(() => {
    // Dropdown (Select) e Tabs do PrimeVue consultam matchMedia e ResizeObserver.
    Object.defineProperty(window, "matchMedia", {
      writable: true,
      value: vi.fn().mockImplementation((query: string) => ({
        matches: false, media: query, onchange: null,
        addListener: vi.fn(), removeListener: vi.fn(),
        addEventListener: vi.fn(), removeEventListener: vi.fn(), dispatchEvent: vi.fn(),
      })),
    });
    Object.defineProperty(window, "ResizeObserver", {
      writable: true,
      value: class { observe() {} unobserve() {} disconnect() {} },
    });
  });

  beforeEach(() => {
    localStorage.clear();
    vi.mocked(obterPainelAcompanhamento).mockClear();
  });

  afterEach(() => {
    wrapperAtual?.unmount();
    wrapperAtual = null;
    document.body.innerHTML = "";
  });

  it("a aba Acompanhamento expõe os dois limiares e as faltas seguidas com os padrões da API", async () => {
    const wrapper = await montar();
    wrapperAtual = wrapper;

    await wrapper.get('[role="tab"][aria-label="Acompanhamento"], [role="tab"]:nth-of-type(2)').trigger("click");
    await flushPromises();

    expect(campoNumerico(wrapper, "relatorio-filtro-limiar-atencao").props("modelValue")).toBe(75);
    expect(campoNumerico(wrapper, "relatorio-filtro-limiar-critico").props("modelValue")).toBe(50);
    expect(campoNumerico(wrapper, "relatorio-filtro-faltas-seguidas").props("modelValue")).toBe(3);
    // Corretiva G5: os rótulos dizem a condição de cada campo.
    expect(wrapper.get('[data-testid="relatorio-limiar-atencao-rotulo"]').text()).toBe("Atenção se frequência ≤ (%)");
    expect(wrapper.get('[data-testid="relatorio-limiar-critico-rotulo"]').text()).toBe("Crítico se frequência ≤ (%)");
    expect(wrapper.get('[data-testid="relatorio-faltas-seguidas-rotulo"]').text()).toBe("Crítico se faltas seguidas ≥");

    // Ao abrir a aba, a busca automática já envia os três parâmetros.
    expect(obterPainelAcompanhamento).toHaveBeenCalledTimes(1);
    expect(ultimaChamada()).toMatchObject({
      departamentoId: 7,
      limiarAtencao: 75,
      limiarCritico: 50,
      faltasConsecutivasCritico: 3,
    });
  });

  it("alterar o limiar crítico e buscar envia o novo valor à API", async () => {
    const wrapper = await montar();
    wrapperAtual = wrapper;

    await wrapper.get('[role="tab"][aria-label="Acompanhamento"], [role="tab"]:nth-of-type(2)').trigger("click");
    await flushPromises();

    campoNumerico(wrapper, "relatorio-filtro-limiar-critico").vm.$emit("update:modelValue", 40);
    campoNumerico(wrapper, "relatorio-filtro-limiar-atencao").vm.$emit("update:modelValue", 80);
    await flushPromises();

    const buscar = wrapper.findAll("button").find((b) => b.text().includes("Buscar"));
    expect(buscar).toBeDefined();
    await buscar!.trigger("click");
    await flushPromises();

    expect(obterPainelAcompanhamento).toHaveBeenCalledTimes(2);
    expect(ultimaChamada()).toMatchObject({ limiarAtencao: 80, limiarCritico: 40, faltasConsecutivasCritico: 3 });
  });

  // Corretiva G5: limiares, regra e descrição do relatório só na aba Acompanhamento; turma
  // e período continuam nas outras abas.
  it("limiares, regra e descrição aparecem só na aba Acompanhamento", async () => {
    const wrapper = await montar();
    wrapperAtual = wrapper;
    const SO_NO_ACOMPANHAMENTO = [
      "#relatorio-filtro-limiar-atencao",
      "#relatorio-filtro-limiar-critico",
      "#relatorio-filtro-faltas-seguidas",
      '[data-testid="relatorio-acompanhamento-regra"]',
      '[data-testid="relatorio-acompanhamento-descricao"]',
    ];
    const abas = wrapper.findAll('[role="tab"]');

    for (const indice of [0, 2]) {
      await abas[indice]!.trigger("click");
      await flushPromises();
      for (const seletor of SO_NO_ACOMPANHAMENTO) expect(wrapper.find(seletor).exists(), seletor).toBe(false);
      expect(wrapper.find("#relatorio-filtro-data-inicio").exists()).toBe(true);
    }

    await abas[1]!.trigger("click");
    await flushPromises();
    for (const seletor of SO_NO_ACOMPANHAMENTO) expect(wrapper.find(seletor).exists(), seletor).toBe(true);
    expect(wrapper.get('[data-testid="relatorio-acompanhamento-descricao"]').text()).toBe(
      "Este relatório aponta os alunos que precisam de acompanhamento: para cada aluno com " +
        "matrícula ativa na turma, no período escolhido, calcula a frequência (presenças " +
        "divididas pelas aulas consolidadas em que o aluno tem chamada registrada) e as faltas " +
        "seguidas mais recentes (contadas da última aula consolidada para trás, até a última " +
        "presença), e classifica em Crítico, Atenção ou regular; a lista mostra só Crítico e " +
        "Atenção. Ajuste os limiares conforme o critério da igreja.",
    );
  });

  // Corretiva G5: a regra aplicada fica escrita abaixo dos filtros, com os padrões da API.
  it("mostra a regra do Acompanhamento com os valores padrão", async () => {
    const wrapper = await montar();
    wrapperAtual = wrapper;

    expect(wrapper.find('[data-testid="relatorio-acompanhamento-regra"]').exists()).toBe(false);
    await wrapper.get('[role="tab"][aria-label="Acompanhamento"], [role="tab"]:nth-of-type(2)').trigger("click");
    await flushPromises();

    expect(wrapper.get('[data-testid="relatorio-acompanhamento-regra"]').text()).toBe(
      "Atenção: frequência igual ou abaixo de 75 % ou 2 faltas seguidas (número fixo). " +
        "Crítico: frequência igual ou abaixo de 50 % ou 3 faltas seguidas.",
    );
  });

  // Corretiva G5: o texto acompanha os valores digitados e os ajustes da API (crítico acima
  // da atenção vale a atenção; com 2 faltas seguidas no crítico, a atenção fica só com a
  // frequência).
  it("o texto da regra acompanha os valores digitados e os ajustes da API", async () => {
    const wrapper = await montar();
    wrapperAtual = wrapper;

    await wrapper.get('[role="tab"][aria-label="Acompanhamento"], [role="tab"]:nth-of-type(2)').trigger("click");
    await flushPromises();

    campoNumerico(wrapper, "relatorio-filtro-limiar-atencao").vm.$emit("update:modelValue", 80);
    campoNumerico(wrapper, "relatorio-filtro-limiar-critico").vm.$emit("update:modelValue", 60.5);
    campoNumerico(wrapper, "relatorio-filtro-faltas-seguidas").vm.$emit("update:modelValue", 4);
    await flushPromises();
    expect(wrapper.get('[data-testid="relatorio-acompanhamento-regra"]').text()).toBe(
      "Atenção: frequência igual ou abaixo de 80 % ou 2 faltas seguidas (número fixo). " +
        "Crítico: frequência igual ou abaixo de 60,5 % ou 4 faltas seguidas.",
    );

    campoNumerico(wrapper, "relatorio-filtro-limiar-critico").vm.$emit("update:modelValue", 90);
    campoNumerico(wrapper, "relatorio-filtro-faltas-seguidas").vm.$emit("update:modelValue", 2);
    await flushPromises();
    expect(wrapper.get('[data-testid="relatorio-acompanhamento-regra"]').text()).toBe(
      "Atenção: frequência igual ou abaixo de 80 %. " +
        "Crítico: frequência igual ou abaixo de 80 % ou 2 faltas seguidas.",
    );
  });
});
