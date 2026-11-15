import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMinhasTurmas from "./PaginaMinhasTurmas.vue";
import { listarMinhasTurmas, type MinhaTurmaVM } from "../../aplicacao/servicos/meusDadosServico";

// Ajuste C3 (RF5 / CSU06): filtro de situação em Minhas Turmas, no padrão de Turmas EBD,
// começando em Ativas; filtro no cliente, para os cartões e para a tabela, com a mensagem
// de lista vazia de acordo com o filtro. Serviço da API simulado.
const compacta = ref(true);
vi.mock("../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => compacta,
}));

vi.mock("../../aplicacao/servicos/meusDadosServico", () => ({
  listarMinhasTurmas: vi.fn(),
}));

function turma(departamentoId: number, nome: string, ativo: boolean): MinhaTurmaVM {
  return { departamentoId, nome, tipo: "EBD", ativo, vinculo: "Aluno", responsavel: null };
}

const ATIVA = turma(7, "Jovens", true);
const INATIVA = turma(9, "Turma antiga", false);

async function montar(turmas: MinhaTurmaVM[]) {
  vi.mocked(listarMinhasTurmas).mockResolvedValue(turmas);
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/:pathMatch(.*)*", component: { template: "<div />" } }],
  });
  await router.push("/minhas-turmas");
  await router.isReady();

  const wrapper = mount(PaginaMinhasTurmas, { global: { plugins: [router, PrimeVue] } });
  await flushPromises();
  return wrapper;
}

async function filtrar(wrapper: Awaited<ReturnType<typeof montar>>, opcao: string) {
  const botao = wrapper
    .get('[data-testid="minhas-turmas-filtro-situacao"]')
    .findAll("button")
    .find((b) => b.text() === opcao);
  expect(botao, `opção ${opcao} do filtro`).toBeDefined();
  await botao!.trigger("click");
  await flushPromises();
}

function titulosDosCartoes(wrapper: Awaited<ReturnType<typeof montar>>) {
  return wrapper
    .findAll('[data-testid="minhas-turmas-cartao"]')
    .map((c) => c.find(".cartao-item-titulo").text());
}

describe("PaginaMinhasTurmas — filtro de situação (ajuste C3)", () => {
  beforeEach(() => {
    compacta.value = true;
  });

  it("começa em Ativas e esconde a turma inativa", async () => {
    const wrapper = await montar([INATIVA, ATIVA]);

    const ativas = wrapper
      .get('[data-testid="minhas-turmas-filtro-situacao"]')
      .findAll("button")
      .find((b) => b.text() === "Ativas")!;
    expect(ativas.attributes("aria-pressed")).toBe("true");
    expect(titulosDosCartoes(wrapper)).toEqual(["Jovens"]);

    // Clicar de novo na opção marcada não deixa o filtro vazio (allowEmpty = false).
    await filtrar(wrapper, "Ativas");
    expect(ativas.attributes("aria-pressed")).toBe("true");
    expect(titulosDosCartoes(wrapper)).toEqual(["Jovens"]);
    wrapper.unmount();
  });

  it("Inativas mostra só a turma inativa", async () => {
    const wrapper = await montar([INATIVA, ATIVA]);

    await filtrar(wrapper, "Inativas");

    expect(titulosDosCartoes(wrapper)).toEqual(["Turma antiga"]);
    expect(wrapper.get('[data-testid="minhas-turmas-cartao"]').text()).toContain("Inativa");
    wrapper.unmount();
  });

  it("Todas mostra as duas, também na tabela do desktop", async () => {
    compacta.value = false;
    const wrapper = await montar([INATIVA, ATIVA]);
    expect(wrapper.findAll("tbody tr")).toHaveLength(1);
    expect(wrapper.get("tbody").text()).toContain("Jovens");

    await filtrar(wrapper, "Todas");

    const linhas = wrapper.findAll("tbody tr");
    expect(linhas).toHaveLength(2);
    expect(wrapper.get("tbody").text()).toContain("Jovens");
    expect(wrapper.get("tbody").text()).toContain("Turma antiga");
    wrapper.unmount();
  });

  it("a lista vazia tem mensagem de acordo com o filtro escolhido", async () => {
    const soInativa = await montar([INATIVA]);
    expect(soInativa.text()).toContain("Você não possui vínculo com turmas ativas.");
    soInativa.unmount();

    const soAtiva = await montar([ATIVA]);
    await filtrar(soAtiva, "Inativas");
    expect(soAtiva.text()).toContain("Você não possui vínculo com turmas inativas.");
    soAtiva.unmount();

    // Na tabela do desktop, a mesma mensagem por filtro.
    compacta.value = false;
    const soInativaNaTabela = await montar([INATIVA]);
    expect(soInativaNaTabela.get("tbody").text()).toContain("Você não possui vínculo com turmas ativas.");
    soInativaNaTabela.unmount();

    const soAtivaNaTabela = await montar([ATIVA]);
    await filtrar(soAtivaNaTabela, "Inativas");
    expect(soAtivaNaTabela.get("tbody").text()).toContain("Você não possui vínculo com turmas inativas.");
    soAtivaNaTabela.unmount();

    const semVinculo = await montar([]);
    await filtrar(semVinculo, "Todas");
    expect(semVinculo.get("tbody").text()).toContain("Você ainda não possui vínculo com turmas.");
    semVinculo.unmount();
  });
});
