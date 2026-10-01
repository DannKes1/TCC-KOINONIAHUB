import { clienteHttp } from "./clienteHttp";
import type { AulaResumidaVM, FrequenciaTurmaVM } from "../modelos/dtos";

// Item de aulasPendentes[] / aulasNaoRealizadas[] (Plano 6.9).
export function normalizarAulaResumida(bruto: any): AulaResumidaVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    data: String(bruto?.Data ?? bruto?.data ?? ""),
    materia: String(bruto?.Materia ?? bruto?.materia ?? ""),
    professor: String(bruto?.Professor ?? bruto?.professor ?? ""),
    departamentoId: Number(bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0),
    departamento: String(bruto?.Departamento ?? bruto?.departamento ?? ""),
  };
}

export function normalizarListaAulasResumidas(
  lista: unknown,
): AulaResumidaVM[] {
  return Array.isArray(lista) ? lista.map(normalizarAulaResumida) : [];
}

function normalizarFrequenciaTurma(bruto: any): FrequenciaTurmaVM {
  return {
    departamentoId: bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0,
    nomeDepartamento: bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    dataInicio: String(bruto?.DataInicio ?? bruto?.dataInicio ?? ""),
    dataFim: String(bruto?.DataFim ?? bruto?.dataFim ?? ""),
    totalAulas: bruto?.TotalAulas ?? bruto?.totalAulas ?? 0,
    totalAlunos: bruto?.TotalAlunos ?? bruto?.totalAlunos ?? 0,
    totalPresentes: bruto?.TotalPresentes ?? bruto?.totalPresentes ?? 0,
    totalAusentesMarcados:
      bruto?.TotalAusentesMarcados ?? bruto?.totalAusentesMarcados ?? 0,
    totalNaoRegistrado:
      bruto?.TotalNaoRegistrado ?? bruto?.totalNaoRegistrado ?? 0,
    percentualPresencaGeral:
      bruto?.PercentualPresencaGeral ?? bruto?.percentualPresencaGeral ?? 0,
    alunos: bruto?.Alunos ?? bruto?.alunos ?? [],
    aulas: bruto?.Aulas ?? bruto?.aulas ?? [],
    aulasPendentes: normalizarListaAulasResumidas(
      bruto?.AulasPendentes ?? bruto?.aulasPendentes,
    ),
  };
}

export async function obterFrequenciaTurma(params: {
  departamentoId: number;
  dataInicio?: string;
  dataFim?: string;
}) {
  const resposta = await clienteHttp.get(
    `/api/relatorios/ebd/frequencia-turma`,
    { params },
  );
  return normalizarFrequenciaTurma(resposta.data);
}

function normalizarAcompanhamento(bruto: any) {
  return {
    departamentoId: bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0,
    nomeDepartamento: bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    dataInicio: String(bruto?.DataInicio ?? bruto?.dataInicio ?? ""),
    dataFim: String(bruto?.DataFim ?? bruto?.dataFim ?? ""),
    totalAulas: bruto?.TotalAulas ?? bruto?.totalAulas ?? 0,
    totalAlunos: bruto?.TotalAlunos ?? bruto?.totalAlunos ?? 0,
    limiarAtencao: bruto?.LimiarAtencao ?? bruto?.limiarAtencao ?? 0,
    limiarCritico: bruto?.LimiarCritico ?? bruto?.limiarCritico ?? 0,
    faltasConsecutivasCritico:
      bruto?.FaltasConsecutivasCritico ?? bruto?.faltasConsecutivasCritico ?? 0,
    totalCritico: bruto?.TotalCritico ?? bruto?.totalCritico ?? 0,
    totalAtencao: bruto?.TotalAtencao ?? bruto?.totalAtencao ?? 0,
    alunos: bruto?.Alunos ?? bruto?.alunos ?? [],
    aulasPendentes: normalizarListaAulasResumidas(
      bruto?.AulasPendentes ?? bruto?.aulasPendentes,
    ),
  };
}

export async function obterPainelAcompanhamento(params: {
  departamentoId: number;
  dataInicio?: string;
  dataFim?: string;
  limiarAtencao?: number;
  limiarCritico?: number;
  faltasConsecutivasCritico?: number;
}) {
  const resposta = await clienteHttp.get(`/api/relatorios/ebd/acompanhamento`, {
    params,
  });
  return normalizarAcompanhamento(resposta.data);
}

function normalizarRankingFaltas(bruto: any) {
  return {
    departamentoId: bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0,
    nomeDepartamento: bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    dataInicio: String(bruto?.DataInicio ?? bruto?.dataInicio ?? ""),
    dataFim: String(bruto?.DataFim ?? bruto?.dataFim ?? ""),
    itens: bruto?.Itens ?? bruto?.itens ?? [],
    aulasPendentes: normalizarListaAulasResumidas(
      bruto?.AulasPendentes ?? bruto?.aulasPendentes,
    ),
  };
}

export async function obterRankingFaltas(params: {
  departamentoId: number;
  dataInicio?: string;
  dataFim?: string;
  top?: number;
}) {
  const resposta = await clienteHttp.get(`/api/relatorios/ebd/ranking-faltas`, {
    params,
  });
  return normalizarRankingFaltas(resposta.data);
}

// RF38 / RNF 38.4: totais só de aulas Consolidadas; as demais situações aparecem
// separadamente (contagens por turma e listas).
export type ResumoDiaTurmaVM = {
  departamentoId: number;
  nome: string;
  temChamada: boolean;
  presentes: number;
  ausentes: number;
  visitantes: number;
  aulasConsolidadas: number;
  aulasNaoRealizadas: number;
  aulasEmAberto: number;
  pendenteFechamento: boolean;
};

export type ResumoDiaVM = {
  data: string;
  turmas: ResumoDiaTurmaVM[];
  totalPresentes: number;
  totalAusentes: number;
  totalVisitantes: number;
  totalAulasConsolidadas: number;
  totalAulasNaoRealizadas: number;
  totalAulasPendentes: number;
  aulasPendentes: AulaResumidaVM[];
  aulasNaoRealizadas: AulaResumidaVM[];
};

export async function obterResumoDia(dataIso: string): Promise<ResumoDiaVM> {
  const resposta = await clienteHttp.get(`/api/relatorios/ebd/resumo-dia`, {
    params: { data: dataIso },
  });
  const bruto: any = resposta.data ?? {};
  const listaTurmas = Array.isArray(bruto?.Turmas ?? bruto?.turmas)
    ? (bruto?.Turmas ?? bruto?.turmas)
    : [];
  return {
    data: String(bruto?.Data ?? bruto?.data ?? dataIso),
    turmas: listaTurmas.map((x: any) => ({
      departamentoId: Number(x?.DepartamentoId ?? x?.departamentoId ?? 0),
      nome: String(x?.Nome ?? x?.nome ?? ""),
      temChamada: Boolean(x?.TemChamada ?? x?.temChamada ?? false),
      presentes: Number(x?.Presentes ?? x?.presentes ?? 0),
      ausentes: Number(x?.Ausentes ?? x?.ausentes ?? 0),
      visitantes: Number(x?.Visitantes ?? x?.visitantes ?? 0),
      aulasConsolidadas: Number(
        x?.AulasConsolidadas ?? x?.aulasConsolidadas ?? 0,
      ),
      aulasNaoRealizadas: Number(
        x?.AulasNaoRealizadas ?? x?.aulasNaoRealizadas ?? 0,
      ),
      aulasEmAberto: Number(x?.AulasEmAberto ?? x?.aulasEmAberto ?? 0),
      pendenteFechamento: Boolean(
        x?.PendenteFechamento ?? x?.pendenteFechamento ?? false,
      ),
    })),
    totalPresentes: Number(bruto?.TotalPresentes ?? bruto?.totalPresentes ?? 0),
    totalAusentes: Number(bruto?.TotalAusentes ?? bruto?.totalAusentes ?? 0),
    totalVisitantes: Number(
      bruto?.TotalVisitantes ?? bruto?.totalVisitantes ?? 0,
    ),
    totalAulasConsolidadas: Number(
      bruto?.TotalAulasConsolidadas ?? bruto?.totalAulasConsolidadas ?? 0,
    ),
    totalAulasNaoRealizadas: Number(
      bruto?.TotalAulasNaoRealizadas ?? bruto?.totalAulasNaoRealizadas ?? 0,
    ),
    totalAulasPendentes: Number(
      bruto?.TotalAulasPendentes ?? bruto?.totalAulasPendentes ?? 0,
    ),
    aulasPendentes: normalizarListaAulasResumidas(
      bruto?.AulasPendentes ?? bruto?.aulasPendentes,
    ),
    aulasNaoRealizadas: normalizarListaAulasResumidas(
      bruto?.AulasNaoRealizadas ?? bruto?.aulasNaoRealizadas,
    ),
  };
}
