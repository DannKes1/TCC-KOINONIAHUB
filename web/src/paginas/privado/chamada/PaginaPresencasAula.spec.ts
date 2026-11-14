import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaPresencasAula from "./PaginaPresencasAula.vue";
import { obterAula } from "../../../aplicacao/servicos/aulasServico";
import { listarPresencasRegistradas } from "../../../aplicacao/servicos/chamadasServico";
import type { AulaVM, PresencaVM } from "../../../aplicacao/modelos/dtos";

// Página "Presenças de <data>" (CSU09, Fluxo Alternativo 4; RF32). Ajuste A3: além de
// Registros, Presentes e Ausentes, a página mostra o total de visitantes que a chamada
// gravou na aula. O total vem de GET /api/aulas/{id} (quantidadeVisitantes) e não entra
// em Registros, que conta só alunos matriculados. Serviços da API simulados.
vi.mock("../../../aplicacao/servicos/aulasServico", () => ({
  obterAula: vi.fn(),
}));

vi.mock("../../../aplicacao/servicos/chamadasServico", () => ({
  listarPresencasRegistradas: vi.fn(),
}));

function aula(quantidadeVisitantes: number): AulaVM {
  return {
    id: 5,
    data: "2026-11-08T12:00:00Z",
    tema: "Tema",
    conteudo: null,
    situacao: "Consolidada",
    pendenteFechamento: false,
    possuiPresencas: true,
    quantidadeVisitantes,
    materiaId: 1,
    nomeMateria: "Samuel",
    departamentoId: 7,
    nomeDepartamento: "Jovens",
    professorId: 3,
    nomeProfessor: "Professor",
    criadoEm: "2026-11-01T12:00:00Z",
  };
}

function presenca(id: number, nomeAluno: string, presente: boolean): PresencaVM {
  return {
    id,
    aulaId: 5,
    alunoDepartamentoId: id,
    pessoaId: id * 10,
    nomeAluno,
    presente,
    observacao: null,
    criadoEm: "2026-11-08T13:00:00Z",
  };
}

async function montar(visitantes: number) {
  vi.mocked(obterAula).mockResolvedValue(aula(visitantes));
  vi.mocked(listarPresencasRegistradas).mockResolvedValue([
    presenca(1, "Ana", true),
    presenca(2, "Bia", false),
  ]);

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/aulas/:aulaId/presencas", component: PaginaPresencasAula },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push("/aulas/5/presencas");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    global: { plugins: [router, PrimeVue] },
  });
  await flushPromises();
  return wrapper;
}

// Rótulo e valor de cada indicador, na ordem da tela.
function indicadores(wrapper: Awaited<ReturnType<typeof montar>>) {
  return wrapper
    .find('[data-testid="presencas-indicadores"]')
    .findAll(".card-indicador")
    .map((card) => [
      card.find(".card-indicador-rotulo").text(),
      card.find(".card-indicador-valor").text(),
    ]);
}

describe("PaginaPresencasAula (CSU09 FA4, ajuste A3: visitantes)", () => {
  beforeEach(() => {
    vi.mocked(obterAula).mockReset();
    vi.mocked(listarPresencasRegistradas).mockReset();
  });

  it("mostra Registros, Presentes, Ausentes e o total de visitantes gravado na aula", async () => {
    const wrapper = await montar(3);

    expect(indicadores(wrapper)).toEqual([
      ["Registros", "2"],
      ["Presentes", "1"],
      ["Ausentes", "1"],
      ["Visitantes", "3"],
    ]);
    expect(obterAula).toHaveBeenCalledWith(5);

    wrapper.unmount();
  });

  it("aula sem visitantes: o indicador mostra 0 e os outros não mudam", async () => {
    const wrapper = await montar(0);

    expect(indicadores(wrapper)).toEqual([
      ["Registros", "2"],
      ["Presentes", "1"],
      ["Ausentes", "1"],
      ["Visitantes", "0"],
    ]);

    wrapper.unmount();
  });
});
