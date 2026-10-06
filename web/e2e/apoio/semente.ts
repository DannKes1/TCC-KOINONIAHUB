import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import {
  aceitarTermo,
  criarAtribuicao,
  criarDepartamento,
  criarMateria,
  criarPessoa,
  criarUsuario,
  login,
  matricular,
  novoClienteApi,
  obterTermoVigente,
  registrarAdmin,
} from "./api";

// Semente dos fluxos E2E (Plano 7.3): igreja, Admin, Professor com atribuição,
// turma com matrículas, matéria — tudo pela própria API, sem SQL. Cada execução
// cria uma igreja nova (isolamento por locatário, RNF 1.1), então o banco de teste
// não precisa ser limpo entre execuções; as igrejas "E2E …" só se acumulam.
//
// Além do que o Plano lista, a semente cria uma conta com senha definida pelo Admin
// e SEM aceite do termo (RNF 13.4) para o E2E-02, e uma pessoa sem usuário para o
// E2E-03 gerar o convite.

export const SENHA_PADRAO = "E2e@12345";
// O projeto é ESM ("type": "module"): não existe __dirname.
const pastaDesteArquivo = dirname(fileURLToPath(import.meta.url));
export const PASTA_AUTH = resolve(pastaDesteArquivo, "..", ".auth");
export const ARQUIVO_SEMENTE = resolve(PASTA_AUTH, "semente.json");
export const ARQUIVO_ESTADO_ADMIN = resolve(PASTA_AUTH, "admin.json");
export const ARQUIVO_ESTADO_PROFESSOR = resolve(PASTA_AUTH, "professor.json");

export type Semente = {
  id: string;
  apiURL: string;
  versaoTermo: string;
  igreja: { nome: string; email: string };
  admin: { nome: string; email: string; senha: string };
  professor: { nome: string; email: string; senha: string; pessoaId: number };
  pendente: { nome: string; email: string; senha: string; pessoaId: number };
  convidado: { nome: string; email: string; pessoaId: number };
  turma: { id: number; nome: string };
  alunos: { pessoaId: number; nome: string }[];
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
    const pessoaConvidado = await criarPessoa(api, `Convidado E2E ${id}`, emailE2E("convidado", id));
    const alunos: Semente["alunos"] = [];
    for (const n of [1, 2, 3]) {
      const aluno = await criarPessoa(api, `Aluno ${n} E2E ${id}`);
      alunos.push({ pessoaId: aluno.id, nome: aluno.nome });
    }

    const turma = await criarDepartamento(api, `Turma E2E ${id}`);
    await criarAtribuicao(api, pessoaProfessor.id, turma.id);
    for (const aluno of alunos) await matricular(api, turma.id, aluno.pessoaId);
    await criarMateria(api, turma.id, "Matéria E2E");

    // Professor com senha definida pelo Admin; aceita o termo pela API para que o
    // storageState dele já entre direto nas telas.
    const professor = {
      nome: pessoaProfessor.nome,
      email: emailE2E("professor", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaProfessor.id,
    };
    await criarUsuario(api, { pessoaId: professor.pessoaId, email: professor.email, perfil: "Professor", senha: professor.senha });

    const apiProfessor = await novoClienteApi(apiURL);
    try {
      const sessao = await login(apiProfessor, professor.email, professor.senha);
      if (!sessao.termoPendente) throw new Error("Semente: a conta do professor deveria nascer com o termo pendente (RNF 13.4).");
      await aceitarTermo(apiProfessor, termo.versao);
    } finally {
      await apiProfessor.dispose();
    }

    // Conta pendente (E2E-02): senha definida pelo Admin, nenhum aceite.
    const pendente = {
      nome: pessoaPendente.nome,
      email: emailE2E("pendente", id),
      senha: SENHA_PADRAO,
      pessoaId: pessoaPendente.id,
    };
    await criarUsuario(api, { pessoaId: pendente.pessoaId, email: pendente.email, perfil: "Usuario", senha: pendente.senha });

    const semente: Semente = {
      id,
      apiURL,
      versaoTermo: termo.versao,
      igreja,
      admin,
      professor,
      pendente,
      convidado: { nome: pessoaConvidado.nome, email: emailE2E("convidado", id), pessoaId: pessoaConvidado.id },
      turma,
      alunos,
    };

    mkdirSync(dirname(ARQUIVO_SEMENTE), { recursive: true });
    writeFileSync(ARQUIVO_SEMENTE, JSON.stringify(semente, null, 2), "utf8");
    return semente;
  } finally {
    await api.dispose();
  }
}

export function lerSemente(): Semente {
  return JSON.parse(readFileSync(ARQUIVO_SEMENTE, "utf8")) as Semente;
}
