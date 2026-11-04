import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Dropdown from "primevue/dropdown";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaMatriculasTurma from "./PaginaMatriculasTurma.vue";
import type { OpcoesConfirmacao } from "../../../aplicacao/servicos/notificacoes";
import {
  listarPessoasDisponiveisDaTurma,
  matricular,
  removerMatricula,
} from "../../../aplicacao/servicos/matriculasServico";
import { listarHistoricoPresencasDaPessoa } from "../../../aplicacao/servicos/presencasPessoaServico";

// Etapa 6.4-C (RF20 / RF27–RF28): no celular a aba Alunos vira cartões — um por
// matrícula, "Responsáveis" como ação principal, "Inativar matrícula" no ⋮ (só com
// matrícula ativa) — e, dentro da Página da Turma, inativar avisa o cabeçalho para
// recontar os alunos ativos (`turmaRecarregarResumo`). Entrega G: a aba abre com as
// matrículas ativas; o filtro (busca por nome e situação) é só no cliente.
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

// Troca a situação do filtro pelo próprio componente (o Select do PrimeVue não abre
// a lista no jsdom).
async function filtrarMatriculas(wrapper: ReturnType<typeof mount>, valor: string) {
  const select = wrapper.findAllComponents(Dropdown).find((c) => c.attributes("data-testid") === "alunos-filtro-situacao");
  expect(select).toBeDefined();
  await select!.vm.$emit("update:modelValue", valor);
  await flushPromises();
}

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
  // O Dropdown (Select) do PrimeVue consulta matchMedia; o jsdom não o implementa.
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
    requireConfirmacao.mockClear();
    vi.mocked(removerMatricula).mockClear();
    document.body.innerHTML = "";
  });

  it("abre com as matrículas ativas; busca por nome e situação filtram no cliente (Entrega G)", async () => {
    const { wrapper } = await montar();
    const titulos = () => wrapper.findAll('[data-testid="aluno-cartao"]').map((c) => c.find(".cartao-item-titulo").text());

    expect(titulos()).toEqual(["Ana", "Bia"]);

    await filtrarMatriculas(wrapper, "inativas");
    expect(titulos()).toEqual(["Caio"]);

    await filtrarMatriculas(wrapper, "todas");
    expect(titulos()).toEqual(["Ana", "Bia", "Caio"]);

    await wrapper.find('[data-testid="alunos-filtro-busca"]').setValue("bi");
    await flushPromises();
    expect(titulos()).toEqual(["Bia"]);

    await wrapper.find('[data-testid="alunos-filtro-busca"]').setValue("zzz");
    await flushPromises();
    expect(titulos()).toEqual([]);
    expect(wrapper.text()).toContain("Nenhum aluno encontrado para os filtros aplicados.");

    wrapper.unmount();
  });

  it("um cartão por aluno, em ordem alfabética, com Responsáveis e o ⋮", async () => {
    const { wrapper } = await montar();
    await filtrarMatriculas(wrapper, "todas");

    const cartoes = wrapper.findAll('[data-testid="aluno-cartao"]');
    expect(cartoes.map((c) => c.find(".cartao-item-titulo").text())).toEqual(["Ana", "Bia", "Caio"]);
    expect(cartoes[0]!.find('[data-testid="aluno-responsaveis"]').exists()).toBe(true);
    expect(cartoes[0]!.text()).toContain("Matrícula ativa");
    expect(cartoes[2]!.text()).toContain("Matrícula inativa");
    expect(cartoes[2]!.text()).toContain("Pessoa: Inativo");
    // O ⋮ aparece em todo cartão: o histórico vale também para a matrícula inativa
    // (7.3); "Inativar matrícula" continua só na ativa (conferido no caso abaixo).
    expect(cartoes[0]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(cartoes[2]!.find('[data-testid="menu-acoes"]').exists()).toBe(true);
    expect(wrapper.find(".p-datatable").exists()).toBe(false);
    wrapper.unmount();
  });

  it("Histórico de presenças pelo ⋮ abre o diálogo e carrega pela pessoa do aluno (CSU14)", async () => {
    const { wrapper } = await montar();
    await filtrarMatriculas(wrapper, "todas");

    // Matrícula inativa (Caio): só o histórico no ⋮, sem "Inativar matrícula".
    await wrapper.findAll('[data-testid="aluno-cartao"]')[2]!.get('[data-testid="menu-acoes"]').trigger("click");
    await flushPromises();
    const itens = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .map((el) => el.textContent?.trim() ?? "");
    expect(itens).toContain("Histórico de presenças");
    expect(itens).not.toContain("Inativar matrícula");

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

  it("inativar pelo ⋮ confirma, inativa (RF29: data de saída, presenças preservadas) e manda o cabeçalho da turma recontar", async () => {
    const { wrapper, recarregarResumo } = await montar();

    await wrapper.findAll('[data-testid="aluno-cartao"]')[0]!.get('[data-testid="menu-acoes"]').trigger("click");
    await flushPromises();
    const remover = Array.from(document.querySelectorAll<HTMLElement>(".p-menu-item-link, .p-menuitem-link"))
      .find((el) => el.textContent?.includes("Inativar matrícula"));
    expect(remover).toBeDefined();
    remover!.click();
    await flushPromises();

    const opcoes = requireConfirmacao.mock.calls[requireConfirmacao.mock.calls.length - 1]![0];
    expect(opcoes.header).toBe("Inativar matrícula");
    expect(opcoes.acceptLabel).toBe("Inativar");
    expect(opcoes.message).toContain("Ana");
    await opcoes.accept!();
    await flushPromises();

    expect(removerMatricula).toHaveBeenCalledWith(7, 2);
    expect(recarregarResumo).toHaveBeenCalledTimes(1);
    wrapper.unmount();
  });

  // Corretiva G4: a validação local fica junto ao campo (sem toast) e o erro de regra da
  // API aparece dentro do diálogo, nunca no topo da página, e some ao alterar um campo.
  it("Matricular: pessoa obrigatória junto ao campo; 400 da API no topo do diálogo, nada na página", async () => {
    const JA_MATRICULADA = "Esta pessoa já está matriculada (ativa) neste departamento.";
    vi.mocked(listarPessoasDisponiveisDaTurma).mockResolvedValueOnce([
      { id: 20, nome: "Dani", email: null, situacao: "Ativo", possuiConta: false } as any,
    ]);
    vi.mocked(matricular).mockRejectedValueOnce({ response: { status: 400, data: { mensagem: JA_MATRICULADA } } });
    const { wrapper, recarregarResumo } = await montar();

    await wrapper.find('button[aria-label="Matricular"]').trigger("click");
    await flushPromises();

    const salvar = () => document.querySelector<HTMLButtonElement>('[data-testid="matricula-dialogo-salvar"]')!.click();
    salvar();
    await flushPromises();
    expect(matricular).not.toHaveBeenCalled();
    expect(document.querySelector('[data-testid="matricula-pessoa-erro"]')!.textContent)
      .toContain("Selecione uma pessoa para matricular.");

    wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "matricula-pessoa")!
      .vm.$emit("update:modelValue", 20);
    await flushPromises();
    expect(document.querySelector('[data-testid="matricula-pessoa-erro"]')).toBeNull();

    salvar();
    await flushPromises();
    expect(matricular).toHaveBeenCalledTimes(1);
    expect(document.querySelector('[data-testid="matricula-dialogo-erro"]')!.textContent).toContain(JA_MATRICULADA);
    expect(wrapper.text()).not.toContain(JA_MATRICULADA);
    expect(recarregarResumo).not.toHaveBeenCalled();

    const observacao = document.querySelector<HTMLInputElement>("#matricula-observacao")!;
    observacao.value = "Transferido";
    observacao.dispatchEvent(new Event("input"));
    await flushPromises();
    expect(document.querySelector('[data-testid="matricula-dialogo-erro"]')).toBeNull();

    wrapper.unmount();
  });
});
