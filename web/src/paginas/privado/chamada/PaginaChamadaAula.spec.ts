import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import ConfirmationService from "primevue/confirmationservice";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaChamadaAula from "./PaginaChamadaAula.vue";
import { setToastHandler } from "../../../aplicacao/servicos/notificacoes";
import { registrarChamada } from "../../../aplicacao/servicos/chamadasServico";

// Ajuste da corretiva da 6.4-B (RF32): "Salvar chamada" acionado duas vezes —
// no mesmo tick (clique duplo) ou com o intervalo de um toque duplo no celular, já
// com a primeira gravação concluída — grava e avisa UMA vez.
vi.mock("../../../aplicacao/servicos/aulasServico", () => ({
  obterAula: vi.fn(async (id: number) => ({
    id,
    data: "2026-10-11",
    tema: "Tema",
    situacao: "EmAberto",
    pendenteFechamento: false,
    quantidadeVisitantes: 0,
    materiaId: 1,
    nomeMateria: "Samuel",
    departamentoId: 7,
    nomeDepartamento: "Jovens",
  })),
  consolidarAula: vi.fn(),
  extrairAlunosSemRegistro: vi.fn(() => []),
}));

vi.mock("../../../aplicacao/servicos/chamadasServico", () => ({
  listarChamadaCompleta: vi.fn(async () => [
    { alunoDepartamentoId: 1, pessoaId: 10, nomeAluno: "Ana", presente: false, observacao: null },
    { alunoDepartamentoId: 2, pessoaId: 11, nomeAluno: "Bia", presente: false, observacao: null },
  ]),
  // Gravação "instantânea", como a API local (menos de 80 ms).
  registrarChamada: vi.fn(async () => undefined),
}));

async function montar() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/aulas/:aulaId/chamada", component: PaginaChamadaAula },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push("/aulas/5/chamada");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue, ConfirmationService] },
  });
  await flushPromises();
  return wrapper;
}

describe("PaginaChamadaAula — Salvar chamada acionado duas vezes", () => {
  const toasts: string[] = [];

  beforeEach(() => {
    vi.useFakeTimers();
    toasts.length = 0;
    setToastHandler((t) => toasts.push(t.detail ?? t.summary));
    vi.mocked(registrarChamada).mockClear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("dois acionamentos no mesmo tick gravam e avisam uma vez", async () => {
    const wrapper = await montar();
    const salvar = wrapper.get('[data-testid="chamada-salvar"]');

    // Sem await entre os dois: o segundo clique chega antes de o Vue redesenhar o botão.
    void salvar.trigger("click");
    void salvar.trigger("click");
    await flushPromises();
    await vi.advanceTimersByTimeAsync(600);

    expect(registrarChamada).toHaveBeenCalledTimes(1);
    expect(toasts).toEqual(["Chamada salva com sucesso."]);
    wrapper.unmount();
  });

  it("toque duplo: o segundo acionamento 80 ms depois, com a gravação já concluída, é ignorado", async () => {
    const wrapper = await montar();
    const salvar = wrapper.get('[data-testid="chamada-salvar"]');

    await salvar.trigger("click");
    await flushPromises(); // primeira gravação concluída
    expect(registrarChamada).toHaveBeenCalledTimes(1);
    expect(salvar.attributes("disabled")).toBeDefined();

    await vi.advanceTimersByTimeAsync(80);
    await salvar.trigger("click");
    await flushPromises();
    expect(registrarChamada).toHaveBeenCalledTimes(1);
    expect(toasts).toEqual(["Chamada salva com sucesso."]);

    // Passada a espera, o botão volta e um novo Salvar é atendido.
    await vi.advanceTimersByTimeAsync(500);
    expect(salvar.attributes("disabled")).toBeUndefined();
    await salvar.trigger("click");
    await flushPromises();
    expect(registrarChamada).toHaveBeenCalledTimes(2);
    wrapper.unmount();
  });
});
