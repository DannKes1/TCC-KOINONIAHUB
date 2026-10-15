import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMatriculasTurma from "./PaginaMatriculasTurma.vue";
import type { OpcoesConfirmacao } from "../../../aplicacao/servicos/notificacoes";
import { removerMatricula } from "../../../aplicacao/servicos/matriculasServico";

// Etapa 6.4-C (RF20 / RF27–RF28): no celular a aba Alunos vira cartões — um por
// matrícula, "Responsáveis" como ação principal, "Remover matrícula" no ⋮ (só com
// matrícula ativa) — e, dentro da Página da Turma, remover avisa o cabeçalho para
// recontar os alunos ativos (`turmaRecarregarResumo`).
vi.mock("../../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => ref(true),
}));

const requireConfirmacao = vi.fn<(opcoes: OpcoesConfirmacao) => void>();
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: requireConfirmacao, close: vi.fn() }),
}));

vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  obterDepartamento: vi.fn(async (id: number) => ({ id, nome: "Jovens", tipo: "EBD", ativo: true })),
}));

vi.mock("../../../aplicacao/servicos/matriculasServico", () => ({
  listarAlunosDaTurma: vi.fn(async () => [
    { matriculaId: 1, pessoaId: 10, nome: "Bia", statusPessoa: "Ativo", matriculaAtiva: true, dataMatricula: "" },
    { matriculaId: 2, pessoaId: 11, nome: "Ana", statusPessoa: "Ativo", matriculaAtiva: true, dataMatricula: "" },
    { matriculaId: 3, pessoaId: 12, nome: "Caio", statusPessoa: "Inativo", matriculaAtiva: false, dataMatricula: "" },
  ]),
  listarPessoasDisponiveisDaTurma: vi.fn(async () => []),
  matricular: vi.fn(async () => undefined),
  removerMatricula: vi.fn(async () => undefined),
}));

vi.mock("../../../aplicacao/servicos/parentescosServico", () => ({
  listarParentescosDaPessoa: vi.fn(async () => []),
}));

async function montar() {
  const recarregarResumo = vi.fn(async () => undefined);
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/departamentos/:departamentoId/matriculas", component: PaginaMatriculasTurma }],
  });
  await router.push("/departamentos/7/matriculas");
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

describe("PaginaMatriculasTurma — cartões no celular (6.4-C)", () => {
  beforeEach(() => {
    requireConfirmacao.mockClear();
    vi.mocked(removerMatricula).mockClear();
    document.body.innerHTML = "";
  });

  it("um cartão por aluno, em ordem alfabética, com Responsáveis e o ⋮", async () => {
    const { wrapper } = await montar();

    const cartoes = wrapper.findAll('[data-testid="aluno-cartao"]');
    expect(cartoes.map((c) => c.find(".cartao-item-titulo").text())).toEqual(["Ana", "Bia", "Caio"]);
    expect(cartoes[0]!.find('[data-testid="aluno-responsaveis"]').exists()).toBe(true);
    expect(cartoes[0]!.text()).toContain("Matrícula ativa");
    expect(cartoes[2]!.text()).toContain("Matrícula inativa");
    expect(cartoes[2]!.text()).toContain("Pessoa: Inativo");
    // Sem matrícula ativa não há ação secundária: o ⋮ nem aparece.
    expect(cartoes[0]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(cartoes[2]!.find('[data-testid="menu-acoes"]').exists()).toBe(false);
    expect(wrapper.find(".p-datatable").exists()).toBe(false);
    wrapper.unmount();
  });

  it("remover pelo ⋮ confirma, remove e manda o cabeçalho da turma recontar", async () => {
    const { wrapper, recarregarResumo } = await montar();

    await wrapper.findAll('[data-testid="aluno-cartao"]')[0]!.get('[data-testid="menu-acoes"]').trigger("click");
    await flushPromises();
    const remover = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .find((el) => el.textContent?.includes("Remover matrícula"));
    expect(remover).toBeDefined();
    remover!.click();
    await flushPromises();

    const opcoes = requireConfirmacao.mock.calls[requireConfirmacao.mock.calls.length - 1]![0];
    expect(opcoes.header).toBe("Remover matrícula");
    expect(opcoes.message).toContain("Ana");
    await opcoes.accept!();
    await flushPromises();

    expect(removerMatricula).toHaveBeenCalledWith(7, 2);
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
    wrapper.unmount();
  });
});
