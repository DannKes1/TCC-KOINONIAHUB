import type { PessoaVM } from "../modelos/dtos";
import { clienteHttp } from "./clienteHttp";
import type {
  MatriculaCriarDTO,
  MatriculaRespostaVM,
  AlunoDaClasseVM,
} from "../modelos/dtos";

function normalizarMatricula(bruto: any): MatriculaRespostaVM {
  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    pessoaId: bruto?.PessoaId ?? bruto?.pessoaId ?? 0,
    nomePessoa: bruto?.NomePessoa ?? bruto?.nomePessoa ?? "",
    departamentoId: bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0,
    nomeDepartamento: bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    ativo: Boolean(bruto?.Ativo ?? bruto?.ativo ?? true),
    dataMatricula: String(bruto?.DataMatricula ?? bruto?.dataMatricula ?? ""),
    dataSaida: (bruto?.DataSaida ?? bruto?.dataSaida ?? null) as string | null,
    observacao: (bruto?.Observacao ?? bruto?.observacao ?? null) as
      | string
      | null,
  };
}

function normalizarAlunoDaClasse(bruto: any): AlunoDaClasseVM {
  return {
    matriculaId: bruto?.MatriculaId ?? bruto?.matriculaId ?? 0,
    pessoaId: bruto?.PessoaId ?? bruto?.pessoaId ?? 0,
    nome: bruto?.Nome ?? bruto?.nome ?? "",
    statusPessoa: bruto?.StatusPessoa ?? bruto?.statusPessoa ?? null,
    matriculaAtiva: Boolean(
      bruto?.MatriculaAtiva ?? bruto?.matriculaAtiva ?? true,
    ),
    dataMatricula: String(bruto?.DataMatricula ?? bruto?.dataMatricula ?? ""),
  };
}

export async function matricular(
  departamentoId: number,
  dto: MatriculaCriarDTO,
) {
  const resposta = await clienteHttp.post(
    `/api/departamentos/${departamentoId}/matriculas`,
    dto,
  );
  return normalizarMatricula(resposta.data);
}

export async function listarAlunosDaTurma(departamentoId: number) {
  const resposta = await clienteHttp.get(
    `/api/departamentos/${departamentoId}/alunos`,
  );
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarAlunoDaClasse);
}

export async function removerMatricula(
  departamentoId: number,
  matriculaId: number,
) {
  await clienteHttp.delete(
    `/api/departamentos/${departamentoId}/matriculas/${matriculaId}`,
  );
}
export async function listarPessoasDisponiveisDaTurma(
  departamentoId: number,
): Promise<PessoaVM[]> {
  const resposta = await clienteHttp.get(
    `/api/departamentos/${departamentoId}/pessoas-disponiveis`,
  );
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map((bruto: any) => ({
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
  }));
}
