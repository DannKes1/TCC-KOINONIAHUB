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

export type PessoaVM = {
  id: number;
  nome: string;
  dataNascimento: string | null;
  sexo: string | null;
  estadoCivil: string | null;
  situacao: string | null;
  dataInativacao: string | null;
  celular: string | null;
  email: string | null;
  endereco: string | null;
  bairro: string | null;
  cidade: string | null;
  estado: string | null;
  cep: string | null;
  criadoEm: string;
  atualizadoEm: string | null;
};

export type PessoaCriarDTO = {
  Nome: string;
  DataNascimento?: string | null;
  Sexo?: string | null;
  EstadoCivil?: string | null;
  Celular?: string | null;
  Email?: string | null;
  Endereco?: string | null;
  Bairro?: string | null;
  Cidade?: string | null;
  Estado?: string | null;
  CEP?: string | null;
  Situacao?: string | null;
};

export type PessoaAtualizarDTO = PessoaCriarDTO;

export type ParentescoVM = {
  id: number;
  pessoaId: number;
  parenteId: number;
  tipoRelacionamento: string;
  parenteNome: string;
  parenteCelular: string | null;
};

export type ParentescoCriarDTO = {
  ParenteId: number;
  TipoRelacionamento: string;
};

export type UsuarioVM = {
  id: number;
  igrejaId: number;
  email: string;
  perfil: string;
  ativo: boolean;
  pessoaId: number | null;
  nomePessoa: string | null;
  convitePendente: boolean;
  // RF43: último aceite do Termo de Uso e Sigilo (null = nunca aceitou).
  aceiteTermo: AceiteTermoVM | null;
};

export type UsuarioCriarDTO = {
  PessoaId: number;
  Email?: string | null;
  // Senha agora é opcional: null/ausente => o sistema gera convite de primeiro acesso
  Senha?: string | null;
  Perfil: string;
};

export type UsuarioAtualizarDTO = {
  Perfil?: string | null;
  Ativo?: boolean | null;
};

// Retorno da criação de usuário (pode vir com o token do convite,
// exibido uma única vez).
export type UsuarioCriadoVM = UsuarioVM & {
  conviteToken: string | null;
  conviteExpiraEm: string | null;
};

// Convite de primeiro acesso gerado/regenerado pelo administrador.
export type ConviteVM = {
  usuarioId: number;
  email: string;
  nomePessoa: string | null;
  token: string;
  expiraEm: string;
};

// Resultado da importação de pessoas via CSV.
export type ImportacaoPessoasItemVM = {
  linha: number;
  nome: string;
  email: string | null;
  status: string; // "Criado" | "Ignorado" | "Erro"
  mensagem: string | null;
};

export type ImportacaoPessoasResultadoVM = {
  totalLinhas: number;
  criados: number;
  ignorados: number;
  erros: number;
  itens: ImportacaoPessoasItemVM[];
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

export type SituacaoAula = "EmAberto" | "Consolidada" | "NaoRealizada";

export type AulaVM = {
  id: number;
  data: string;
  tema: string | null;
  // Situação da aula (RF31): EmAberto | Consolidada | NaoRealizada.
  situacao: SituacaoAula;
  // Em aberto com data já ocorrida (RNF 31.3), calculado pela API.
  pendenteFechamento: boolean;
  quantidadeVisitantes: number;
  materiaId: number;
  nomeMateria: string;
  professorId: number;
  nomeProfessor: string;
  criadoEm: string;
};

// Aluno com matrícula ativa e sem registro de presença/ausência na aula, devolvido
// no 400 da consolidação (RNFs 32.6/33.3).
export type AlunoSemRegistroVM = {
  alunoDepartamentoId: number;
  nomeAluno: string;
};

export type AulaCriarDTO = {
  Data: string;
  Tema?: string | null;
  Conteudo?: string | null;
  Observacoes?: string | null;
  MateriaId: number;
  ProfessorId: number;
};

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

// Aula listada à parte nos relatórios (Plano 6.9): pendente de fechamento ou
// Não realizada. Não entra nos cálculos.
export type AulaResumidaVM = {
  id: number;
  data: string;
  materia: string;
  professor: string;
  departamentoId: number;
  departamento: string;
};

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
  // RNF 35.5: Em aberto com data já ocorrida no período.
  aulasPendentes: AulaResumidaVM[];
};

export type RankingFaltasVM = {
  departamentoId: number;
  nomeDepartamento: string;
  dataInicio: string;
  dataFim: string;
  itens: any[];
  aulasPendentes: AulaResumidaVM[];
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
  // Situação da aula do registro (RF34/CSU14); só Consolidada conta como frequência.
  situacaoAula: SituacaoAula;
};

// ---- Termo de Uso e Sigilo (RF42 / RF43) ----

export type TermoIgrejaVM = {
  nome: string;
  email: string | null;
  telefone: string | null;
};

export type TermoVigenteVM = {
  versao: string;
  vigenteDesde: string;
  texto: string;
  hash: string;
  // Identificação da igreja para o cabeçalho dinâmico (RNF 42.7): vem da sessão
  // ou do token de convite; no cadastro inicial não existe ainda (null).
  igreja: TermoIgrejaVM | null;
};

export type AceiteTermoVM = {
  versao: string;
  aceitoEm: string;
  meio: string;
  // false quando o aceite é de uma versão anterior à vigente.
  vigente: boolean;
};
