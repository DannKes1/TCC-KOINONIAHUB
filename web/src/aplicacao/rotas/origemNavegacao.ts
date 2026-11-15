import { ref } from "vue";
import type { NavigationFailure, RouteLocationNormalized } from "vue-router";

// Ajuste D1: o link de voltar leva à tela de onde a pessoa veio.
//
// Página da Turma: aberta por Minhas Turmas, volta para Minhas Turmas; aberta por Turmas
// EBD, volta para Turmas EBD, em qualquer perfil. A origem vale para a "família" da turma
// (abas, chamada e presenças das aulas), então sobrevive à troca de aba e à ida e volta da
// chamada. Ela fica só na memória: com o endereço digitado ou a página recarregada, a
// origem é desconhecida e vale a regra por perfil (gestão → Turmas EBD; Professor → Minhas
// Turmas).
//
// Minha frequência: aberta pelo Painel, volta para o Painel; nos demais casos, para Minhas
// Turmas, como antes.

export type Voltar = { para: string; rotulo: string };

type OrigemTurma = "minhas-turmas" | "turmas-ebd";

const VOLTAR_MINHAS_TURMAS: Voltar = { para: "/minhas-turmas", rotulo: "Minhas Turmas" };
const VOLTAR_TURMAS_EBD: Voltar = { para: "/departamentos", rotulo: "Turmas EBD" };
const VOLTAR_PAINEL: Voltar = { para: "/", rotulo: "Painel" };

const ABAS_DA_TURMA = /^\/departamentos\/\d+(\/(matriculas|materias|aulas|atribuicoes))?$/;
const PAGINAS_DA_AULA = /^\/aulas\/\d+\/(chamada|presencas)$/;
const MINHA_FREQUENCIA = /^\/departamentos\/\d+\/minha-frequencia$/;

const origemDaTurma = ref<{ departamentoId: number | null; origem: OrigemTurma } | null>(null);
const frequenciaAbertaPeloPainel = ref(false);

// O roteador aceita barra no fim e maiúsculas (favorito, endereço digitado); a tela é
// reconhecida pelo caminho normalizado.
function caminhoDa(rota: RouteLocationNormalized) {
  return rota.path.toLowerCase().replace(/\/+$/, "") || "/";
}

function naFamiliaDaTurma(rota: RouteLocationNormalized) {
  const caminho = caminhoDa(rota);
  return ABAS_DA_TURMA.test(caminho) || PAGINAS_DA_AULA.test(caminho);
}

// A primeira navegação depois de carregar a página parte de START_LOCATION, que não
// casa com nenhuma rota: origem desconhecida.
function ehRotaReal(rota: RouteLocationNormalized) {
  return rota.matched.length > 0;
}

function turmaDaRota(rota: RouteLocationNormalized): number | null {
  const id = Number(rota.params?.departamentoId);
  return Number.isInteger(id) && id > 0 ? id : null;
}

function origemPelaTela(rota: RouteLocationNormalized): OrigemTurma | null {
  if (!ehRotaReal(rota)) return null;
  if (caminhoDa(rota) === "/minhas-turmas") return "minhas-turmas";
  if (caminhoDa(rota) === "/departamentos") return "turmas-ebd";
  return null;
}

/** Registrado em `router.afterEach`: anota a origem de cada navegação concluída. */
export function registrarOrigem(
  destino: RouteLocationNormalized,
  partida: RouteLocationNormalized,
  falha?: NavigationFailure | void,
) {
  if (falha) return;

  if (MINHA_FREQUENCIA.test(caminhoDa(destino))) {
    frequenciaAbertaPeloPainel.value = ehRotaReal(partida) && caminhoDa(partida) === "/";
  }

  if (!naFamiliaDaTurma(destino)) return;

  const turmaDestino = turmaDaRota(destino);

  // Entrada na turma vinda de fora: a origem é a tela de partida (ou desconhecida).
  if (!ehRotaReal(partida) || !naFamiliaDaTurma(partida)) {
    const origem = origemPelaTela(partida);
    origemDaTurma.value = origem ? { departamentoId: turmaDestino, origem } : null;
    return;
  }

  // Dentro da família (aba, chamada, presenças): a origem continua, desde que seja a
  // mesma turma (as páginas da aula não trazem a turma no endereço).
  const atual = origemDaTurma.value;
  if (atual && turmaDestino !== null && atual.departamentoId !== turmaDestino) {
    origemDaTurma.value = null;
  }
}

/** Link de voltar da Página da Turma: a origem conhecida ou a regra por perfil. */
export function voltarDaTurma(departamentoId: number, administrativo: boolean): Voltar {
  const atual = origemDaTurma.value;
  const origem = atual && atual.departamentoId === departamentoId ? atual.origem : null;
  if (origem === "minhas-turmas") return VOLTAR_MINHAS_TURMAS;
  if (origem === "turmas-ebd") return VOLTAR_TURMAS_EBD;
  return administrativo ? VOLTAR_TURMAS_EBD : VOLTAR_MINHAS_TURMAS;
}

/** Link de voltar de Minha frequência. */
export function voltarDaMinhaFrequencia(): Voltar {
  return frequenciaAbertaPeloPainel.value ? VOLTAR_PAINEL : VOLTAR_MINHAS_TURMAS;
}

/** Esquece as origens anotadas, como acontece ao recarregar a página. */
export function esquecerOrigens() {
  origemDaTurma.value = null;
  frequenciaAbertaPeloPainel.value = false;
}
