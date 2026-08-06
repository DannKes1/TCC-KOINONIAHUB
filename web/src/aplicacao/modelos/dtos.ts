// =====================
// Departamentos (Turmas)
// =====================
export type DepartamentoVM = {
  id: number;
  nome: string;
  tipo: string;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string | null;
};

export type DepartamentoCriarDTO = {
  Nome: string;
  Tipo?: string;
  Ativo?: boolean;
};

export type DepartamentoAtualizarDTO = {
  Nome: string;
  Tipo?: string;
  Ativo: boolean;
};

// ==========
// Pessoas
// ==========
export type PessoaVM = {
  id: number;
  nome: string;
  cpf: string | null;
  dataNascimento: string | null;
  sexo: string | null;
  estadoCivil: string | null;
  situacao: string | null;
  categoria: string | null;
  dataInativacao: string | null;
  telefone: string | null;
  celular: string | null;
  email: string | null;
  endereco: string | null;
  bairro: string | null;
  cidade: string | null;
  estado: string | null;
  cep: string | null;
  dataBatismo: string | null;
  dataMembresia: string | null;
  fotoUrl: string | null;
  observacoes: string | null;
  criadoEm: string;
  atualizadoEm: string | null;
};

export type PessoaCriarDTO = {
  Nome: string;
  CPF?: string | null;
  DataNascimento?: string | null;
  Sexo?: string | null;
  EstadoCivil?: string | null;
  Telefone?: string | null;
  Celular?: string | null;
  Email?: string | null;
  Endereco?: string | null;
  Bairro?: string | null;
  Cidade?: string | null;
  Estado?: string | null;
  CEP?: string | null;
  Situacao?: string | null;
  Categoria?: string | null;
  DataBatismo?: string | null;
  DataMembresia?: string | null;
  FotoUrl?: string | null;
  Observacoes?: string | null;
};

export type PessoaAtualizarDTO = PessoaCriarDTO;

export type ParentescoVM = {
  id: number;
  pessoaId: number;
  parenteId: number;
  tipoRelacionamento: string;
  parenteNome: string;
  parenteTelefone: string | null;
  parenteCelular: string | null;
};

export type ParentescoCriarDTO = {
  ParenteId: number;
  TipoRelacionamento: string;
};

// ==========
// Usuários
// ==========
export type UsuarioVM = {
  id: number;
  igrejaId: number;
  email: string;
  perfil: string;
  ativo: boolean;
  pessoaId: number | null;
  nomePessoa: string | null;
};

export type UsuarioCriarDTO = {
  PessoaId: number;
  Email?: string | null;
  Senha: string;
  Perfil: string;
};

export type UsuarioAtualizarDTO = {
  Perfil?: string | null;
  Ativo?: boolean | null;
};

export type UsuarioResetarSenhaDTO = {
  NovaSenha: string;
};

export type AtribuicaoVM = {
  id: number;
  pessoaId: number;
  pessoaNome: string;
  departamentoId: number;
  departamentoNome: string;
  funcao: string;
  dataInicio: string;
  dataFim: string | null;
  ativo: boolean;
};

export type AtribuicaoCriarDTO = {
  PessoaId: number;
  DepartamentoId: number;
  Funcao: string;
  DataInicio?: string | null;
  Ativo: boolean;
};

export type AtribuicaoAtualizarDTO = {
  Funcao: string;
  Ativo: boolean;
  DataFim?: string | null;
};

// ==========
// Matrículas
// ==========
export type MatriculaRespostaVM = {
  id: number;
  pessoaId: number;
  nomePessoa: string;
  departamentoId: number;
  nomeDepartamento: string;
  ativo: boolean;
  dataMatricula: string;
  dataSaida: string | null;
  observacao: string | null;
};

export type MatriculaCriarDTO = {
  PessoaId: number;
  Observacao?: string | null;
};

export type AlunoDaClasseVM = {
  matriculaId: number;
  pessoaId: number;
  nome: string;
  statusPessoa: string | null;
  matriculaAtiva: boolean;
  dataMatricula: string;
};

// =======
// Matérias
// =======
export type MateriaVM = {
  id: number;
  nome: string;
  ativo: boolean;
  ordemExibicao: number | null;
  departamentoId: number;
  nomeDepartamento: string;
  criadoEm: string;
  atualizadoEm: string | null;
};

export type MateriaCriarDTO = {
  Nome: string;
  Descricao?: string | null;
  ImagemUrl?: string | null;
  OrdemExibicao?: number | null;
  Ativo: boolean;
  DepartamentoId: number;
};

export type MateriaAtualizarDTO = MateriaCriarDTO;

// ====
// Aulas
// ====
export type AulaVM = {
  id: number;
  data: string;
  tema: string | null;
  consolidada: boolean;
  quantidadeVisitantes: number;
  materiaId: number;
  nomeMateria: string;
  professorId: number;
  nomeProfessor: string;
  criadoEm: string;
};

export type AulaCriarDTO = {
  Data: string;
  Tema?: string | null;
  Conteudo?: string | null;
  Observacoes?: string | null;
  MateriaId: number;
  ProfessorId: number;
};

// =========
// Chamada
// =========
export type ItemChamadaCompletaVM = {
  alunoDepartamentoId: number;
  pessoaId: number;
  nomeAluno: string;
  presente: boolean;
  observacao: string | null;
};

export type PresencaVM = {
  id: number;
  aulaId: number;
  alunoDepartamentoId: number;
  pessoaId: number;
  nomeAluno: string;
  presente: boolean;
  observacao: string | null;
  criadoEm: string;
};

export type ChamadaRegistrarDTO = {
  QuantidadeVisitantes?: number | null;
  Itens: Array<{
    AlunoDepartamentoId: number;
    Presente: boolean;
    Observacao?: string | null;
  }>;
};

// ==========
// Relatórios
// ==========
export type FrequenciaTurmaVM = {
  departamentoId: number;
  nomeDepartamento: string;
  dataInicio: string;
  dataFim: string;
  totalAulas: number;
  totalAlunos: number;
  totalPresentes: number;
  totalAusentesMarcados: number;
  totalNaoRegistrado: number;
  percentualPresencaGeral: number;
  alunos: any[];
  aulas: any[];
};

export type RankingFaltasVM = {
  departamentoId: number;
  nomeDepartamento: string;
  dataInicio: string;
  dataFim: string;
  itens: any[];
};
export type HistoricoPresencaPessoaVM = {
  aulaId: number;
  dataAula: string;
  departamentoId: number;
  departamentoNome: string;
  materiaId: number;
  materiaNome: string;
  presente: boolean;
  observacao: string | null;
};
