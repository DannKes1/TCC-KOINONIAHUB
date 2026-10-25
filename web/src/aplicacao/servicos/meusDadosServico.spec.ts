import { obterMinhaFrequencia } from "./meusDadosServico";
import { clienteHttp } from "./clienteHttp";

// CSU07 (RF6): a frequência do próprio aluno aceita a resposta da API em
// PascalCase ou camelCase; sem a lista de aulas, devolve [] e mantém os totais.
function responder(data: unknown) {
  return vi.spyOn(clienteHttp, "get").mockResolvedValue({ data } as any);
}

describe("obterMinhaFrequencia", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("lê a resposta em PascalCase (Aulas) e consulta a turma pedida", async () => {
    const get = responder({
      DepartamentoId: 7,
      NomeDepartamento: "Jovens",
      DataInicio: "2026-10-01",
      DataFim: "2026-10-31",
      TotalAulas: 2,
      Presentes: 1,
      AusentesMarcados: 1,
      NaoRegistrado: 0,
      PercentualPresenca: 50,
      Aulas: [
        { AulaId: 10, Data: "2026-10-04", Tema: "Samuel", Situacao: "Presente" },
        { AulaId: 11, Data: "2026-10-11", Tema: null, Situacao: "Ausente" },
      ],
      AulasPendentes: [
        { Id: 12, Data: "2026-10-18", Materia: "Samuel", Professor: "Paula", DepartamentoId: 7, Departamento: "Jovens" },
      ],
    });

    const r = await obterMinhaFrequencia(7);

    expect(get).toHaveBeenCalledWith("/api/relatorios/ebd/minha-frequencia", {
      params: { departamentoId: 7 },
    });
    expect(r).toMatchObject({
      departamentoId: 7,
      nomeDepartamento: "Jovens",
      dataInicio: "2026-10-01",
      dataFim: "2026-10-31",
      totalAulas: 2,
      presentes: 1,
      ausentesMarcados: 1,
      naoRegistrado: 0,
      percentualPresenca: 50,
    });
    expect(r.aulas).toEqual([
      { aulaId: 10, data: "2026-10-04", tema: "Samuel", situacao: "Presente" },
      { aulaId: 11, data: "2026-10-11", tema: null, situacao: "Ausente" },
    ]);
    expect(r.aulasPendentes.map((a) => a.id)).toEqual([12]);
  });

  it("lê a resposta em camelCase (aulas)", async () => {
    responder({
      departamentoId: 3,
      nomeDepartamento: "Adultos",
      dataInicio: "2026-09-01",
      dataFim: "2026-09-30",
      totalAulas: 1,
      presentes: 0,
      ausentesMarcados: 0,
      naoRegistrado: 1,
      percentualPresenca: 0,
      aulas: [{ aulaId: 20, data: "2026-09-07", tema: "Atos", situacao: "NaoRegistrado" }],
      aulasPendentes: [],
    });

    const r = await obterMinhaFrequencia(3);

    expect(r).toMatchObject({
      departamentoId: 3,
      nomeDepartamento: "Adultos",
      totalAulas: 1,
      presentes: 0,
      ausentesMarcados: 0,
      naoRegistrado: 1,
      percentualPresenca: 0,
    });
    expect(r.aulas).toEqual([
      { aulaId: 20, data: "2026-09-07", tema: "Atos", situacao: "NaoRegistrado" },
    ]);
    expect(r.aulasPendentes).toEqual([]);
  });

  it("sem a lista de aulas, devolve [] e mantém os totais", async () => {
    responder({
      DepartamentoId: 5,
      NomeDepartamento: "Crianças",
      TotalAulas: 4,
      Presentes: 3,
      AusentesMarcados: 1,
      NaoRegistrado: 0,
      PercentualPresenca: 75,
    });

    const r = await obterMinhaFrequencia(5);

    expect(r.aulas).toEqual([]);
    expect(r.aulasPendentes).toEqual([]);
    expect(r).toMatchObject({
      departamentoId: 5,
      nomeDepartamento: "Crianças",
      dataInicio: "",
      dataFim: "",
      totalAulas: 4,
      presentes: 3,
      ausentesMarcados: 1,
      naoRegistrado: 0,
      percentualPresenca: 75,
    });
  });
});
