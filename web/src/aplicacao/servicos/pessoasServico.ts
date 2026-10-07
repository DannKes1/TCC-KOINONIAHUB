import { clienteHttp } from "./clienteHttp";
import type {
  PessoaVM,
  PessoaCriarDTO,
  PessoaAtualizarDTO,
  ImportacaoPessoasItemVM,
  ImportacaoPessoasResultadoVM,
} from "../modelos/dtos";

function normalizarPessoa(bruto: any): PessoaVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    nome: bruto?.Nome ?? bruto?.nome ?? "",
    dataNascimento: bruto?.DataNascimento ?? bruto?.dataNascimento ?? null,
    sexo: bruto?.Sexo ?? bruto?.sexo ?? null,
    estadoCivil: bruto?.EstadoCivil ?? bruto?.estadoCivil ?? null,
    situacao: bruto?.Situacao ?? bruto?.situacao ?? null,
    dataInativacao: bruto?.DataInativacao ?? bruto?.dataInativacao ?? null,
    celular: bruto?.Celular ?? bruto?.celular ?? null,
    email: bruto?.Email ?? bruto?.email ?? null,
    endereco: bruto?.Endereco ?? bruto?.endereco ?? null,
    bairro: bruto?.Bairro ?? bruto?.bairro ?? null,
    cidade: bruto?.Cidade ?? bruto?.cidade ?? null,
    estado: bruto?.Estado ?? bruto?.estado ?? null,
    cep: bruto?.CEP ?? bruto?.cep ?? null,
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
    atualizadoEm: (bruto?.AtualizadoEm ?? bruto?.atualizadoEm ?? null) as
      | string
      | null,
  };
}

export async function listarPessoas() {
  const resposta = await clienteHttp.get("/api/pessoas");
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarPessoa);
}

export async function obterPessoa(id: number) {
  const resposta = await clienteHttp.get(`/api/pessoas/${id}`);
  return normalizarPessoa(resposta.data);
}

export async function criarPessoa(dto: PessoaCriarDTO) {
  const resposta = await clienteHttp.post("/api/pessoas", dto);
  return normalizarPessoa(resposta.data);
}

export async function atualizarPessoa(id: number, dto: PessoaAtualizarDTO) {
  await clienteHttp.put(`/api/pessoas/${id}`, dto); // 204
}

// Resultado da importação (RF41 / RNF 41.3). Função pura para o Plano 7.2 poder
// testá-la: totais de criadas/ignoradas/erros e a marcação das linhas sinalizadas
// para conferência. Se a API não mandar os totais, eles são recontados dos itens.
export function normalizarResultadoImportacao(
  bruto: any,
): ImportacaoPessoasResultadoVM {
  const listaBruta = bruto?.Itens ?? bruto?.itens;
  const itens: ImportacaoPessoasItemVM[] = (
    Array.isArray(listaBruta) ? listaBruta : []
  ).map((i: any) => ({
    linha: Number(i?.Linha ?? i?.linha ?? 0),
    nome: String(i?.Nome ?? i?.nome ?? ""),
    email: (i?.Email ?? i?.email ?? null) as string | null,
    status: String(i?.Status ?? i?.status ?? "Erro"),
    mensagem: (i?.Mensagem ?? i?.mensagem ?? null) as string | null,
    paraConferencia: Boolean(i?.ParaConferencia ?? i?.paraConferencia ?? false),
  }));

  const contar = (status: string) =>
    itens.filter((i) => i.status === status).length;

  return {
    totalLinhas: Number(bruto?.TotalLinhas ?? bruto?.totalLinhas ?? itens.length),
    criados: Number(bruto?.Criados ?? bruto?.criados ?? contar("Criado")),
    ignorados: Number(bruto?.Ignorados ?? bruto?.ignorados ?? contar("Ignorado")),
    erros: Number(bruto?.Erros ?? bruto?.erros ?? contar("Erro")),
    paraConferencia: Number(
      bruto?.ParaConferencia ??
        bruto?.paraConferencia ??
        itens.filter((i) => i.paraConferencia).length,
    ),
    itens,
  };
}

// Importa pessoas em lote a partir de um arquivo CSV.
export async function importarPessoas(
  arquivo: File,
): Promise<ImportacaoPessoasResultadoVM> {
  const form = new FormData();
  form.append("arquivo", arquivo);

  const resposta = await clienteHttp.post("/api/pessoas/importar", form);
  return normalizarResultadoImportacao(resposta.data ?? {});
}
