import {
  chamadaAlterada,
  montarLoteChamada,
  normalizarObservacao,
  resumirChamada,
  type LinhaChamada,
} from "./chamada";

// RF32 / CSU10 e decisão 3 da 6.4 (Consolidar salva as alterações pendentes antes):
// regras puras da tela de chamada.
function linha(id: number, presente: boolean, observacao: string | null = null): LinhaChamada {
  return { alunoDepartamentoId: id, pessoaId: id * 10, nomeAluno: `Aluno ${id}`, presente, observacao };
}

describe("resumirChamada", () => {
  it("conta presentes, ausentes e total", () => {
    expect(resumirChamada([linha(1, true), linha(2, false), linha(3, true)])).toEqual({
      total: 3,
      presentes: 2,
      ausentes: 1,
    });
    expect(resumirChamada([])).toEqual({ total: 0, presentes: 0, ausentes: 0 });
  });
});

describe("chamadaAlterada", () => {
  const salva = [linha(1, true), linha(2, false, "atraso")];

  it("sem mudanças (inclusive observação só com espaços), não há alteração", () => {
    const atual = [linha(1, true), linha(2, false, "  atraso ")];
    expect(chamadaAlterada(atual, salva, 0, 0)).toBe(false);
  });

  it("detecta presença, observação e visitantes alterados", () => {
    expect(chamadaAlterada([linha(1, false), linha(2, false, "atraso")], salva, 0, 0)).toBe(true);
    expect(chamadaAlterada([linha(1, true), linha(2, false, "dispensa")], salva, 0, 0)).toBe(true);
    expect(chamadaAlterada([linha(1, true), linha(2, false, null)], salva, 0, 0)).toBe(true);
    expect(chamadaAlterada([linha(1, true), linha(2, false, "atraso")], salva, 2, 0)).toBe(true);
  });

  it("lista com aluno a mais ou a menos conta como alteração", () => {
    expect(chamadaAlterada([linha(1, true)], salva, 0, 0)).toBe(true);
    expect(chamadaAlterada([...salva, linha(3, false)], salva, 0, 0)).toBe(true);
  });
});

describe("montarLoteChamada", () => {
  it("monta o lote da API com observações normalizadas", () => {
    expect(montarLoteChamada([linha(1, true, "  "), linha(2, false, " saída antecipada ")], 3)).toEqual({
      QuantidadeVisitantes: 3,
      Itens: [
        { AlunoDepartamentoId: 1, Presente: true, Observacao: null },
        { AlunoDepartamentoId: 2, Presente: false, Observacao: "saída antecipada" },
      ],
    });
  });

  it("normalizarObservacao devolve null para vazio", () => {
    expect(normalizarObservacao(undefined)).toBeNull();
    expect(normalizarObservacao("")).toBeNull();
    expect(normalizarObservacao(" x ")).toBe("x");
  });
});
