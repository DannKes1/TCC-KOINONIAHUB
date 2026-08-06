import { clienteHttp } from "./clienteHttp";
import type {
  ItemChamadaCompletaVM,
  PresencaVM,
  ChamadaRegistrarDTO,
} from "../modelos/dtos";

function normalizarItemChamada(bruto: any): ItemChamadaCompletaVM {
  return {
    alunoDepartamentoId: Number(
      bruto?.AlunoDepartamentoId ?? bruto?.alunoDepartamentoId ?? 0,
    ),
    pessoaId: Number(bruto?.PessoaId ?? bruto?.pessoaId ?? 0),
    nomeAluno: String(bruto?.NomeAluno ?? bruto?.nomeAluno ?? ""),
    presente: Boolean(bruto?.Presente ?? bruto?.presente ?? false),
    observacao: (bruto?.Observacao ?? bruto?.observacao ?? null) as
      | string
      | null,
  };
}

function normalizarPresenca(bruto: any): PresencaVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    aulaId: Number(bruto?.AulaId ?? bruto?.aulaId ?? 0),
    alunoDepartamentoId: Number(
      bruto?.AlunoDepartamentoId ?? bruto?.alunoDepartamentoId ?? 0,
    ),
    pessoaId: Number(bruto?.PessoaId ?? bruto?.pessoaId ?? 0),
    nomeAluno: String(bruto?.NomeAluno ?? bruto?.nomeAluno ?? ""),
    presente: Boolean(bruto?.Presente ?? bruto?.presente ?? false),
    observacao: (bruto?.Observacao ?? bruto?.observacao ?? null) as
      | string
      | null,
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
  };
}

export async function listarChamadaCompleta(aulaId: number) {
  const resposta = await clienteHttp.get(`/api/aulas/${aulaId}/chamada`);
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarItemChamada);
}

export async function listarPresencasRegistradas(aulaId: number) {
  const resposta = await clienteHttp.get(`/api/aulas/${aulaId}/presencas`);
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarPresenca);
}

export async function registrarChamada(
  aulaId: number,
  dto: ChamadaRegistrarDTO,
) {
  const resposta = await clienteHttp.post(
    `/api/aulas/${aulaId}/presencas`,
    dto,
  );
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarPresenca);
}
