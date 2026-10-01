import {
  aulaAtendeFiltro,
  ehPendenteFechamento,
  normalizarSituacaoAula,
  rotuloSituacaoAula,
  severidadeSituacaoAula,
} from "./situacaoAula";

// Plano 7.2, primeira linha: Em aberto com data passada → pendente; Em aberto
// hoje → não; Consolidada → não; Não realizada → não.
describe("ehPendenteFechamento (RNF 31.3)", () => {
  const hoje = new Date("2026-09-27T15:00:00Z");

  it("Em aberto com data passada é pendente", () => {
    expect(ehPendenteFechamento("EmAberto", "2026-09-20T12:00:00Z", hoje)).toBe(
      true,
    );
  });

  it("Em aberto no próprio dia não é pendente", () => {
    expect(ehPendenteFechamento("EmAberto", "2026-09-27T09:00:00Z", hoje)).toBe(
      false,
    );
  });

  it("Em aberto com data futura não é pendente", () => {
    expect(ehPendenteFechamento("EmAberto", "2026-10-04T12:00:00Z", hoje)).toBe(
      false,
    );
  });

  it("Consolidada nunca é pendente, mesmo com data passada", () => {
    expect(
      ehPendenteFechamento("Consolidada", "2026-09-20T12:00:00Z", hoje),
    ).toBe(false);
  });

  it("Não realizada nunca é pendente, mesmo com data passada", () => {
    expect(
      ehPendenteFechamento("NaoRealizada", "2026-09-20T12:00:00Z", hoje),
    ).toBe(false);
  });

  it("data inválida não é pendente", () => {
    expect(ehPendenteFechamento("EmAberto", "", hoje)).toBe(false);
  });
});

describe("rótulo e cor da situação (RF31)", () => {
  it("usa os três rótulos da monografia", () => {
    expect(rotuloSituacaoAula("EmAberto")).toBe("Em aberto");
    expect(rotuloSituacaoAula("Consolidada")).toBe("Consolidada");
    expect(rotuloSituacaoAula("NaoRealizada")).toBe("Não realizada");
  });

  it("atribui uma severidade distinta a cada situação", () => {
    const severidades = new Set([
      severidadeSituacaoAula("EmAberto"),
      severidadeSituacaoAula("Consolidada"),
      severidadeSituacaoAula("NaoRealizada"),
    ]);
    expect(severidades.size).toBe(3);
  });

  it("normaliza valores desconhecidos para Em aberto", () => {
    expect(normalizarSituacaoAula("Consolidada")).toBe("Consolidada");
    expect(normalizarSituacaoAula("qualquer")).toBe("EmAberto");
    expect(normalizarSituacaoAula(undefined)).toBe("EmAberto");
  });
});

describe("filtro por situação (RNF 31.2)", () => {
  const emAbertoPendente = {
    situacao: "EmAberto" as const,
    pendenteFechamento: true,
  };
  const emAbertoFutura = {
    situacao: "EmAberto" as const,
    pendenteFechamento: false,
  };
  const consolidada = {
    situacao: "Consolidada" as const,
    pendenteFechamento: false,
  };

  it("sem filtro aceita todas", () => {
    expect(aulaAtendeFiltro(consolidada, null)).toBe(true);
  });

  it("'Pendentes' só aceita Em aberto com data já ocorrida", () => {
    expect(aulaAtendeFiltro(emAbertoPendente, "Pendentes")).toBe(true);
    expect(aulaAtendeFiltro(emAbertoFutura, "Pendentes")).toBe(false);
    expect(aulaAtendeFiltro(consolidada, "Pendentes")).toBe(false);
  });

  it("filtro por situação compara a situação", () => {
    expect(aulaAtendeFiltro(emAbertoFutura, "EmAberto")).toBe(true);
    expect(aulaAtendeFiltro(consolidada, "EmAberto")).toBe(false);
    expect(aulaAtendeFiltro(consolidada, "Consolidada")).toBe(true);
  });
});
