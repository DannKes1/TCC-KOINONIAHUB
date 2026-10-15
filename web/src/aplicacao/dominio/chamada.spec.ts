import {
  alunosSemRegistro,
  chamadaAlterada,
  montarLoteChamada,
  normalizarObservacao,
  resumirChamada,
  type LinhaChamada,
} from "./chamada";

// RF32 / CSU10 e decisão 3 da 6.4 (Consolidar salva as alterações pendentes antes):
// regras puras da tela de chamada.
function linha(id: number, presente: boolean, observacao: string | null = null, registrado = true): LinhaChamada {
  return { alunoDepartamentoId: id, pessoaId: id * 10, nomeAluno: `Aluno ${id}`, presente, observacao, registrado };
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

// Etapa 6.4-C (a): quem está sem registro e o professor ainda não tocou é quem será
// gravado como ausente — é a lista que a confirmação do Consolidar nomeia.
describe("alunosSemRegistro", () => {
  const salva = [linha(1, true), linha(2, false, null, false), linha(3, false, null, false)];
  const ids = new Set([2, 3]);

  it("lista os sem registro não tocados, na ordem da chamada", () => {
    const atual = salva.map((l) => ({ ...l }));
    expect(alunosSemRegistro(atual, salva, ids).map((l) => l.alunoDepartamentoId)).toEqual([2, 3]);
  });

  it("quem foi marcado presente ou recebeu observação sai da lista", () => {
    const atual = [linha(1, true), linha(2, true, null, false), linha(3, false, "avisou", false)];
    expect(alunosSemRegistro(atual, salva, ids)).toEqual([]);
  });

  it("sem ids, lista vazia mesmo com linhas", () => {
    expect(alunosSemRegistro(salva, salva, new Set())).toEqual([]);
  });
});
