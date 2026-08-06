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

    if (requerAutenticacao && !autenticacao.autenticado) {
      return { path: "/login", query: { redirecionar: to.fullPath } };
    }

    if (requerVisitante && autenticacao.autenticado) {
      return "/";
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
