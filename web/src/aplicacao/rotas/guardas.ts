import type { Router } from "vue-router";
import { usarAutenticacaoStore } from "../armazenamentos/autenticacaoStore";

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

    if (requerGestor && !autenticacao.isGestor) {
      return "/";
    }

    if (requerAdministrativo && !autenticacao.isAdministrativo) {
      return "/";
    }
  });
}
