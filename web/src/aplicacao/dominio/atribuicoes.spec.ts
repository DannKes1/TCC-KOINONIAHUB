import {
  AVISO_PERFIL_USUARIO,
  AVISO_PERFIL_USUARIO_CURTO,
  AVISO_SEM_CONTA,
  avisoConta,
} from "./atribuicoes";

// Etapa 7.1 (RNF 21.5): texto da tag ao lado da pessoa na lista de atribuições.
describe("avisoConta (RNF 21.5)", () => {
  it("sem conta de acesso → 'Sem conta'", () => {
    expect(avisoConta({ perfilConta: null })).toBe(AVISO_SEM_CONTA);
    expect(avisoConta({ perfilConta: "" })).toBe(AVISO_SEM_CONTA);
  });

  it("conta de perfil Usuário → aviso de que não acessa a turma (sem diferenciar maiúsculas)", () => {
    expect(avisoConta({ perfilConta: "Usuario" })).toBe(AVISO_PERFIL_USUARIO);
    expect(avisoConta({ perfilConta: " usuario " })).toBe(AVISO_PERFIL_USUARIO);
  });

  it("na tela compacta o aviso do perfil Usuário é o texto curto; 'Sem conta' não muda", () => {
    expect(avisoConta({ perfilConta: "Usuario" }, true)).toBe(AVISO_PERFIL_USUARIO_CURTO);
    expect(avisoConta({ perfilConta: null }, true)).toBe(AVISO_SEM_CONTA);
  });

  it("conta Professor ou administrativa → sem aviso", () => {
    for (const perfil of ["Professor", "Admin", "Pastor", "Superintendente"]) {
      expect(avisoConta({ perfilConta: perfil })).toBeNull();
    }
  });
});
