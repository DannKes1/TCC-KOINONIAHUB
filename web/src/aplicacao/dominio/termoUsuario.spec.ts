import { DICA_TERMO_PENDENTE, TERMO_PENDENTE, resumirTermoUsuario } from "./termoUsuario";

// RF43 / RNF 43.1: a lista de Usuários mostra a versão e a data e hora do aceite de
// cada conta (não só em tooltip, que não existe no celular).
describe("resumirTermoUsuario (RF43)", () => {
  const aceite = { versao: "1.0", aceitoEm: "2026-10-18T17:05:00Z", meio: "Login", vigente: true };
  const quandoEsperado = new Date(aceite.aceitoEm).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });

  it("sem aceite → 'Pendente', sem data", () => {
    const resumo = resumirTermoUsuario(null);
    expect(resumo.rotulo).toBe(TERMO_PENDENTE);
    expect(resumo.severidade).toBe("warn");
    expect(resumo.quando).toBe("");
    expect(resumo.dica).toBe(DICA_TERMO_PENDENTE);
  });

  it("aceite da versão vigente → 'Aceito vX' com a data e hora", () => {
    const resumo = resumirTermoUsuario(aceite);
    expect(resumo.rotulo).toBe("Aceito v1.0");
    expect(resumo.severidade).toBe("success");
    expect(resumo.quando).toBe(quandoEsperado);
    expect(resumo.quando).toMatch(/^\d{2}\/\d{2}\/\d{4},? \d{2}:\d{2}$/);
    expect(resumo.dica).toBe(`Aceito em ${quandoEsperado}.`);
  });

  it("aceite de versão anterior → 'Versão anterior (vX)' em alerta, com a data", () => {
    const resumo = resumirTermoUsuario({ ...aceite, vigente: false });
    expect(resumo.rotulo).toBe("Versão anterior (v1.0)");
    expect(resumo.severidade).toBe("warn");
    expect(resumo.quando).toBe(quandoEsperado);
    expect(resumo.dica).toContain("a versão vigente ainda está pendente");
  });

  it("data inválida → sem 'quando', mas o rótulo continua", () => {
    const resumo = resumirTermoUsuario({ ...aceite, aceitoEm: "" });
    expect(resumo.rotulo).toBe("Aceito v1.0");
    expect(resumo.quando).toBe("");
  });
});
