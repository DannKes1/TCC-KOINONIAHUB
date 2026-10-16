import { clienteHttp } from "./clienteHttp";
import type {
  AlunoSemRegistroVM,
  AulaVM,
  AulaAtualizarDTO,
  AulaCriarDTO,
} from "../modelos/dtos";
import {
  ehPendenteFechamento,
  normalizarSituacaoAula,
} from "../dominio/situacaoAula";

function normalizarAula(bruto: any): AulaVM {
  const situacao = normalizarSituacaoAula(bruto?.Situacao ?? bruto?.situacao);
  const data = String(bruto?.Data ?? bruto?.data ?? "");
  const pendenteDaApi = bruto?.PendenteFechamento ?? bruto?.pendenteFechamento;
  const possuiPresencasDaApi = bruto?.PossuiPresencas ?? bruto?.possuiPresencas;

  return {
    id: bruto?.Id ?? bruto?.id ?? 0,
    data,
    tema: bruto?.Tema ?? bruto?.tema ?? null,
    conteudo: bruto?.Conteudo ?? bruto?.conteudo ?? null,
    situacao,
    // A API calcula (Plano 6.2); a regra local só cobre resposta sem o campo.
    pendenteFechamento:
      typeof pendenteDaApi === "boolean"
        ? pendenteDaApi
        : ehPendenteFechamento(situacao, data),
    // Sem o campo (API anterior à 6.5), assume que há registros: Excluir não aparece.
    possuiPresencas:
      typeof possuiPresencasDaApi === "boolean" ? possuiPresencasDaApi : true,
    quantidadeVisitantes: Number(
      bruto?.QuantidadeVisitantes ?? bruto?.quantidadeVisitantes ?? 0,
    ),
    materiaId: bruto?.MateriaId ?? bruto?.materiaId ?? 0,
    nomeMateria: bruto?.NomeMateria ?? bruto?.nomeMateria ?? "",
    departamentoId: Number(bruto?.DepartamentoId ?? bruto?.departamentoId ?? 0),
    nomeDepartamento: String(
      bruto?.NomeDepartamento ?? bruto?.nomeDepartamento ?? "",
    ),
    professorId: bruto?.ProfessorId ?? bruto?.professorId ?? 0,
    nomeProfessor: bruto?.NomeProfessor ?? bruto?.nomeProfessor ?? "",
    criadoEm: String(bruto?.CriadoEm ?? bruto?.criadoEm ?? ""),
  };
}

export async function listarAulasPorDepartamento(departamentoId: number) {
  const resposta = await clienteHttp.get(`/api/aulas`, {
    params: { departamentoId },
  });
  const lista = Array.isArray(resposta.data) ? resposta.data : [];
  return lista.map(normalizarAula);
}

export async function obterAula(id: number) {
  const resposta = await clienteHttp.get(`/api/aulas/${id}`);
  return normalizarAula(resposta.data);
}

export async function criarAula(dto: AulaCriarDTO) {
  const resposta = await clienteHttp.post(`/api/aulas`, dto);
  return normalizarAula(resposta.data);
}

// RF45 — editar aula Em aberto (RNFs 45.2 e 45.4; 400 quando a situação, a
// matéria ou o professor não permitem).
export async function atualizarAula(id: number, dto: AulaAtualizarDTO) {
  const resposta = await clienteHttp.put(`/api/aulas/${id}`, dto);
  return normalizarAula(resposta.data);
}

// RF45 — excluir aula Em aberto sem nenhum registro de presença (RNF 45.5).
export async function excluirAula(id: number) {
  await clienteHttp.delete(`/api/aulas/${id}`);
}

// RF33 — consolidar (só Em aberto e com chamada completa; 400 com
// alunosSemRegistro[] quando faltar registro — ver extrairAlunosSemRegistro).
export async function consolidarAula(id: number) {
  await clienteHttp.patch(`/api/aulas/${id}/consolidar`);
}

// RF33 — marcar como Não realizada (só Em aberto e sem registros de presença).
export async function marcarAulaNaoRealizada(id: number) {
  await clienteHttp.patch(`/api/aulas/${id}/nao-realizada`);
}

// RF33 — reabrir (somente Admin; Consolidada ou Não realizada volta a Em aberto).
export async function reabrirAula(id: number) {
  await clienteHttp.patch(`/api/aulas/${id}/reabrir`);
}

// Lê a lista de alunos sem registro do corpo do 400 da consolidação
// (RNFs 32.6/33.3). Devolve [] para qualquer outro erro.
export function extrairAlunosSemRegistro(erro: unknown): AlunoSemRegistroVM[] {
  const lista = (erro as any)?.response?.data?.alunosSemRegistro;
  if (!Array.isArray(lista)) return [];

  return lista.map((item: any) => ({
    alunoDepartamentoId: Number(
      item?.alunoDepartamentoId ?? item?.AlunoDepartamentoId ?? 0,
    ),
    nomeAluno: String(item?.nomeAluno ?? item?.NomeAluno ?? ""),
  }));
}
