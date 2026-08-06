import { clienteHttp } from "./clienteHttp";
import type { ParentescoVM, ParentescoCriarDTO } from "../modelos/dtos";

function normalizarParentesco(bruto: any): ParentescoVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    pessoaId: Number(bruto?.PessoaId ?? bruto?.pessoaId ?? 0),
    parenteId: Number(bruto?.ParenteId ?? bruto?.parenteId ?? 0),
    tipoRelacionamento: String(
      bruto?.TipoRelacionamento ?? bruto?.tipoRelacionamento ?? "",
    ),
    parenteNome: String(bruto?.ParenteNome ?? bruto?.parenteNome ?? ""),
    parenteTelefone: (bruto?.ParenteTelefone ??
      bruto?.parenteTelefone ??
      null) as string | null,
    parenteCelular: (bruto?.ParenteCelular ?? bruto?.parenteCelular ?? null) as
      | string
      | null,
  };
}

export async function listarParentescosDaPessoa(pessoaId: number) {
  const resposta = await clienteHttp.get(
    `/api/pessoas/${pessoaId}/parentescos`,
  );
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarParentesco);
}

export async function criarParentesco(
  pessoaId: number,
  dto: ParentescoCriarDTO,
) {
  const resposta = await clienteHttp.post(
    `/api/pessoas/${pessoaId}/parentescos`,
    dto,
  );
  return normalizarParentesco(resposta.data);
}

export async function removerParentesco(
  pessoaId: number,
  parentescoId: number,
) {
  await clienteHttp.delete(
    `/api/pessoas/${pessoaId}/parentescos/${parentescoId}`,
  );
}
