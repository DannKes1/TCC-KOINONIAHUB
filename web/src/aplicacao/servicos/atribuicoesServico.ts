import { clienteHttp } from "./clienteHttp";
import type {
  AtribuicaoVM,
  AtribuicaoCriarDTO,
  AtribuicaoAtualizarDTO,
} from "../modelos/dtos";

function normalizarAtribuicao(bruto: any): AtribuicaoVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    pessoaId: Number(bruto?.PessoaId ?? bruto?.pessoaId ?? 0),
    pessoaNome: String(bruto?.PessoaNome ?? bruto?.pessoaNome ?? ""),
    departamentoId: Number(bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0),
    departamentoNome: String(
      bruto?.DepartamentoNome ?? bruto?.departamentoNome ?? "",
    ),
    funcao: String(bruto?.Funcao ?? bruto?.funcao ?? ""),
    dataInicio: String(bruto?.DataInicio ?? bruto?.dataInicio ?? ""),
    dataFim: (bruto?.DataFim ?? bruto?.dataFim ?? null) as string | null,
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? false),
  };
}

export async function listarAtribuicoes() {
  const resposta = await clienteHttp.get("/api/atribuicoes");
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarAtribuicao);
}

export async function listarAtribuicoesPorDepartamento(
  departamentoId: number,
  filtros?: { funcao?: string | null; ativo?: boolean | null },
) {
  const resposta = await clienteHttp.get(
    `/api/atribuicoes/departamento/${departamentoId}`,
    {
      params: {
        funcao: filtros?.funcao || undefined,
        ativo:
          typeof filtros?.ativo === "boolean" ? filtros.ativo : undefined,
      },
    },
  );

  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarAtribuicao);
}

export async function obterAtribuicao(id: number) {
  const resposta = await clienteHttp.get(`/api/atribuicoes/${id}`);
  return normalizarAtribuicao(resposta.data);
}

export async function criarAtribuicao(dto: AtribuicaoCriarDTO) {
  const resposta = await clienteHttp.post("/api/atribuicoes", dto);
  return normalizarAtribuicao(resposta.data);
}

export async function atualizarAtribuicao(
  id: number,
  dto: AtribuicaoAtualizarDTO,
) {
  await clienteHttp.put(`/api/atribuicoes/${id}`, dto);
}

export async function encerrarAtribuicao(id: number) {
  await clienteHttp.patch(`/api/atribuicoes/${id}/encerrar`);
}