import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMinhaFrequencia from "./PaginaMinhaFrequencia.vue";
import { esquecerOrigens, registrarOrigem } from "../../../aplicacao/rotas/origemNavegacao";

// Ajustes D1 e D3: aberta pelo Painel, Minha frequência volta para o Painel; pelas outras
// telas (ou sem origem conhecida), para Minhas Turmas. O subtítulo diz "Turma <nome>".
// Serviço da API simulado.
const compacta = ref(true);
vi.mock("../../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => compacta,
}));

vi.mock("../../../aplicacao/servicos/meusDadosServico", () => ({
  obterMinhaFrequencia: vi.fn(async (departamentoId: number) => ({
    departamentoId,
    nomeDepartamento: "Jovens",
    // Meio do dia: a data local é a mesma em qualquer fuso das Américas.
    dataInicio: "2026-08-16T15:00:00Z",
    dataFim: "2026-11-14T15:00:00Z",
    totalAulas: 0,
    presentes: 0,
    ausentesMarcados: 0,
    naoRegistrado: 0,
    percentualPresenca: 0,
    aulas: [],
    aulasPendentes: [],
  })),
}));

async function montar(...caminhos: string[]) {
  const Vazio = { template: "<div />" };
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/", component: Vazio },
      { path: "/minhas-turmas", component: Vazio },
      { path: "/departamentos/:departamentoId/minha-frequencia", component: PaginaMinhaFrequencia },
    ],
  });
  router.afterEach(registrarOrigem);
  for (const caminho of caminhos) await router.push(caminho);
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, { global: { plugins: [router, PrimeVue] } });
  await flushPromises();
  return { wrapper, router };
}

describe("PaginaMinhaFrequencia — voltar e cabeçalho (ajustes D1 e D3)", () => {
  beforeEach(() => {
    esquecerOrigens();
  });

  it("o subtítulo é \"Turma <nome>\"", async () => {
    const { wrapper } = await montar("/minhas-turmas", "/departamentos/7/minha-frequencia");

    expect(wrapper.get("h2.page-header-titulo").text()).toBe("Minha frequência");
    expect(wrapper.get(".page-header-subtitulo").text()).toBe("Turma Jovens");
    expect(wrapper.get('[data-testid="minha-frequencia-periodo"]').text()).toContain("16/08/2026 a 14/11/2026");
    wrapper.unmount();
  });

  it("aberta pelo Painel volta para o Painel; por Minhas Turmas ou sem origem, para Minhas Turmas", async () => {
    const peloPainel = await montar("/", "/departamentos/7/minha-frequencia");
    expect(peloPainel.wrapper.get(".page-header-voltar").text()).toBe("Painel");
    await peloPainel.wrapper.get(".page-header-voltar").trigger("click");
    await flushPromises();
    expect(peloPainel.router.currentRoute.value.path).toBe("/");
    peloPainel.wrapper.unmount();

    const porMinhasTurmas = await montar("/minhas-turmas", "/departamentos/7/minha-frequencia");
    expect(porMinhasTurmas.wrapper.get(".page-header-voltar").text()).toBe("Minhas Turmas");
    porMinhasTurmas.wrapper.unmount();

    esquecerOrigens();
    const semOrigem = await montar("/departamentos/7/minha-frequencia");
    expect(semOrigem.wrapper.get(".page-header-voltar").text()).toBe("Minhas Turmas");
    await semOrigem.wrapper.get(".page-header-voltar").trigger("click");
    await flushPromises();
    expect(semOrigem.router.currentRoute.value.path).toBe("/minhas-turmas");
    semOrigem.wrapper.unmount();
  });
});
