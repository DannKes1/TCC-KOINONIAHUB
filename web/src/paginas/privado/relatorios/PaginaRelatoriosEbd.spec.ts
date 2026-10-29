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
});
