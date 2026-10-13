import type {
  ChamadaRegistrarDTO,
  ItemChamadaCompletaVM,
} from "../modelos/dtos";

// Regras puras da tela de chamada (RF32 / CSU10), separadas da página para o
// Vitest cobrir: contador de presentes, detecção de alteração não salva (decisão 3
// da 6.4: Consolidar salva as pendentes antes) e montagem do lote enviado à API.

export type LinhaChamada = ItemChamadaCompletaVM;

export function normalizarObservacao(
  valor: string | null | undefined,
): string | null {
  const texto = (valor ?? "").trim();
  return texto === "" ? null : texto;
}

export function resumirChamada(linhas: LinhaChamada[]) {
  const total = linhas.length;
  const presentes = linhas.filter((l) => l.presente).length;
  return { total, presentes, ausentes: total - presentes };
}

/** True quando algo na tela difere do que a API devolveu por último. */
export function chamadaAlterada(
  atual: LinhaChamada[],
  salva: LinhaChamada[],
  visitantesAtual: number,
  visitantesSalvo: number,
): boolean {
  if (Number(visitantesAtual) !== Number(visitantesSalvo)) return true;
  if (atual.length !== salva.length) return true;

  const porId = new Map(salva.map((l) => [l.alunoDepartamentoId, l]));
  return atual.some((l) => {
    const antes = porId.get(l.alunoDepartamentoId);
    if (!antes) return true;
    return (
      Boolean(antes.presente) !== Boolean(l.presente) ||
      normalizarObservacao(antes.observacao) !== normalizarObservacao(l.observacao)
    );
  });
}

export function montarLoteChamada(
  linhas: LinhaChamada[],
  visitantes: number,
): ChamadaRegistrarDTO {
  return {
    QuantidadeVisitantes: Number(visitantes ?? 0),
    Itens: linhas.map((l) => ({
      AlunoDepartamentoId: l.alunoDepartamentoId,
      Presente: Boolean(l.presente),
      Observacao: normalizarObservacao(l.observacao),
    })),
  };
}

export function copiarLinhas(linhas: LinhaChamada[]): LinhaChamada[] {
  return linhas.map((l) => ({ ...l }));
}
