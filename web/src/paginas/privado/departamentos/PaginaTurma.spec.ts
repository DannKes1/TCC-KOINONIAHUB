import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaTurma from "./PaginaTurma.vue";
import { usarAutenticacaoStore } from "../../../aplicacao/armazenamentos/autenticacaoStore";

// Etapa 6.4 (RF20): cabeçalho da turma com contadores e equipe, abas como rotas
// filhas (URLs preservadas), aba Atribuições só para a gestão e redirecionamento
// de /departamentos/:id para a aba Aulas. Serviços da API simulados.
vi.mock("../../../aplicacao/servicos/departamentosServico", () => ({
  obterDepartamento: vi.fn(async (id: number) => ({
    id,
    nome: "Jovens",
    tipo: "EBD",
    ativo: true,
    criadoEm: "",
    atualizadoEm: null,
  })),
}));

vi.mock("../../../aplicacao/servicos/matriculasServico", () => ({
  listarAlunosDaTurma: vi.fn(async () => [
    { matriculaId: 1, pessoaId: 10, nome: "Ana", statusPessoa: "Ativo", matriculaAtiva: true, dataMatricula: "" },
    { matriculaId: 2, pessoaId: 11, nome: "Bia", statusPessoa: "Ativo", matriculaAtiva: true, dataMatricula: "" },
    { matriculaId: 3, pessoaId: 12, nome: "Caio", statusPessoa: "Ativo", matriculaAtiva: false, dataMatricula: "" },
  ]),
}));

vi.mock("../../../aplicacao/servicos/materiasServico", () => ({
  listarMaterias: vi.fn(async () => [
    { id: 1, nome: "Samuel", ativo: true, ordemExibicao: 1, departamentoId: 7 },
    { id: 2, nome: "Antiga", ativo: false, ordemExibicao: 2, departamentoId: 7 },
  ]),
}));

vi.mock("../../../aplicacao/servicos/atribuicoesServico", () => ({
  listarAtribuicoesPorDepartamento: vi.fn(async () => [
    { id: 1, pessoaId: 20, pessoaNome: "Paula Lima", departamentoId: 7, departamentoNome: "Jovens", funcao: "Professor", dataInicio: "", dataFim: null, ativo: true },
    { id: 2, pessoaId: 21, pessoaNome: "Marcos Silva", departamentoId: 7, departamentoNome: "Jovens", funcao: "Auxiliar", dataInicio: "", dataFim: null, ativo: true },
    { id: 3, pessoaId: 22, pessoaNome: "Antigo", departamentoId: 7, departamentoNome: "Jovens", funcao: "Professor", dataInicio: "", dataFim: "2026-01-01", ativo: false },
  ]),
}));

vi.mock("../../../aplicacao/servicos/aulasServico", () => ({
  listarAulasPorDepartamento: vi.fn(async () => [
    { id: 1, data: "2026-10-04", situacao: "Consolidada", pendenteFechamento: false },
    { id: 2, data: "2026-10-11", situacao: "EmAberto", pendenteFechamento: true },
    { id: 3, data: "2026-10-18", situacao: "EmAberto", pendenteFechamento: false },
  ]),
}));

// O TabList do PrimeVue mede a largura com ResizeObserver, que o jsdom não tem.
class ResizeObserverFalso {
  observe() {}
  unobserve() {}
  disconnect() {}
}
beforeAll(() => {
  (globalThis as any).ResizeObserver ??= ResizeObserverFalso;
});

const Aba = (nome: string) => ({ template: `<div data-testid="aba-conteudo">${nome}</div>` });

async function montar(perfil: string, caminho: string) {
  const pinia = createPinia();
  setActivePinia(pinia);
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 3600_000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: `${perfil.toLowerCase()}@teste.com`,
    Perfil: perfil,
    IgrejaId: 1,
    PessoaId: 1,
  });

  // Mesma forma das rotas reais: filhas com meta abaDaTurma e redirecionamento.
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: "/departamentos/:departamentoId",
        component: PaginaTurma,
        children: [
          { path: "", redirect: (to) => ({ path: `/departamentos/${String(to.params.departamentoId)}/aulas` }) },
          { path: "matriculas", component: Aba("alunos"), meta: { abaDaTurma: true } },
          { path: "materias", component: Aba("materias"), meta: { abaDaTurma: true } },
          { path: "aulas", component: Aba("aulas"), meta: { abaDaTurma: true } },
          { path: "atribuicoes", component: Aba("atribuicoes"), meta: { abaDaTurma: true } },
        ],
      },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push(caminho);
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    global: { plugins: [pinia, router, PrimeVue] },
  });
  await flushPromises();
  return { wrapper, router };
}

function rotulosDasAbas(wrapper: ReturnType<typeof mount>) {
  return wrapper.findAll(".p-tab").map((t) => t.text().trim());
}

describe("PaginaTurma (RF20, Etapa 6.4)", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("sem aba na URL, abre Aulas; cabeçalho traz nome, contadores e equipe", async () => {
    const { wrapper, router } = await montar("Professor", "/departamentos/7");

    expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");
    expect(wrapper.find("h2.page-header-titulo").text()).toBe("Jovens");
    expect(wrapper.find('[data-testid="turma-situacao"]').text()).toBe("Ativa");
    // 2 matrículas ativas de 3; 2 aulas Em aberto de 3.
    expect(wrapper.find('[data-testid="turma-total-alunos"]').text()).toBe("2");
    expect(wrapper.find('[data-testid="turma-total-aulas-abertas"]').text()).toBe("2");
    // Só atribuições ativas; a encerrada fica de fora.
    const equipe = wrapper.find('[data-testid="turma-equipe"]').text();
    expect(equipe).toContain("Professor(a): Paula Lima");
    expect(equipe).toContain("Auxiliar: Marcos Silva");
    expect(equipe).not.toContain("Antigo");
    expect(wrapper.find('[data-testid="aba-conteudo"]').text()).toBe("aulas");

    wrapper.unmount();
  });

  it("Professor vê Alunos, Matérias e Aulas; a gestão vê também Atribuições", async () => {
    const prof = await montar("Professor", "/departamentos/7/aulas");
    expect(rotulosDasAbas(prof.wrapper)).toEqual(["Alunos", "Matérias", "Aulas"]);
    prof.wrapper.unmount();

    const sup = await montar("Superintendente", "/departamentos/7/aulas");
    expect(rotulosDasAbas(sup.wrapper)).toEqual(["Alunos", "Matérias", "Aulas", "Atribuições"]);
    sup.wrapper.unmount();
  });

  it("tocar numa aba navega para a rota filha correspondente", async () => {
    const { wrapper, router } = await montar("Admin", "/departamentos/7/aulas");

    await wrapper.find('[data-testid="turma-aba-matriculas"]').trigger("click");
    await flushPromises();

    expect(router.currentRoute.value.path).toBe("/departamentos/7/matriculas");
    expect(wrapper.find('[data-testid="aba-conteudo"]').text()).toBe("alunos");

    wrapper.unmount();
  });

  it("o link de voltar leva a gestão a Turmas EBD e o professor a Minhas Turmas", async () => {
    const admin = await montar("Admin", "/departamentos/7/aulas");
    expect(admin.wrapper.find(".page-header-voltar").text()).toContain("Turmas EBD");
    admin.wrapper.unmount();

    const prof = await montar("Professor", "/departamentos/7/aulas");
    expect(prof.wrapper.find(".page-header-voltar").text()).toContain("Minhas Turmas");
    prof.wrapper.unmount();
  });
});
