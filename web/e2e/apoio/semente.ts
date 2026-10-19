import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import type { APIRequestContext } from "@playwright/test";

import {
  aceitarTermo,
  consolidarAula,
  criarAtribuicao,
  criarAula,
  criarDepartamento,
  criarMateria,
  criarPessoa,
  criarUsuario,
  login,
  matricular,
  novoClienteApi,
  obterChamada,
  obterTermoVigente,
  registrarAdmin,
  registrarChamada,
} from "./api";

// Semente dos fluxos E2E (Plano 7.3): igreja, Admin, Professor com atribuição,
// turma com matrículas, matéria — tudo pela própria API, sem SQL. Cada execução
// cria uma igreja nova (isolamento por locatário, RNF 1.1), então o banco de teste
// não precisa ser limpo entre execuções; as igrejas "E2E …" só se acumulam.
//
// Além do que o Plano lista, a semente cria uma conta com senha definida pelo Admin
// e SEM aceite do termo (RNF 13.4) para o E2E-02 (uma por projeto do Playwright,
// porque cada conta só aceita o termo uma vez: `pendente` para o desktop e
// `pendenteMobile` para o celular), uma pessoa sem usuário para o E2E-03 gerar o
// convite e, desde a Etapa 6.3, um aluno com usuário (perfil Usuario, termo aceito)
// para o fluxo do aluno no celular (Painel e Minha Frequência).
//
// Etapa 7.2 (E2E-04 a E2E-08): cada cenário que grava aulas tem turma própria, para
// não mexer nas contagens da turma principal que o E2E-M1 confere:
//   turmasChamada     E2E-04, uma por projeto (desktop/celular), professor atribuído,
//                     3 alunos e uma matéria, sem aula;
//   turmaReabertura   E2E-06, 3 alunos e uma aula Consolidada (A e B presentes, C ausente);
//   turmaSemProfessor E2E-05, sem atribuição do professor: "Fora da Turma" (alvo do 403)
//                     e o usuário de perfil Usuario com atribuição Auxiliar ativa (21.4);
//   jaCadastrada      E2E-07, pessoa de nome e e-mail fixos que a fixture CSV repete
//                     (fixos servem porque cada rodada cria uma igreja nova).

export const SENHA_PADRAO = "E2e@12345";
// O projeto é ESM ("type": "module"): não existe __dirname.
const pastaDesteArquivo = dirname(fileURLToPath(import.meta.url));
export const PASTA_AUTH = resolve(pastaDesteArquivo, "..", ".auth");
export const ARQUIVO_SEMENTE = resolve(PASTA_AUTH, "semente.json");
export const ARQUIVO_ESTADO_ADMIN = resolve(PASTA_AUTH, "admin.json");
export const ARQUIVO_ESTADO_PROFESSOR = resolve(PASTA_AUTH, "professor.json");
export const ARQUIVO_ESTADO_ALUNO = resolve(PASTA_AUTH, "aluno.json");

type Conta = { nome: string; email: string; senha: string; pessoaId: number };

export type TurmaSemeada = {
  id: number;
  nome: string;
  materiaId: number;
  alunos: { pessoaId: number; nome: string }[];
};

// E2E-07: a fixture web/e2e/fixtures/importacao-pessoas.csv repete este nome (sem
// e-mail) e este e-mail (com outro nome).
export const PESSOA_JA_CADASTRADA = {
  nome: "Pessoa Já Cadastrada E2E",
  email: "ja.cadastrada@e2e.koinoniahub.test",
};

export type Semente = {
  id: string;
  apiURL: string;
  versaoTermo: string;
  igreja: { nome: string; email: string };
  admin: { nome: string; email: string; senha: string };
  professor: Conta;
  pendente: Conta;
  pendenteMobile: Conta;
  // Primeiro aluno da turma, com usuário próprio (perfil Usuario) e termo aceito.
  aluno: Conta;
  convidado: { nome: string; email: string; pessoaId: number };
  turma: { id: number; nome: string };
  materiaId: number;
  alunos: { pessoaId: number; nome: string }[];
  // Etapa 7.2.
  turmasChamada: { desktop: TurmaSemeada; mobile: TurmaSemeada };
  turmaReabertura: TurmaSemeada & { aulaId: number; tema: string };
  turmaSemProfessor: TurmaSemeada;
  foraDaTurma: { pessoaId: number; nome: string };
  usuarioComAtribuicao: Conta;
  jaCadastrada: { pessoaId: number; nome: string; email: string };
};

export function emailE2E(prefixo: string, id: string) {
  return `${prefixo}.${id}@e2e.koinoniahub.test`;
}

export async function criarSemente(apiURL: string): Promise<Semente> {
  const id = Date.now().toString(36);
  const api = await novoClienteApi(apiURL);

  try {
    const termo = await obterTermoVigente(api);

    const admin = { nome: "Admin E2E", email: emailE2E("admin", id), senha: SENHA_PADRAO };
    const igreja = { nome: `E2E Igreja ${id}`, email: emailE2E("igreja", id) };

    // Cadastro inicial pela API (CSU01): igreja + Admin + aceite do termo.
    await registrarAdmin(api, {
      nomeIgreja: igreja.nome,
      emailIgreja: igreja.email,
      nomeAdmin: admin.nome,
      emailAdmin: admin.email,
      senhaAdmin: admin.senha,
      versaoTermo: termo.versao,
    });
    await login(api, admin.email, admin.senha);

    const pessoaProfessor = await criarPessoa(api, `Professor E2E ${id}`, emailE2E("professor", id));
    const pessoaPendente = await criarPessoa(api, `Pendente E2E ${id}`, emailE2E("pendente", id));
    const pessoaPendenteMobile = await criarPessoa(api, `Pendente Celular E2E ${id}`, emailE2E("pendente.celular", id));
    const pessoaConvidado = await criarPessoa(api, `Convidado E2E ${id}`, emailE2E("convidado", id));
    const alunos: Semente["alunos"] = [];
    for (const n of [1, 2, 3]) {
      // O aluno 1 ganha e-mail e usuário próprio (fluxo do aluno no celular).
      const aluno = await criarPessoa(api, `Aluno ${n} E2E ${id}`, n === 1 ? emailE2E("aluno", id) : undefined);
      alunos.push({ pessoaId: aluno.id, nome: aluno.nome });
    }

    const turma = await criarDepartamento(api, `Turma E2E ${id}`);
    await criarAtribuicao(api, pessoaProfessor.id, turma.id);
    for (const aluno of alunos) await matricular(api, turma.id, aluno.pessoaId);
    const materia = await criarMateria(api, turma.id, "Matéria E2E");

    // Contas com senha definida pelo Admin (RNF 13.4). Professor e aluno aceitam o
    // termo pela API para que o storageState deles já entre direto nas telas.
    const professor: Conta = {
      nome: pessoaProfessor.nome,
      email: emailE2E("professor", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaProfessor.id,
    };
    await criarUsuario(api, { pessoaId: professor.pessoaId, email: professor.email, perfil: "Professor", senha: professor.senha });
    await aceitarTermoPelaApi(apiURL, professor, termo.versao);

    const aluno: Conta = {
      nome: alunos[0]!.nome,
      email: emailE2E("aluno", id),
      senha: SENHA_PADRAO,
      pessoaId: alunos[0]!.pessoaId,
    };
    await criarUsuario(api, { pessoaId: aluno.pessoaId, email: aluno.email, perfil: "Usuario", senha: aluno.senha });
    await aceitarTermoPelaApi(apiURL, aluno, termo.versao);

    // Contas pendentes (E2E-02, uma por projeto): senha definida pelo Admin, nenhum aceite.
    const pendente: Conta = {
      nome: pessoaPendente.nome,
      email: emailE2E("pendente", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaPendente.id,
    };
    await criarUsuario(api, { pessoaId: pendente.pessoaId, email: pendente.email, perfil: "Usuario", senha: pendente.senha });

    const pendenteMobile: Conta = {
      nome: pessoaPendenteMobile.nome,
      email: emailE2E("pendente.celular", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaPendenteMobile.id,
    };
    await criarUsuario(api, { pessoaId: pendenteMobile.pessoaId, email: pendenteMobile.email, perfil: "Usuario", senha: pendenteMobile.senha });

    // ---- Etapa 7.2 ----

    // E2E-04: uma turma por projeto, ainda sem aula (a aula nasce pela tela).
    const turmasChamada = {
      desktop: await criarTurmaComAlunos(api, `Turma Chamada E2E ${id}`, "Chamada", professor.pessoaId),
      mobile: await criarTurmaComAlunos(api, `Turma Chamada Celular E2E ${id}`, "Chamada Celular", professor.pessoaId),
    };

    // E2E-06: aula Consolidada há uma semana, com A e B presentes e C ausente.
    const baseReabertura = await criarTurmaComAlunos(api, `Turma Reabertura E2E ${id}`, "Reabertura", professor.pessoaId);
    const temaReabertura = "Aula para reabrir E2E";
    const umaSemanaAtras = new Date();
    umaSemanaAtras.setDate(umaSemanaAtras.getDate() - 7);
    const aulaReabertura = await criarAula(api, {
      materiaId: baseReabertura.materiaId,
      professorId: professor.pessoaId,
      data: umaSemanaAtras,
      tema: temaReabertura,
    });
    const chamadaReabertura = await obterChamada(api, aulaReabertura.id);
    const ausente = baseReabertura.alunos[2]!.pessoaId;
    await registrarChamada(
      api,
      aulaReabertura.id,
      chamadaReabertura.map((l) => ({ alunoDepartamentoId: l.alunoDepartamentoId, presente: l.pessoaId !== ausente })),
    );
    await consolidarAula(api, aulaReabertura.id);

    // E2E-05: turma sem atribuição do professor.
    const turmaSemProfessor = await criarTurmaComAlunos(api, `Turma Sem Professor E2E ${id}`, "Sem Professor", null, 0);
    const pessoaFora = await criarPessoa(api, `Fora da Turma E2E ${id}`);
    await matricular(api, turmaSemProfessor.id, pessoaFora.id);
    turmaSemProfessor.alunos.push({ pessoaId: pessoaFora.id, nome: pessoaFora.nome });

    // RNF 21.4: conta de perfil Usuario, matriculada e com atribuição Auxiliar ativa
    // na mesma turma (o cenário do xUnit RF21_PerfilOperacionalTests).
    const pessoaUsuarioAtribuido = await criarPessoa(api, `Usuário com Atribuição E2E ${id}`, emailE2E("usuario.atribuido", id));
    await matricular(api, turmaSemProfessor.id, pessoaUsuarioAtribuido.id);
    turmaSemProfessor.alunos.push({ pessoaId: pessoaUsuarioAtribuido.id, nome: pessoaUsuarioAtribuido.nome });
    await criarAtribuicao(api, pessoaUsuarioAtribuido.id, turmaSemProfessor.id, "Auxiliar");
    const usuarioComAtribuicao: Conta = {
      nome: pessoaUsuarioAtribuido.nome,
      email: emailE2E("usuario.atribuido", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaUsuarioAtribuido.id,
    };
    await criarUsuario(api, { pessoaId: usuarioComAtribuicao.pessoaId, email: usuarioComAtribuicao.email, perfil: "Usuario", senha: usuarioComAtribuicao.senha });
    await aceitarTermoPelaApi(apiURL, usuarioComAtribuicao, termo.versao);

    // E2E-07: pessoa que a fixture CSV repete.
    const jaCadastrada = await criarPessoa(api, PESSOA_JA_CADASTRADA.nome, PESSOA_JA_CADASTRADA.email);

    const semente: Semente = {
      id,
      apiURL,
      versaoTermo: termo.versao,
      igreja,
      admin,
      professor,
      pendente,
      pendenteMobile,
      aluno,
      convidado: { nome: pessoaConvidado.nome, email: emailE2E("convidado", id), pessoaId: pessoaConvidado.id },
      turma,
      materiaId: materia.id,
      alunos,
      turmasChamada,
      turmaReabertura: { ...baseReabertura, aulaId: aulaReabertura.id, tema: temaReabertura },
      turmaSemProfessor,
      foraDaTurma: { pessoaId: pessoaFora.id, nome: pessoaFora.nome },
      usuarioComAtribuicao,
      jaCadastrada: { pessoaId: jaCadastrada.id, nome: jaCadastrada.nome, email: PESSOA_JA_CADASTRADA.email },
    };

    mkdirSync(dirname(ARQUIVO_SEMENTE), { recursive: true });
    writeFileSync(ARQUIVO_SEMENTE, JSON.stringify(semente, null, 2), "utf8");
    return semente;
  } finally {
    await api.dispose();
  }
}

// Turma com matéria e `quantidade` alunos novos (sem e-mail); com `professorId`, o
// professor recebe atribuição "Professor" ativa na turma.
async function criarTurmaComAlunos(
  api: APIRequestContext,
  nome: string,
  rotuloAlunos: string,
  professorId: number | null,
  quantidade = 3,
): Promise<TurmaSemeada> {
  const turma = await criarDepartamento(api, nome);
  if (professorId !== null) await criarAtribuicao(api, professorId, turma.id);
  const alunos: TurmaSemeada["alunos"] = [];
  const sufixo = nome.split(" ").pop();
  for (const letra of ["A", "B", "C"].slice(0, quantidade)) {
    const pessoa = await criarPessoa(api, `Aluno ${letra} ${rotuloAlunos} E2E ${sufixo}`);
    await matricular(api, turma.id, pessoa.id);
    alunos.push({ pessoaId: pessoa.id, nome: pessoa.nome });
  }
  const materia = await criarMateria(api, turma.id, `Matéria ${rotuloAlunos} E2E`);
  return { id: turma.id, nome: turma.nome, materiaId: materia.id, alunos };
}

async function aceitarTermoPelaApi(apiURL: string, conta: Conta, versaoTermo: string) {
  const apiConta = await novoClienteApi(apiURL);
  try {
    const sessao = await login(apiConta, conta.email, conta.senha);
    if (!sessao.termoPendente) {
      throw new Error(`Semente: a conta ${conta.email} deveria nascer com o termo pendente (RNF 13.4).`);
    }
    await aceitarTermo(apiConta, versaoTermo);
  } finally {
    await apiConta.dispose();
  }
}

export function lerSemente(): Semente {
  return JSON.parse(readFileSync(ARQUIVO_SEMENTE, "utf8")) as Semente;
}

// E2E-02 roda no desktop e no celular; cada projeto usa a sua conta pendente.
export function contaPendentePara(nomeProjeto: string, semente: Semente): Conta {
  return nomeProjeto === "mobile" ? semente.pendenteMobile : semente.pendente;
}

// E2E-04 roda no desktop e no celular; cada projeto usa a sua turma.
export function turmaChamadaPara(nomeProjeto: string, semente: Semente): TurmaSemeada {
  return nomeProjeto === "mobile" ? semente.turmasChamada.mobile : semente.turmasChamada.desktop;
}
