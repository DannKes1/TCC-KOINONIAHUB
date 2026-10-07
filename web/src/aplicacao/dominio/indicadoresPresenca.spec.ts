import {
  JANELA_PADRAO_DIAS,
  filtrarJanela,
  formatarPercentual,
  inicioDaJanela,
  resumirPresencas,
} from "./indicadoresPresenca";
import type { HistoricoPresencaPessoaVM } from "../modelos/dtos";

// Etapa 6 (Plano 5, "Acabamento de UI"): indicadores do painel do aluno (RF3) e do
// diálogo de histórico de Pessoas seguem o CSU07 — só aulas Consolidadas contam —
// e a janela do painel é a mesma de Minha Frequência (90 dias).
function registro(
  dataAula: string,
  presente: boolean,
  situacaoAula: HistoricoPresencaPessoaVM["situacaoAula"] = "Consolidada",
): HistoricoPresencaPessoaVM {
  return {
    aulaId: Math.floor(Math.random() * 1_000_000),
    dataAula,
    departamentoId: 1,
    departamentoNome: "Turma",
    materiaId: 1,
    materiaNome: "Matéria",
    presente,
    observacao: null,
    situacaoAula,
  };
}

describe("resumirPresencas", () => {
  it("conta só registros de aulas Consolidadas", () => {
    const r = resumirPresencas([
      registro("2026-09-06", true),
      registro("2026-09-13", false),
      registro("2026-09-20", true),
      registro("2026-09-27", true, "EmAberto"), // chamada lançada, aula ainda aberta
      registro("2026-10-04", false, "NaoRealizada"),
    ]);

    expect(r).toEqual({
      aulasConsolidadas: 3,
      presencas: 2,
      faltas: 1,
      percentual: 67,
    });
  });

  it("sem aula consolidada o percentual é null (não 0%)", () => {
    const r = resumirPresencas([registro("2026-09-27", true, "EmAberto")]);

    expect(r.aulasConsolidadas).toBe(0);
    expect(r.percentual).toBeNull();
    expect(formatarPercentual(r.percentual)).toBe("-");
    expect(formatarPercentual(67)).toBe("67%");
  });
});

describe("filtrarJanela", () => {
  const hoje = new Date(2026, 9, 6, 12, 0, 0); // 06/10/2026 meio-dia (hora local)

  it("a janela padrão tem 90 dias e começa 90 dias antes de hoje", () => {
    expect(JANELA_PADRAO_DIAS).toBe(90);
    const inicio = inicioDaJanela(JANELA_PADRAO_DIAS, hoje);
    expect(inicio.getFullYear()).toBe(2026);
    expect(inicio.getMonth()).toBe(6); // julho
    expect(inicio.getDate()).toBe(8); // 06/10 - 90 dias = 08/07
  });

  it("mantém só registros dentro da janela; datas inválidas ficam fora", () => {
    const dentro = registro("2026-09-13T00:00:00", true);
    const noLimite = registro("2026-07-08T12:00:00", true);
    const fora = registro("2026-07-07T12:00:00", true);
    const futuro = registro("2026-10-07T12:00:00", true);
    const invalido = registro("", true);

    const resultado = filtrarJanela(
      [dentro, noLimite, fora, futuro, invalido],
      JANELA_PADRAO_DIAS,
      hoje,
    );

    expect(resultado.map((r) => r.dataAula)).toEqual([
      dentro.dataAula,
      noLimite.dataAula,
    ]);
  });

  it("janela e resumo combinados: um registro antigo não entra no percentual", () => {
    const registros = [
      registro("2026-09-13T00:00:00", true),
      registro("2026-09-20T00:00:00", false),
      registro("2026-05-10T00:00:00", false), // fora dos 90 dias
    ];

    expect(resumirPresencas(registros).percentual).toBe(33);
    expect(resumirPresencas(filtrarJanela(registros, 90, hoje)).percentual).toBe(50);
  });
});
