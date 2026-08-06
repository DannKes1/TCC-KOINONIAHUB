import { clienteHttp } from "./clienteHttp";
import type {
  DepartamentoVM,
  DepartamentoCriarDTO,
  DepartamentoAtualizarDTO,
} from "../modelos/dtos";

function normalizarDepartamento(bruto: any): DepartamentoVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    nome: bruto?.Nome ?? bruto?.nome ?? "",
    tipo: bruto?.Tipo ?? bruto?.tipo ?? "EBD",
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? true),
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
    atualizadoEm: (bruto?.AtualizadoEm ?? bruto?.atualizadoEm ?? null) as
      | string
      | null,
  };
}

export async function listarDepartamentos() {
  const resposta = await clienteHttp.get("/api/departamentos");
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarDepartamento);
}

export async function criarDepartamento(dto: DepartamentoCriarDTO) {
  const resposta = await clienteHttp.post("/api/departamentos", dto);
  return normalizarDepartamento(resposta.data);
}

export async function atualizarDepartamento(
  id: number,
  dto: DepartamentoAtualizarDTO,
) {
  await clienteHttp.put(`/api/departamentos/${id}`, dto); // 204 NoContent
}

export async function obterDepartamento(id: number) {
  const resposta = await clienteHttp.get(`/api/departamentos/${id}`);
  return normalizarDepartamento(resposta.data);
}
