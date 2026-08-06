import { clienteHttp } from "./clienteHttp";
import type {
  MateriaVM,
  MateriaCriarDTO,
  MateriaAtualizarDTO,
} from "../modelos/dtos";

function normalizarMateria(bruto: any): MateriaVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    nome: bruto?.Nome ?? bruto?.nome ?? "",
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? true),
    ordemExibicao: (bruto?.OrdemExibicao ?? bruto?.ordemExibicao ?? null) as
      | number
      | null,
    departamentoId: bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0,
    nomeDepartamento: bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
    atualizadoEm: (bruto?.AtualizadoEm ?? bruto?.atualizadoEm ?? null) as
      | string
      | null,
  };
}

export async function listarMaterias(departamentoId: number) {
  const resposta = await clienteHttp.get(`/api/materias`, {
    params: { departamentoId },
  });
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarMateria);
}

export async function obterMateria(id: number) {
  const resposta = await clienteHttp.get(`/api/materias/${id}`);
  return normalizarMateria(resposta.data);
}

export async function criarMateria(dto: MateriaCriarDTO) {
  const resposta = await clienteHttp.post(`/api/materias`, dto);
  return normalizarMateria(resposta.data);
}

export async function atualizarMateria(id: number, dto: MateriaAtualizarDTO) {
  await clienteHttp.put(`/api/materias/${id}`, dto); // 204
}
