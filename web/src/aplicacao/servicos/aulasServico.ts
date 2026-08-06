import { clienteHttp } from "./clienteHttp";
import type { AulaVM, AulaCriarDTO } from "../modelos/dtos";

function normalizarAula(bruto: any): AulaVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    data: String(bruto?.Data ?? bruto?.data ?? ""),
    tema: bruto?.Tema ?? bruto?.tema ?? null,
    consolidada: Boolean(bruto?.Consolidada ?? bruto?.consolidada ?? false),
    quantidadeVisitantes: Number(
      bruto?.QuantidadeVisitantes ?? bruto?.quantidadeVisitantes ?? 0,
    ),
    materiaId: bruto?.MateriaId ?? bruto?.materiaId ?? 0,
    nomeMateria: bruto?.NomeMateria ?? bruto?.nomeMateria ?? "",
    professorId: bruto?.ProfessorId ?? bruto?.professorId ?? 0,
    nomeProfessor: bruto?.NomeProfessor ?? bruto?.nomeProfessor ?? "",
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
  };
}

export async function listarAulasPorDepartamento(departamentoId: number) {
  const resposta = await clienteHttp.get(`/api/aulas`, {
    params: { departamentoId },
  });
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarAula);
}

export async function obterAula(id: number) {
  const resposta = await clienteHttp.get(`/api/aulas/${id}`);
  return normalizarAula(resposta.data);
}

export async function criarAula(dto: AulaCriarDTO) {
  const resposta = await clienteHttp.post(`/api/aulas`, dto);
  return normalizarAula(resposta.data);
}

export async function consolidarAula(id: number) {
  await clienteHttp.patch(`/api/aulas/${id}/consolidar`);
}
