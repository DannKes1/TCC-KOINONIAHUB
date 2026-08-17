import { clienteHttp } from "./clienteHttp";
import type {
  PessoaVM,
  PessoaCriarDTO,
  PessoaAtualizarDTO,
  ImportacaoPessoasResultadoVM,
} from "../modelos/dtos";

function normalizarPessoa(bruto: any): PessoaVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    nome: bruto?.Nome ?? bruto?.nome ?? "",
    cpf: bruto?.CPF ?? bruto?.cpf ?? null,
    dataNascimento: bruto?.DataNascimento ?? bruto?.dataNascimento ?? null,
    sexo: bruto?.Sexo ?? bruto?.sexo ?? null,
    estadoCivil: bruto?.EstadoCivil ?? bruto?.estadoCivil ?? null,
    situacao: bruto?.Situacao ?? bruto?.situacao ?? null,
    categoria: bruto?.Categoria ?? bruto?.categoria ?? null,
    dataInativacao: bruto?.DataInativacao ?? bruto?.dataInativacao ?? null,
    telefone: bruto?.Telefone ?? bruto?.telefone ?? null,
    celular: bruto?.Celular ?? bruto?.celular ?? null,
    email: bruto?.Email ?? bruto?.email ?? null,
    endereco: bruto?.Endereco ?? bruto?.endereco ?? null,
    bairro: bruto?.Bairro ?? bruto?.bairro ?? null,
    cidade: bruto?.Cidade ?? bruto?.cidade ?? null,
    estado: bruto?.Estado ?? bruto?.estado ?? null,
    cep: bruto?.CEP ?? bruto?.cep ?? null,
    dataBatismo: bruto?.DataBatismo ?? bruto?.dataBatismo ?? null,
    dataMembresia: bruto?.DataMembresia ?? bruto?.dataMembresia ?? null,
    fotoUrl: bruto?.FotoUrl ?? bruto?.fotoUrl ?? null,
    observacoes: bruto?.Observacoes ?? bruto?.observacoes ?? null,
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

// Importa pessoas em lote a partir de um arquivo CSV.
export async function importarPessoas(
  arquivo: File,
): Promise<ImportacaoPessoasResultadoVM> {
  const form = new FormData();
  form.append("arquivo", arquivo);

  const resposta = await clienteHttp.post("/api/pessoas/importar", form);
  const bruto = resposta.data ?? {};

  const itens = Array.isArray(bruto.Itens ?? bruto.itens)
    ? (bruto.Itens ?? bruto.itens)
    : [];

  return {
    totalLinhas: Number(bruto.TotalLinhas ?? bruto.totalLinhas ?? 0),
    criados: Number(bruto.Criados ?? bruto.criados ?? 0),
    ignorados: Number(bruto.Ignorados ?? bruto.ignorados ?? 0),
    erros: Number(bruto.Erros ?? bruto.erros ?? 0),
    itens: itens.map((i: any) => ({
      linha: Number(i.Linha ?? i.linha ?? 0),
      nome: String(i.Nome ?? i.nome ?? ""),
      email: (i.Email ?? i.email ?? null) as string | null,
      status: String(i.Status ?? i.status ?? "Erro"),
      mensagem: (i.Mensagem ?? i.mensagem ?? null) as string | null,
    })),
  };
}
