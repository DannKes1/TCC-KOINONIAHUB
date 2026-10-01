import type { SituacaoAula } from "../modelos/dtos";

// Regras de exibição da situação da aula (RF31 / RNF 31.3; Plano 6.2).
// A API é a fonte da verdade para `pendenteFechamento`; a função abaixo aplica
// a mesma regra (Em aberto e data já ocorrida, comparando datas em UTC) para os
// casos em que o valor não vier na resposta e para o filtro da tela.

export const SITUACOES_AULA: SituacaoAula[] = [
  "EmAberto",
  "Consolidada",
  "NaoRealizada",
];

export type SeveridadeTag =
  | "success"
  | "info"
  | "warning"
  | "danger"
  | "secondary"
  | "contrast";

export function normalizarSituacaoAula(valor: unknown): SituacaoAula {
  const texto = String(valor ?? "");
  return (SITUACOES_AULA as string[]).includes(texto)
    ? (texto as SituacaoAula)
    : "EmAberto";
}

function diaUtc(data: Date): number {
  return Date.UTC(data.getUTCFullYear(), data.getUTCMonth(), data.getUTCDate());
}

// Em aberto com data anterior a hoje (uma aula do próprio dia não é pendente).
export function ehPendenteFechamento(
  situacao: SituacaoAula,
  dataIso: string,
  hoje: Date = new Date(),
): boolean {
  if (situacao !== "EmAberto") return false;

  const data = new Date(dataIso);
  if (Number.isNaN(data.getTime())) return false;

  return diaUtc(data) < diaUtc(hoje);
}

export function rotuloSituacaoAula(situacao: SituacaoAula): string {
  switch (situacao) {
    case "Consolidada":
      return "Consolidada";
    case "NaoRealizada":
      return "Não realizada";
    default:
      return "Em aberto";
  }
}

export function severidadeSituacaoAula(situacao: SituacaoAula): SeveridadeTag {
  switch (situacao) {
    case "Consolidada":
      return "success";
    case "NaoRealizada":
      return "secondary";
    default:
      return "info";
  }
}

// Filtro da listagem de aulas (RNF 31.2/31.3): as três situações e o recorte
// "Pendentes" (Em aberto com data já ocorrida).
export type FiltroSituacaoAula = SituacaoAula | "Pendentes";

export const OPCOES_FILTRO_SITUACAO: {
  label: string;
  value: FiltroSituacaoAula;
}[] = [
  { label: "Em aberto", value: "EmAberto" },
  { label: "Pendentes de fechamento", value: "Pendentes" },
  { label: "Consolidadas", value: "Consolidada" },
  { label: "Não realizadas", value: "NaoRealizada" },
];

export function aulaAtendeFiltro(
  aula: { situacao: SituacaoAula; pendenteFechamento: boolean },
  filtro: FiltroSituacaoAula | null,
): boolean {
  if (!filtro) return true;
  if (filtro === "Pendentes") return aula.pendenteFechamento;
  return aula.situacao === filtro;
}
