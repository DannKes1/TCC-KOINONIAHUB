import { clienteHttp } from "./clienteHttp";
import type { IgrejaAtualizarDTO, IgrejaVM } from "../modelos/dtos";

// RF44 — Editar Dados da Igreja (CSU24). Só a própria igreja: a API confere o id
// contra o token (RNF 44.2) e exige perfil Admin para alterar (RNF 44.1).

export function normalizarIgreja(bruto: any): IgrejaVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    nome: String(bruto?.Nome ?? bruto?.nome ?? ""),
    cidade: (bruto?.Cidade ?? bruto?.cidade ?? null) as string | null,
    estado: (bruto?.Estado ?? bruto?.estado ?? null) as string | null,
    email: (bruto?.Email ?? bruto?.email ?? null) as string | null,
    telefone: (bruto?.Telefone ?? bruto?.telefone ?? null) as string | null,
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
    atualizadoEm: (bruto?.AtualizadoEm ?? bruto?.atualizadoEm ?? null) as
      | string
      | null,
  };
}

export async function obterIgreja(id: number): Promise<IgrejaVM> {
  const resposta = await clienteHttp.get(`/api/igrejas/${id}`);
  return normalizarIgreja(resposta.data);
}

export async function atualizarIgreja(
  id: number,
  dto: IgrejaAtualizarDTO,
): Promise<IgrejaVM> {
  const resposta = await clienteHttp.put(`/api/igrejas/${id}`, dto);
  return normalizarIgreja(resposta.data);
}
