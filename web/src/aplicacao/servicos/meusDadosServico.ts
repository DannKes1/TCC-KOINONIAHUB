import { clienteHttp } from "./clienteHttp";
import type { PessoaVM } from "../modelos/dtos";

export type MeusDadosAtualizarDTO = {
  Telefone?: string | null;
  Celular?: string | null;
  Email?: string | null;
  Endereco?: string | null;
  Bairro?: string | null;
  Cidade?: string | null;
  Estado?: string | null;
  CEP?: string | null;
};


export type MeusDadosVM = PessoaVM;

function normalizarMeusDados(bruto: any): MeusDadosVM {
  return {
    id: Number(bruto?.Id ?? bruto?.id ?? 0),
    nome: String(bruto?.Nome ?? bruto?.nome ?? ""),
    cpf: (bruto?.CPF ?? bruto?.cpf ?? null) as string | null,
    dataNascimento: (bruto?.DataNascimento ?? bruto?.dataNascimento ?? null) as
      | string
      | null,
    sexo: (bruto?.Sexo ?? bruto?.sexo ?? null) as string | null,
    estadoCivil: (bruto?.EstadoCivil ?? bruto?.estadoCivil ?? null) as
      | string
      | null,
    situacao: (bruto?.Situacao ?? bruto?.situacao ?? null) as string | null,
    categoria: (bruto?.Categoria ?? bruto?.categoria ?? null) as string | null,
    dataInativacao: (bruto?.DataInativacao ?? bruto?.dataInativacao ?? null) as
      | string
      | null,
    telefone: (bruto?.Telefone ?? bruto?.telefone ?? null) as string | null,
    celular: (bruto?.Celular ?? bruto?.celular ?? null) as string | null,
    email: (bruto?.Email ?? bruto?.email ?? null) as string | null,
    endereco: (bruto?.Endereco ?? bruto?.endereco ?? null) as string | null,
    bairro: (bruto?.Bairro ?? bruto?.bairro ?? null) as string | null,
    cidade: (bruto?.Cidade ?? bruto?.cidade ?? null) as string | null,
    estado: (bruto?.Estado ?? bruto?.estado ?? null) as string | null,
    cep: (bruto?.CEP ?? bruto?.cep ?? null) as string | null,
    dataBatismo: (bruto?.DataBatismo ?? bruto?.dataBatismo ?? null) as
      | string
      | null,
    dataMembresia: (bruto?.DataMembresia ?? bruto?.dataMembresia ?? null) as
      | string
      | null,
    fotoUrl: (bruto?.FotoUrl ?? bruto?.fotoUrl ?? null) as string | null,
    observacoes: (bruto?.Observacoes ?? bruto?.observacoes ?? null) as
      | string
      | null,
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
    atualizadoEm: (bruto?.AtualizadoEm ?? bruto?.atualizadoEm ?? null) as
      | string
      | null,
  };
}

export async function obterMeusDados(): Promise<MeusDadosVM> {
  const resposta = await clienteHttp.get("/api/meus-dados");
  return normalizarMeusDados(resposta.data);
}

export async function atualizarMeusDados(dto: MeusDadosAtualizarDTO) {
  await clienteHttp.put("/api/meus-dados", dto);
}

export type MinhaTurmaVM = {
  departamentoId: number;
  nome: string;
  tipo: string;
  ativo: boolean;
  vinculo: string;
  responsavel: string | null;
};

export async function listarMinhasTurmas(): Promise<MinhaTurmaVM[]> {
  const resposta = await clienteHttp.get("/api/meus-dados/minhas-turmas");
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map((bruto: any) => ({
    departamentoId: Number(bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0),
    nome: String(bruto?.Nome ?? bruto?.nome ?? ""),
    tipo: String(bruto?.Tipo ?? bruto?.tipo ?? ""),
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? false),
    vinculo: String(bruto?.Vinculo ?? bruto?.vinculo ?? ""),
    responsavel: bruto?.Responsavel ?? bruto?.responsavel ?? null,
  }));
}

export type MinhaFrequenciaAulaVM = {
  aulaId: number;
  data: string;
  tema: string | null;
  situacao: string; 
};

export type MinhaFrequenciaTurmaVM = {
  departamentoId: number;
  nomeDepartamento: string;
  dataInicio: string;
  dataFim: string;
  totalAulas: number;
  presentes: number;
  ausentesMarcados: number;
  naoRegistrado: number;
  percentualPresenca: number;
  aulas: MinhaFrequenciaAulaVM[];
};

export async function obterMinhaFrequencia(
  departamentoId: number,
): Promise<MinhaFrequenciaTurmaVM> {
  const resposta = await clienteHttp.get(
    "/api/relatorios/ebd/minha-frequencia",
    {
      params: { departamentoId },
    },
  );
  const b = resposta.data;
  return {
    departamentoId: Number(b?.DepartamentoId ?? b?.departamentoId ?? 0),
    nomeDepartamento: String(b?.NomeDepartamento ?? b?.nomeDepartamento ?? ""),
    dataInicio: String(b?.DataInicio ?? b?.dataInicio ?? ""),
    dataFim: String(b?.DataFim ?? b?.dataFim ?? ""),
    totalAulas: Number(b?.TotalAulas ?? b?.totalAulas ?? 0),
    presentes: Number(b?.Presentes ?? b?.presentes ?? 0),
    ausentesMarcados: Number(b?.AusentesMarcados ?? b?.ausentesMarcados ?? 0),
    naoRegistrado: Number(b?.NaoRegistrado ?? b?.naoRegistrado ?? 0),
    percentualPresenca: Number(
      b?.PercentualPresenca ?? b?.percentualPresenca ?? 0,
    ),
    aulas: (Array.isArray(b?.Aulas ?? b?.aulas)
      ? (b?.Aulas ?? b?.aulas)
      : []
    ).map((a: any) => ({
      aulaId: Number(a?.AulaId ?? a?.aulaId ?? 0),
      data: String(a?.Data ?? a?.data ?? ""),
      tema: (a?.Tema ?? a?.tema ?? null) as string | null,
      situacao: String(a?.Situacao ?? a?.situacao ?? ""),
    })),
  };
}
