import { createRouter, createWebHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import { aplicarGuardas } from "./guardas";
import PaginaMeusDados from "../../paginas/privado/meus-dados/PaginaMeusDados.vue";
import PaginaLogin from "../../paginas/publico/PaginaLogin.vue";
import PaginaCadastroInicial from "../../paginas/publico/PaginaCadastroInicial.vue";
import PaginaPrimeiroAcesso from "../../paginas/publico/PaginaPrimeiroAcesso.vue";

import LayoutPrincipal from "../../components/layout/LayoutPrincipal.vue";
import PaginaPainel from "../../paginas/privado/PaginaPainel.vue";
import PaginaDepartamentosLista from "../../paginas/privado/departamentos/PaginaDepartamentosLista.vue";
import PaginaPessoasLista from "../../paginas/privado/pessoas/PaginaPessoasLista.vue";
import PaginaUsuariosLista from "../../paginas/privado/usuarios/PaginaUsuariosLista.vue";
import PaginaMinhasTurmas from "../../paginas/privado/PaginaMinhasTurmas.vue";
import PaginaMatriculasTurma from "../../paginas/privado/matriculas/PaginaMatriculasTurma.vue";
import PaginaMateriasTurma from "../../paginas/privado/materias/PaginaMateriasTurma.vue";
import PaginaAulasTurma from "../../paginas/privado/aulas/PaginaAulasTurma.vue";
import PaginaChamadaAula from "../../paginas/privado/chamada/PaginaChamadaAula.vue";
import PaginaRelatoriosEbd from "../../paginas/privado/relatorios/PaginaRelatoriosEbd.vue";
import PaginaAtribuicoesDepartamento from "../../paginas/privado/atribuicoes/PaginaAtribuicoesDepartamento.vue";
import PaginaPresencasAula from "../../paginas/privado/chamada/PaginaPresencasAula.vue";
import PaginaMinhaFrequencia from "../../paginas/privado/minha-frequencia/PaginaMinhaFrequencia.vue";
const rotas: RouteRecordRaw[] = [
  {
    path: "/login",
    component: PaginaLogin,
    meta: { requerVisitante: true },
  },
  {
    path: "/cadastro-inicial",
    component: PaginaCadastroInicial,
    meta: { requerVisitante: true },
  },
  {
    path: "/primeiro-acesso",
    component: PaginaPrimeiroAcesso,
    meta: { requerVisitante: true },
  },
  {
    path: "/",
    component: LayoutPrincipal,
    meta: { requerAutenticacao: true },
    children: [
      {
        path: "",
        component: PaginaPainel,
      },
      {
        path: "pessoas",
        component: PaginaPessoasLista,
        meta: { requerAdministrativo: true },
      },
      {
        path: "usuarios",
        component: PaginaUsuariosLista,
        meta: { requerAdmin: true },
      },
      {
        path: "departamentos",
        component: PaginaDepartamentosLista,
      },
      {
        path: "departamentos/:departamentoId/matriculas",
        component: PaginaMatriculasTurma,
      },
      {
        path: "meus-dados",
        component: PaginaMeusDados,
      },
      {
        path: "minhas-turmas",
        component: PaginaMinhasTurmas,
      },
      {
        path: "departamentos/:departamentoId/materias",
        component: PaginaMateriasTurma,
      },
      {
        path: "departamentos/:departamentoId/aulas",
        component: PaginaAulasTurma,
      },
      {
        path: "departamentos/:departamentoId/minha-frequencia",
        component: PaginaMinhaFrequencia,
      },
      {
        path: "aulas/:aulaId/chamada",
        component: PaginaChamadaAula,
      },
      {
        path: "aulas/:aulaId/presencas",
        component: PaginaPresencasAula,
      },
      {
        path: "relatorios/ebd",
        component: PaginaRelatoriosEbd,
      },
      {
        path: "departamentos/:departamentoId/atribuicoes",
        component: PaginaAtribuicoesDepartamento,
      },
    ],
  },
  { path: "/:pathMatch(.*)*", redirect: "/" },
];

const router = createRouter({
  history: createWebHistory(),
  routes: rotas,
});

aplicarGuardas(router);

export default router;
