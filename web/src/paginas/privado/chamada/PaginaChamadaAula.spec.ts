import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter, type Router } from "vue-router";

import PaginaChamadaAula from "./PaginaChamadaAula.vue";
import {
  setToastHandler,
  type OpcoesConfirmacao,
} from "../../../aplicacao/servicos/notificacoes";
import {
  listarChamadaCompleta,
  registrarChamada,
} from "../../../aplicacao/servicos/chamadasServico";
import { consolidarAula } from "../../../aplicacao/servicos/aulasServico";
import type { ItemChamadaCompletaVM } from "../../../aplicacao/modelos/dtos";

// Página da chamada (RF32 / RF33, CSU10 / CSU11):
// - ajuste da corretiva da 6.4-B: "Salvar chamada" acionado duas vezes grava e avisa
//   UMA vez (mesmo tick, ou toque duplo com a primeira gravação já concluída);
// - 6.4-C (a): "Sem registro" desde a carga e a confirmação do Consolidar nomeando
//   quem será gravado como ausente, gravando antes de consolidar (CSU11 passo 2);
// - 6.4-C (b): sair ou recarregar com alterações não salvas pede confirmação.
// O serviço de confirmação é simulado para o spec ler as opções e acionar
// accept/reject diretamente.
const requireConfirmacao = vi.fn<(opcoes: OpcoesConfirmacao) => void>();
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: requireConfirmacao, close: vi.fn() }),
}));

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
  consolidarAula: vi.fn(async () => undefined),
  extrairAlunosSemRegistro: vi.fn(() => []),
}));

vi.mock("../../../aplicacao/servicos/chamadasServico", () => ({
  listarChamadaCompleta: vi.fn(),
  // Gravação "instantânea", como a API local (menos de 80 ms).
  registrarChamada: vi.fn(async () => []),
}));

function item(id: number, nome: string, registrado: boolean, presente = false): ItemChamadaCompletaVM {
  return { alunoDepartamentoId: id, pessoaId: id * 10, nomeAluno: nome, presente, observacao: null, registrado };
}

const TODOS_REGISTRADOS = [item(1, "Ana", true), item(2, "Bia", true)];

async function montar(itens: ItemChamadaCompletaVM[] = TODOS_REGISTRADOS) {
  vi.mocked(listarChamadaCompleta).mockImplementation(async () => itens.map((i) => ({ ...i })));

  const router: Router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/aulas/:aulaId/chamada", component: PaginaChamadaAula },
      { path: "/departamentos/:id/aulas", component: { template: "<div>aulas</div>" } },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push("/aulas/5/chamada");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return { wrapper, router };
}

function ultimaConfirmacao(): OpcoesConfirmacao {
  const chamadas = requireConfirmacao.mock.calls;
  expect(chamadas.length).toBeGreaterThan(0);
  return chamadas[chamadas.length - 1]![0];
}

describe("PaginaChamadaAula", () => {
  const toasts: string[] = [];

  beforeEach(() => {
    vi.useFakeTimers();
    toasts.length = 0;
    setToastHandler((t) => toasts.push(t.detail ?? t.summary));
    vi.mocked(registrarChamada).mockClear();
    vi.mocked(consolidarAula).mockClear();
    requireConfirmacao.mockClear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe("Salvar chamada acionado duas vezes (ajuste da corretiva da 6.4-B)", () => {
    it("dois acionamentos no mesmo tick gravam e avisam uma vez", async () => {
      const { wrapper } = await montar();
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
      const { wrapper } = await montar();
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

  describe("alunos sem registro (6.4-C a — RNFs 32.6/33.3, CSU11 passo 2)", () => {
    const COM_SEM_REGISTRO = [item(1, "Ana", true, true), item(2, "Bia", false), item(3, "Caio", false)];

    it("destaca desde a carga quem não tem registro e avisa quantos são", async () => {
      const { wrapper } = await montar(COM_SEM_REGISTRO);

      const linhas = wrapper.findAll('[data-testid="chamada-linha"]');
      expect(linhas.map((l) => l.classes().includes("linha-chamada-sem-registro"))).toEqual([false, true, true]);
      expect(wrapper.get('[data-testid="chamada-mensagem"]').text()).toContain("2 alunos sem registro");
      // Sem alteração: o que está na tela é o que a API devolveu.
      expect(wrapper.get('[data-testid="chamada-estado-salvo"]').text()).toContain("Tudo salvo");
      wrapper.unmount();
    });

    it("quem o professor marca deixa de ser 'sem registro'", async () => {
      const { wrapper } = await montar(COM_SEM_REGISTRO);

      await wrapper.findAll('[data-testid="chamada-linha"]')[1]!.trigger("click");
      await flushPromises();

      const linhas = wrapper.findAll('[data-testid="chamada-linha"]');
      expect(linhas.map((l) => l.classes().includes("linha-chamada-sem-registro"))).toEqual([false, false, true]);
      expect(wrapper.get('[data-testid="chamada-mensagem"]').text()).toContain("1 aluno sem registro");
      wrapper.unmount();
    });

    it("Consolidar nomeia quem será gravado como ausente e, confirmado, grava antes de consolidar", async () => {
      const { wrapper } = await montar(COM_SEM_REGISTRO);

      await wrapper.get('[data-testid="chamada-consolidar"]').trigger("click");
      const opcoes = ultimaConfirmacao();
      expect(opcoes.header).toBe("Consolidar aula");
      expect(opcoes.message).toContain("serão gravados como ausentes");
      expect(opcoes.itens).toEqual(["Bia", "Caio"]);
      expect(opcoes.acceptLabel).toBe("Salvar e consolidar");

      // A ordem importa: a chamada é gravada e só então a aula é consolidada.
      const ordem: string[] = [];
      vi.mocked(registrarChamada).mockImplementationOnce(async () => { ordem.push("gravar"); return []; });
      vi.mocked(consolidarAula).mockImplementationOnce(async () => { ordem.push("consolidar"); });

      opcoes.accept!();
      await flushPromises();
      expect(ordem).toEqual(["gravar", "consolidar"]);
      expect(toasts).toEqual(["Aula consolidada com sucesso."]);
      wrapper.unmount();
    });

    it("sem pendência, a confirmação é a simples e não grava antes", async () => {
      const { wrapper } = await montar();

      await wrapper.get('[data-testid="chamada-consolidar"]').trigger("click");
      const opcoes = ultimaConfirmacao();
      expect(opcoes.itens).toBeUndefined();
      expect(opcoes.acceptLabel).toBe("Consolidar");

      opcoes.accept!();
      await flushPromises();
      expect(registrarChamada).not.toHaveBeenCalled();
      expect(consolidarAula).toHaveBeenCalledTimes(1);
      wrapper.unmount();
    });
  });

  describe("alterações não salvas (6.4-C b)", () => {
    it("sair pela rota pede confirmação; recusar mantém na chamada, aceitar sai", async () => {
      const { wrapper, router } = await montar();

      await wrapper.findAll('[data-testid="chamada-linha"]')[0]!.trigger("click");
      await flushPromises();
      expect(wrapper.get('[data-testid="chamada-estado-salvo"]').text()).toContain("Alterações não salvas");

      // Recusa: continua na chamada.
      let navegacao = router.push("/departamentos/7/aulas");
      await flushPromises();
      let opcoes = ultimaConfirmacao();
      expect(opcoes.header).toBe("Sair da chamada");
      opcoes.reject!();
      await navegacao;
      expect(router.currentRoute.value.path).toBe("/aulas/5/chamada");

      // Aceita: sai.
      navegacao = router.push("/departamentos/7/aulas");
      await flushPromises();
      opcoes = ultimaConfirmacao();
      opcoes.accept!();
      await navegacao;
      expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");
      wrapper.unmount();
    });

    it("sem alteração, sai sem perguntar", async () => {
      const { wrapper, router } = await montar();

      await router.push("/departamentos/7/aulas");
      expect(requireConfirmacao).not.toHaveBeenCalled();
      expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");
      wrapper.unmount();
    });

    it("fechar ou recarregar a aba com alteração aciona o aviso do navegador", async () => {
      const { wrapper } = await montar();
      const dispararBeforeUnload = () => {
        const evento = new Event("beforeunload", { cancelable: true });
        window.dispatchEvent(evento);
        return evento.defaultPrevented;
      };

      expect(dispararBeforeUnload()).toBe(false);

      await wrapper.findAll('[data-testid="chamada-linha"]')[0]!.trigger("click");
      await flushPromises();
      expect(dispararBeforeUnload()).toBe(true);

      wrapper.unmount();
      expect(dispararBeforeUnload()).toBe(false);
    });

    it("Recarregar com alteração pede confirmação antes de descartar", async () => {
      const { wrapper } = await montar();
      vi.mocked(listarChamadaCompleta).mockClear();

      await wrapper.findAll('[data-testid="chamada-linha"]')[0]!.trigger("click");
      await flushPromises();

      await wrapper.get('[data-testid="menu-acoes"]').trigger("click");
      await flushPromises();
      const recarregar = Array.from(document.querySelectorAll(".p-menu-item-link, .p-menuitem-link"))
        .find((el) => el.textContent?.includes("Recarregar")) as HTMLElement | undefined;
      expect(recarregar).toBeDefined();
      recarregar!.click();
      await flushPromises();

      const opcoes = ultimaConfirmacao();
      expect(opcoes.header).toBe("Recarregar chamada");
      expect(listarChamadaCompleta).not.toHaveBeenCalled();

      opcoes.accept!();
      await flushPromises();
      expect(listarChamadaCompleta).toHaveBeenCalledTimes(1);
      expect(wrapper.get('[data-testid="chamada-estado-salvo"]').text()).toContain("Tudo salvo");
      wrapper.unmount();
    });
  });
});
