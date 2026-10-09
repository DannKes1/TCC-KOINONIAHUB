import { request } from "@playwright/test";
import type { APIRequestContext, APIResponse } from "@playwright/test";

import { baseURL } from "./ambiente";

// Cliente da API para a semente e para os passos que não são o objeto do teste
// (Plano 7.3). Fala direto com a API (https, certificado de dev ignorado), com o
// Origin da SPA — sem ele toda escrita recebe 403 do middleware da RNF 2.6.
// O contexto guarda o cookie httpOnly da sessão entre as chamadas.

export const ORIGEM_SPA = baseURL;

export async function novoClienteApi(apiURL: string): Promise<APIRequestContext> {
  return request.newContext({
    baseURL: apiURL,
    ignoreHTTPSErrors: true,
    extraHTTPHeaders: {
      Origin: ORIGEM_SPA,
      Accept: "application/json",
    },
  });
}

async function corpoOk<T = any>(resposta: APIResponse, oQue: string): Promise<T> {
  if (!resposta.ok()) {
    throw new Error(
      `${oQue}: HTTP ${resposta.status()} ${resposta.statusText()} — ${await resposta.text()}`,
    );
  }
  const texto = await resposta.text();
  return (texto ? JSON.parse(texto) : null) as T;
}

export async function obterTermoVigente(api: APIRequestContext) {
  const r = await corpoOk(await api.get("/api/termo/vigente"), "GET termo/vigente");
  return { versao: String(r.versao), hash: String(r.hash) };
}

export async function registrarAdmin(
  api: APIRequestContext,
  dados: {
    nomeIgreja: string;
    emailIgreja: string;
    nomeAdmin: string;
    emailAdmin: string;
    senhaAdmin: string;
    versaoTermo: string;
  },
) {
  const r = await corpoOk(
    await api.post("/api/auth/registrar-admin", {
      data: {
        Igreja: {
          Nome: dados.nomeIgreja,
          Cidade: "Ji-Paraná",
          Estado: "RO",
          Email: dados.emailIgreja,
        },
        EmailAdmin: dados.emailAdmin,
        SenhaAdmin: dados.senhaAdmin,
        NomeAdmin: dados.nomeAdmin,
        AceiteTermoVersao: dados.versaoTermo,
      },
    }),
    "POST auth/registrar-admin",
  );
  return { usuarioId: Number(r.usuarioId), igrejaId: Number(r.igrejaId) };
}

export async function login(api: APIRequestContext, email: string, senha: string) {
  const r = await corpoOk(
    await api.post("/api/auth/login", { data: { Email: email, Senha: senha } }),
    `POST auth/login (${email})`,
  );
  return { termoPendente: Boolean(r.termoPendente), perfil: String(r.perfil) };
}

export async function logout(api: APIRequestContext) {
  await api.post("/api/auth/logout");
}

export async function aceitarTermo(api: APIRequestContext, versao: string) {
  await corpoOk(
    await api.post("/api/termo/aceitar", { data: { Versao: versao } }),
    "POST termo/aceitar",
  );
}

export async function criarPessoa(
  api: APIRequestContext,
  nome: string,
  email?: string,
): Promise<{ id: number; nome: string }> {
  const r = await corpoOk(
    await api.post("/api/pessoas", { data: { Nome: nome, Email: email ?? null, Situacao: "Ativo" } }),
    `POST pessoas (${nome})`,
  );
  return { id: Number(r.id), nome: String(r.nome) };
}

export async function criarDepartamento(api: APIRequestContext, nome: string): Promise<{ id: number; nome: string }> {
  const r = await corpoOk(
    await api.post("/api/departamentos", { data: { Nome: nome, Tipo: "EBD", Ativo: true } }),
    `POST departamentos (${nome})`,
  );
  return { id: Number(r.id), nome: String(r.nome) };
}

export async function criarAtribuicao(api: APIRequestContext, pessoaId: number, departamentoId: number) {
  await corpoOk(
    await api.post("/api/atribuicoes", {
      data: { PessoaId: pessoaId, DepartamentoId: departamentoId, Funcao: "Professor", Ativo: true },
    }),
    "POST atribuicoes",
  );
}

export async function matricular(api: APIRequestContext, departamentoId: number, pessoaId: number) {
  await corpoOk(
    await api.post(`/api/departamentos/${departamentoId}/matriculas`, { data: { PessoaId: pessoaId } }),
    `POST departamentos/${departamentoId}/matriculas`,
  );
}

export async function criarMateria(api: APIRequestContext, departamentoId: number, nome: string) {
  const r = await corpoOk(
    await api.post("/api/materias", {
      data: { Nome: nome, DepartamentoId: departamentoId, OrdemExibicao: 1, Ativo: true },
    }),
    `POST materias (${nome})`,
  );
  return { id: Number(r.id) };
}

// Sem `senha` a API gera o convite de primeiro acesso e devolve o token em claro
// uma única vez (RF39). Com `senha`, a conta nasce com senha definida pelo Admin e
// o aceite do termo fica para o primeiro login (RNF 13.4) — é o que o E2E-02 usa.
export async function criarUsuario(
  api: APIRequestContext,
  dados: { pessoaId: number; email: string; perfil: string; senha?: string },
): Promise<{ id: number; conviteToken: string | null }> {
  const r = await corpoOk(
    await api.post("/api/usuarios", {
      data: {
        PessoaId: dados.pessoaId,
        Email: dados.email,
        Perfil: dados.perfil,
        Senha: dados.senha ?? null,
      },
    }),
    `POST usuarios (${dados.email})`,
  );
  return { id: Number(r.id), conviteToken: (r.conviteToken ?? null) as string | null };
}

// Aula Em aberto na matéria informada (RF31). Quem chama precisa de acesso à turma:
// Admin, ou Professor/Auxiliar com atribuição ativa; o ProfessorId é o PessoaId de
// um professor com atribuição "Professor" ativa na turma.
export async function criarAula(
  api: APIRequestContext,
  dados: { materiaId: number; professorId: number; data: Date; tema?: string },
): Promise<{ id: number }> {
  const r = await corpoOk(
    await api.post("/api/aulas", {
      data: {
        Data: dados.data.toISOString(),
        Tema: dados.tema ?? null,
        MateriaId: dados.materiaId,
        ProfessorId: dados.professorId,
      },
    }),
    "POST aulas",
  );
  return { id: Number(r.id) };
}
