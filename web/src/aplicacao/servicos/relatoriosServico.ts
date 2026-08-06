import { clienteHttp } from "./clienteHttp";
import type { FrequenciaTurmaVM } from "../modelos/dtos";

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

export type ResumoDiaTurmaVM = {
  departamentoId: number;
  nome: string;
  temChamada: boolean;
  presentes: number;
  ausentes: number;
  visitantes: number;
};

export type ResumoDiaVM = {
  data: string;
  turmas: ResumoDiaTurmaVM[];
  totalPresentes: number;
  totalAusentes: number;
  totalVisitantes: number;
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
    })),
    totalPresentes: Number(bruto?.TotalPresentes ?? bruto?.totalPresentes ?? 0),
    totalAusentes: Number(bruto?.TotalAusentes ?? bruto?.totalAusentes ?? 0),
    totalVisitantes: Number(
      bruto?.TotalVisitantes ?? bruto?.totalVisitantes ?? 0,
    ),
  };
}
