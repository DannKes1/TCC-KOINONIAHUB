import type { Router } from "vue-router";
import { usarAutenticacaoStore } from "../armazenamentos/autenticacaoStore";
import { toastWarn } from "../servicos/notificacoes";

// RNF 21.4 (7.1): a Página da Turma e suas abas são a área de operação da EBD; uma
// conta de perfil Usuário não a alcança, ainda que a pessoa tenha atribuição. A
// guarda devolve a Minhas Turmas com um único aviso, em vez de carregar a página
// e deixar cada consulta responder 403.
export const AVISO_SEM_PERFIL_OPERACIONAL =
  "Sua conta não tem perfil para operar turmas. Se você é professor(a) ou auxiliar, peça a um Admin para ajustar o perfil da sua conta.";

export function aplicarGuardas(router: Router) {
  router.beforeEach((to) => {
    const autenticacao = usarAutenticacaoStore();

    const requerAdministrativo = Boolean(to.meta?.requerAdministrativo);

    if (autenticacao.tokenExpirado) {
      autenticacao.sair();
    }

    const requerAutenticacao = Boolean(to.meta?.requerAutenticacao);
    const requerVisitante = Boolean(to.meta?.requerVisitante);
    const requerAdmin = Boolean(to.meta?.requerAdmin);
    const requerGestor = Boolean(to.meta?.requerGestor);
    const telaTermo = Boolean(to.meta?.telaTermo);

    if (requerAutenticacao && !autenticacao.autenticado) {
      return { path: "/login", query: { redirecionar: to.fullPath } };
    }

    if (requerVisitante && autenticacao.autenticado) {
      return "/";
    }

    // RNF 2.5 / 13.4: com aceite do Termo de Uso e Sigilo pendente, a única tela
    // da sessão é /termo; o destino original volta pela query após o aceite.
    if (autenticacao.autenticado && autenticacao.termoPendente && !telaTermo) {
      return { path: "/termo", query: { redirecionar: to.fullPath } };
    }

    if (requerAdmin && !autenticacao.isAdmin) {
      return "/";
    }

    // Gestor = administrativo ou Professor (a área de operação da EBD).
    if (requerGestor && !autenticacao.isGestor) {
      toastWarn(AVISO_SEM_PERFIL_OPERACIONAL, "Sem permissão");
      return "/minhas-turmas";
    }

    if (requerAdministrativo && !autenticacao.isAdministrativo) {
      return "/";
    }
  });
}
