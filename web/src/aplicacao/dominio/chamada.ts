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

/** True quando a linha difere do seu último estado salvo (presença ou observação). */
export function linhaAlterada(atual: LinhaChamada, antes: LinhaChamada | undefined): boolean {
  if (!antes) return true;
  return (
    Boolean(antes.presente) !== Boolean(atual.presente) ||
    normalizarObservacao(antes.observacao) !== normalizarObservacao(atual.observacao)
  );
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
  return atual.some((l) => linhaAlterada(l, porId.get(l.alunoDepartamentoId)));
}

/**
 * Alunos sem registro explícito que o professor ainda não tocou (Etapa 6.4-C,
 * RNFs 32.6/33.3, CSU11 passo 2): ao salvar ou consolidar, serão gravados como
 * ausentes — é quem a confirmação do Consolidar nomeia. `idsSemRegistro` vem da
 * carga (`registrado === false`) ou do 400 da consolidação (alunosSemRegistro[]).
 * Uma linha dessas que o professor já marcou (presente ou observação) deixa de
 * ser "sem registro": vai ser gravada com o que ele escolheu.
 */
export function alunosSemRegistro(
  atual: LinhaChamada[],
  salva: LinhaChamada[],
  idsSemRegistro: Set<number>,
): LinhaChamada[] {
  if (idsSemRegistro.size === 0) return [];
  const porId = new Map(salva.map((l) => [l.alunoDepartamentoId, l]));
  return atual.filter(
    (l) =>
      idsSemRegistro.has(l.alunoDepartamentoId) &&
      !linhaAlterada(l, porId.get(l.alunoDepartamentoId)),
  );
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
