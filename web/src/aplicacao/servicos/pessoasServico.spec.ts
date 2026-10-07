import { normalizarResultadoImportacao } from "./pessoasServico";

// Plano 7.2, "Parser do resultado de importação" (RF41 / RNF 41.3): totais de
// criadas/ignoradas/erro e marcação das linhas sinalizadas para conferência.
const respostaDaApi = {
  totalLinhas: 4,
  criados: 1,
  ignorados: 2,
  erros: 1,
  paraConferencia: 1,
  itens: [
    { linha: 2, nome: "Maria da Silva", email: "maria@teste.com", status: "Criado", mensagem: null, paraConferencia: false },
    { linha: 3, nome: "Ana Outra Grafia", email: "ana@teste.com", status: "Ignorado", mensagem: "Já existe uma pessoa com este e-mail (linha ignorada).", paraConferencia: false },
    { linha: 4, nome: "Carlos Souza", email: null, status: "Ignorado", mensagem: "Já existe uma pessoa com este nome; linha ignorada para conferência. Se for outra pessoa, cadastre manualmente.", paraConferencia: true },
    { linha: 5, nome: "", email: null, status: "Erro", mensagem: "Nome é obrigatório.", paraConferencia: false },
  ],
};

describe("normalizarResultadoImportacao", () => {
  it("lê os totais e os itens da resposta (camelCase)", () => {
    const r = normalizarResultadoImportacao(respostaDaApi);

    expect(r.totalLinhas).toBe(4);
    expect(r.criados).toBe(1);
    expect(r.ignorados).toBe(2);
    expect(r.erros).toBe(1);
    expect(r.paraConferencia).toBe(1);
    expect(r.itens).toHaveLength(4);
    expect(r.itens[0]).toMatchObject({ linha: 2, status: "Criado", paraConferencia: false });
  });

  it("marca só a linha de nome repetido sem e-mail como 'para conferência' (41.3)", () => {
    const r = normalizarResultadoImportacao(respostaDaApi);

    const sinalizadas = r.itens.filter((i) => i.paraConferencia);
    expect(sinalizadas.map((i) => i.linha)).toEqual([4]);
    expect(sinalizadas[0]!.status).toBe("Ignorado");
    expect(sinalizadas[0]!.mensagem).toContain("conferência");

    // E-mail repetido é ignorado, mas não é caso de conferência.
    expect(r.itens.find((i) => i.linha === 3)!.paraConferencia).toBe(false);
  });

  it("aceita PascalCase e reconta os totais quando a API não os manda", () => {
    const r = normalizarResultadoImportacao({
      Itens: [
        { Linha: 2, Nome: "A", Status: "Criado" },
        { Linha: 3, Nome: "B", Status: "Ignorado", ParaConferencia: true },
        { Linha: 4, Nome: "C", Status: "Ignorado" },
        { Linha: 5, Nome: "", Status: "Erro" },
      ],
    });

    expect(r.totalLinhas).toBe(4);
    expect(r.criados).toBe(1);
    expect(r.ignorados).toBe(2);
    expect(r.erros).toBe(1);
    expect(r.paraConferencia).toBe(1);
    expect(r.itens[1]!.paraConferencia).toBe(true);
    expect(r.itens[2]!.paraConferencia).toBe(false);
  });

  it("resposta vazia ou inválida vira resultado zerado", () => {
    expect(normalizarResultadoImportacao(null)).toEqual({
      totalLinhas: 0,
      criados: 0,
      ignorados: 0,
      erros: 0,
      paraConferencia: 0,
      itens: [],
    });
  });
});
