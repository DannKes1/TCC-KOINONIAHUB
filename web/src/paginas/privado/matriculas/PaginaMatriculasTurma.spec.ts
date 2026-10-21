import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMatriculasTurma from "./PaginaMatriculasTurma.vue";
import type { OpcoesConfirmacao } from "../../../aplicacao/servicos/notificacoes";
import { removerMatricula } from "../../../aplicacao/servicos/matriculasServico";
import { listarHistoricoPresencasDaPessoa } from "../../../aplicacao/servicos/presencasPessoaServico";

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

// Corretiva da 7.3 (CSU14): o histórico do aluno abre pelo ⋮ e carrega pela rota
// da pessoa (RNF 34.3), com um registro de aula Consolidada para os totais.
vi.mock("../../../aplicacao/servicos/presencasPessoaServico", () => ({
  listarHistoricoPresencasDaPessoa: vi.fn(async () => [
    { aulaId: 1, dataAula: "2026-10-05T12:00:00Z", departamentoId: 7, departamentoNome: "Jovens", materiaId: 1, materiaNome: "Gênesis", presente: true, observacao: null, situacaoAula: "Consolidada" },
  ]),
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
    // O ⋮ aparece em todo cartão: o histórico vale também para a matrícula inativa
    // (7.3); "Remover matrícula" continua só na ativa (conferido no caso abaixo).
    expect(cartoes[0]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(cartoes[2]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(wrapper.find(".p-datatable").exists()).toBe(false);
    wrapper.unmount();
  });

  it("Histórico de presenças pelo ⋮ abre o diálogo e carrega pela pessoa do aluno (CSU14)", async () => {
    const { wrapper } = await montar();

    // Matrícula inativa (Caio): só o histórico no ⋮, sem "Remover matrícula".
    await wrapper.findAll('[data-testid="aluno-cartao"]')[2]!.get('[data-testid="menu-acoes"]').trigger("click");
    await flushPromises();
    const itens = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .map((el) => el.textContent?.trim() ?? "");
    expect(itens).toContain("Histórico de presenças");
    expect(itens).not.toContain("Remover matrícula");

    Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .find((el) => el.textContent?.includes("Histórico de presenças"))!
      .click();
    await flushPromises();

    expect(listarHistoricoPresencasDaPessoa).toHaveBeenCalledWith(12);
    const dialogo = document.querySelector('[data-testid="historico-dialogo"]');
    expect(dialogo).not.toBeNull();
    expect(dialogo!.textContent).toContain("Caio");
    expect(dialogo!.querySelector('[data-testid="historico-consolidadas"]')!.textContent?.trim()).toBe("1");
    expect(dialogo!.querySelectorAll('[data-testid="historico-cartao"]')).toHaveLength(1);
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
