import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { ref } from "vue";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaPainel from "./PaginaPainel.vue";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";
import { listarAulasPorDepartamento } from "../../aplicacao/servicos/aulasServico";
import { listarMinhasTurmas, type MinhaTurmaVM } from "../../aplicacao/servicos/meusDadosServico";
import type { AulaVM } from "../../aplicacao/modelos/dtos";

// Ajustes C2 e C2b (RF3: o Painel segue o perfil). C2: nos Painéis administrativo e do
// Professor, a conta que também é aluno(a) vê no fim "Turmas em que sou aluno(a)", com
// "Minha frequência", só para as turmas ativas. C2b: no Painel administrativo, a conta com
// atribuição vê, antes das tabelas, "Aulas em aberto das minhas turmas", feito das aulas
// que o painel já carrega.
// Se a carga das turmas da conta falhar, o resto do painel continua. Serviços simulados.
const compacta = ref(true);
vi.mock("../../aplicacao/composables/usarTelaCompacta", () => ({
  usarTelaCompacta: () => compacta,
}));

vi.mock("../../aplicacao/servicos/departamentosServico", () => ({
  listarDepartamentos: vi.fn(async () => [
    { id: 7, nome: "Jovens", tipo: "EBD", ativo: true },
    { id: 8, nome: "Adultos", tipo: "EBD", ativo: true },
  ]),
}));

vi.mock("../../aplicacao/servicos/pessoasServico", () => ({
  listarPessoas: vi.fn(async () => []),
}));

vi.mock("../../aplicacao/servicos/presencasPessoaServico", () => ({
  listarHistoricoPresencasDaPessoa: vi.fn(async () => []),
}));

vi.mock("../../aplicacao/servicos/aulasServico", () => ({
  listarAulasPorDepartamento: vi.fn(),
}));

vi.mock("../../aplicacao/servicos/meusDadosServico", () => ({
  listarMinhasTurmas: vi.fn(),
}));

function aula(id: number, departamentoId: number, situacao: AulaVM["situacao"]): AulaVM {
  return {
    id,
    data: `2026-11-0${(id % 9) + 1}T12:00:00Z`,
    tema: "Tema",
    conteudo: null,
    situacao,
    pendenteFechamento: false,
    possuiPresencas: false,
    quantidadeVisitantes: 0,
    materiaId: departamentoId,
    nomeMateria: `Matéria ${departamentoId}`,
    departamentoId,
    nomeDepartamento: departamentoId === 7 ? "Jovens" : "Adultos",
    professorId: 3,
    nomeProfessor: "Professor",
    criadoEm: "2026-11-01T12:00:00Z",
  };
}

// Turma 7 (Jovens): uma aula Em aberto (21) e uma Consolidada (22). Turma 8 (Adultos):
// uma aula Em aberto (31).
const aulasPorTurma: Record<number, AulaVM[]> = {
  7: [aula(21, 7, "EmAberto"), aula(22, 7, "Consolidada")],
  8: [aula(31, 8, "EmAberto")],
};

function turma(departamentoId: number, nome: string, vinculo: string, ativo = true): MinhaTurmaVM {
  return { departamentoId, nome, tipo: "EBD", ativo, vinculo, responsavel: null };
}

async function montar(perfil: string, turmasDaConta: MinhaTurmaVM[] | Error) {
  if (turmasDaConta instanceof Error) vi.mocked(listarMinhasTurmas).mockRejectedValue(turmasDaConta);
  else vi.mocked(listarMinhasTurmas).mockResolvedValue(turmasDaConta);

  const pinia = createPinia();
  setActivePinia(pinia);
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: `${perfil.toLowerCase()}@teste.com`,
    Perfil: perfil,
    IgrejaId: 1,
    PessoaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/:pathMatch(.*)*", component: { template: "<div />" } }],
  });
  await router.push("/");
  await router.isReady();

  const wrapper = mount(PaginaPainel, {
    attachTo: document.body,
    global: { plugins: [pinia, router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return { wrapper, router };
}

// O bloco b vem depois do bloco a no documento.
function vemDepois(a: Element, b: Element) {
  return Boolean(a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING);
}

describe("PaginaPainel — blocos da própria conta (ajustes C2 e C2b)", () => {
  beforeEach(() => {
    localStorage.clear();
    document.body.innerHTML = "";
    compacta.value = true;
    vi.mocked(listarAulasPorDepartamento).mockImplementation(async (id: number) => aulasPorTurma[id] ?? []);
  });

  it("Admin matriculado(a) vê no fim Turmas em que sou aluno(a), só com as turmas ativas", async () => {
    const { wrapper, router } = await montar("Admin", [
      turma(7, "Jovens", "Aluno"),
      turma(9, "Turma antiga", "Aluno", false),
    ]);

    const bloco = wrapper.get('[data-testid="painel-turmas-como-aluno"]');
    expect(bloco.text()).toContain("Turmas em que sou aluno(a)");
    expect(vemDepois(wrapper.get('[data-testid="painel-gestao-tabelas"]').element, bloco.element)).toBe(true);

    const cartoes = bloco.findAll('[data-testid="painel-turma-como-aluno"]');
    expect(cartoes.map((c) => c.find(".cartao-item-titulo").text())).toEqual(["Jovens"]);
    expect(bloco.text()).not.toContain("Turma antiga");

    await bloco.get('[data-testid="painel-minha-frequencia"]').trigger("click");
    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/departamentos/7/minha-frequencia");
    wrapper.unmount();
  });

  it("Professor matriculado(a) vê no fim, no desktop, só as turmas ativas de aluno(a), e não vê o bloco da gestão", async () => {
    compacta.value = false;
    const { wrapper, router } = await montar("Professor", [
      turma(8, "Adultos", "Professor"),
      turma(7, "Jovens", "Aluno"),
      turma(9, "Turma antiga", "Aluno", false),
    ]);

    const bloco = wrapper.get('[data-testid="painel-turmas-como-aluno"]');
    const cartoesDoPainel = wrapper.findAll(".card-tabela");
    expect(cartoesDoPainel[cartoesDoPainel.length - 1]!.element).toBe(bloco.element);
    expect(wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"]').exists()).toBe(false);

    const linhas = bloco.findAll("tbody tr");
    expect(linhas).toHaveLength(1);
    expect(linhas[0]!.text()).toContain("Jovens");
    expect(bloco.text()).not.toContain("Adultos");
    expect(bloco.text()).not.toContain("Turma antiga");

    await bloco.get('[data-testid="painel-minha-frequencia"]').trigger("click");
    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/departamentos/7/minha-frequencia");
    wrapper.unmount();
  });

  it("sem matrícula em turma ativa, o bloco não aparece (sem turmas, só atribuição ou só turma inativa)", async () => {
    const admin = await montar("Admin", []);
    expect(admin.wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    expect(admin.wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"]').exists()).toBe(false);
    admin.wrapper.unmount();

    const professor = await montar("Professor", [turma(8, "Adultos", "Professor")]);
    expect(professor.wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    professor.wrapper.unmount();

    for (const perfil of ["Admin", "Professor"]) {
      const soInativa = await montar(perfil, [turma(9, "Turma antiga", "Aluno", false)]);
      expect(soInativa.wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
      soInativa.wrapper.unmount();
    }
  });

  it("Admin com atribuição vê, antes das tabelas, só as aulas Em aberto das turmas dele, com Fazer chamada", async () => {
    compacta.value = false;
    const { wrapper, router } = await montar("Admin", [turma(7, "Jovens", "Professor")]);

    const bloco = wrapper.get('[data-testid="painel-gestao-minhas-aulas-abertas"]');
    expect(bloco.text()).toContain("Aulas em aberto das minhas turmas");
    expect(vemDepois(bloco.element, wrapper.get('[data-testid="painel-gestao-tabelas"]').element)).toBe(true);

    // Só a aula 21: a 22 é Consolidada e a 31 é de uma turma sem atribuição da conta.
    const linhas = bloco.findAll("tbody tr");
    expect(linhas).toHaveLength(1);
    expect(linhas[0]!.text()).toContain("Jovens");
    expect(linhas[0]!.text()).toContain("Matéria 7");

    await bloco.get('[data-testid="painel-gestao-fazer-chamada"]').trigger("click");
    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/aulas/21/chamada");
    expect(wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("no celular, Aulas em aberto das minhas turmas vem em cartões, com Fazer chamada", async () => {
    const { wrapper, router } = await montar("Admin", [turma(7, "Jovens", "Professor")]);

    const cartoes = wrapper.findAll('[data-testid="painel-gestao-minha-aula-aberta"]');
    expect(cartoes).toHaveLength(1);
    expect(cartoes[0]!.text()).toContain("Jovens · Matéria 7");
    expect(wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"] table').exists()).toBe(false);

    await cartoes[0]!.get('[data-testid="painel-gestao-fazer-chamada"]').trigger("click");
    await flushPromises();
    expect(router.currentRoute.value.path).toBe("/aulas/21/chamada");
    wrapper.unmount();
  });

  it("o Painel do usuário não ganha os blocos novos (RF3: os painéis não se misturam)", async () => {
    const { wrapper } = await montar("Usuario", [turma(7, "Jovens", "Aluno")]);

    expect(wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"]').exists()).toBe(false);
    expect(wrapper.get('[data-testid="painel-aluno-turmas-cartoes"]').text()).toContain("Jovens");
    wrapper.unmount();
  });

  it("depois de um Recarregar, a resposta antiga das turmas da conta é descartada", async () => {
    const { wrapper } = await montar("Admin", []);
    const recarregar = () =>
      wrapper.findAll("button").find((b) => b.text().includes("Recarregar"))!.trigger("click");

    // Primeiro Recarregar: a resposta das turmas da conta demora.
    let responderAntiga!: (turmas: MinhaTurmaVM[]) => void;
    vi.mocked(listarMinhasTurmas).mockImplementationOnce(
      () => new Promise((resolve) => (responderAntiga = resolve)),
    );
    await recarregar();
    await flushPromises();

    // Segundo Recarregar: responde logo, sem turmas. Depois chega a resposta antiga.
    await recarregar();
    await flushPromises();
    responderAntiga([turma(7, "Jovens", "Aluno")]);
    await flushPromises();

    expect(wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    wrapper.unmount();
  });

  it("Admin sem atribuição não vê Aulas em aberto das minhas turmas, mesmo com aula em aberto na turma em que é aluno(a)", async () => {
    const { wrapper } = await montar("Admin", [turma(7, "Jovens", "Aluno")]);

    expect(wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(true);
    wrapper.unmount();
  });

  it("se a carga das turmas da conta falha, o resto do painel aparece, sem erro e sem os blocos", async () => {
    const { wrapper } = await montar("Admin", new Error("falha de rede"));

    expect(wrapper.text()).toContain("Turmas EBD ativas");
    expect(wrapper.find('[data-testid="painel-gestao-tabelas"]').text()).toContain("Jovens");
    expect(wrapper.text()).not.toContain("Não foi possível carregar o painel.");
    expect(wrapper.find('[data-testid="painel-turmas-como-aluno"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="painel-gestao-minhas-aulas-abertas"]').exists()).toBe(false);
    wrapper.unmount();
  });
});
