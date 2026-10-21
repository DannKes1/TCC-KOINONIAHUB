import { createPinia, setActivePinia } from "pinia";
import { createMemoryHistory, createRouter } from "vue-router";

import { AVISO_RESTRITO_ADMIN, AVISO_SEM_PERFIL_OPERACIONAL, aplicarGuardas } from "./guardas";
import { usarAutenticacaoStore } from "../armazenamentos/autenticacaoStore";
import { setToastHandler, type ToastPayload } from "../servicos/notificacoes";

// Plano 7.2 (RNF 2.5 / 13.4): com o aceite do Termo de Uso e Sigilo pendente, a
// única rota da sessão é /termo; o destino original volta pela query.
// Etapa 7.1 (RNF 21.4): a Página da Turma exige perfil gestor (administrativo ou
// Professor); o perfil Usuário volta a Minhas Turmas com um único aviso.
const Vazio = { template: "<div />" };

function criarRouter() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/login", component: Vazio, meta: { requerVisitante: true } },
      {
        path: "/termo",
        component: Vazio,
        meta: { requerAutenticacao: true, telaTermo: true },
      },
      {
        path: "/",
        component: Vazio,
        meta: { requerAutenticacao: true },
        children: [
          { path: "", component: Vazio },
          { path: "pessoas", component: Vazio, meta: { requerAdministrativo: true } },
          { path: "igreja", component: Vazio, meta: { requerAdmin: true } },
          { path: "meus-dados", component: Vazio },
          { path: "minhas-turmas", component: Vazio },
          {
            path: "departamentos/:departamentoId",
            component: Vazio,
            meta: { requerGestor: true },
            children: [
              { path: "aulas", component: Vazio },
              { path: "atribuicoes", component: Vazio, meta: { requerAdministrativo: true } },
            ],
          },
        ],
      },
    ],
  });
  aplicarGuardas(router);
  return router;
}

function entrarComo(perfil: string, termoPendente: boolean) {
  usarAutenticacaoStore().entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: `${perfil.toLowerCase()}@teste.com`,
    Perfil: perfil,
    IgrejaId: 1,
    TermoPendente: termoPendente,
  });
}

describe("guardas de rota — termo pendente", () => {
  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
  });

  it("com termo pendente, qualquer rota autenticada vai para /termo com o destino", async () => {
    entrarComo("Admin", true);
    const router = criarRouter();

    await router.push("/pessoas");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/termo");
    expect(router.currentRoute.value.query.redirecionar).toBe("/pessoas");
  });

  it("com termo pendente, /termo abre normalmente", async () => {
    entrarComo("Professor", true);
    const router = criarRouter();

    await router.push("/termo");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/termo");
  });

  it("sem pendência, as rotas seguem as regras normais", async () => {
    entrarComo("Admin", false);
    const router = criarRouter();

    await router.push("/meus-dados");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/meus-dados");
  });

  it("após marcar o aceite na store, a navegação é liberada na mesma sessão", async () => {
    entrarComo("Usuario", true);
    const router = criarRouter();

    await router.push("/meus-dados");
    await router.isReady();
    expect(router.currentRoute.value.path).toBe("/termo");

    usarAutenticacaoStore().marcarTermoPendente(false);
    await router.push("/meus-dados");

    expect(router.currentRoute.value.path).toBe("/meus-dados");
  });

  it("visitante sem sessão continua indo para /login (o termo não interfere)", async () => {
    const router = criarRouter();

    await router.push("/meus-dados");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/login");
    expect(router.currentRoute.value.query.redirecionar).toBe("/meus-dados");
  });
});

describe("guardas de rota — Página da Turma exige perfil gestor (RNF 21.4)", () => {
  const toasts: ToastPayload[] = [];

  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    toasts.length = 0;
    setToastHandler((payload) => toasts.push(payload));
  });

  it("perfil Usuário vai para /minhas-turmas com um único aviso, mesmo entrando por uma aba", async () => {
    entrarComo("Usuario", false);
    const router = criarRouter();

    await router.push("/departamentos/7/aulas");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/minhas-turmas");
    expect(toasts).toHaveLength(1);
    expect(toasts[0]).toMatchObject({ severity: "warn", summary: "Sem permissão", detail: AVISO_SEM_PERFIL_OPERACIONAL });
  });

  it("Professor abre a aba Aulas; a aba Atribuições continua só da gestão", async () => {
    entrarComo("Professor", false);
    const router = criarRouter();

    await router.push("/departamentos/7/aulas");
    await router.isReady();
    expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");

    await router.push("/departamentos/7/atribuicoes");
    expect(router.currentRoute.value.path).toBe("/");
    expect(toasts).toHaveLength(0);
  });
});

// Corretiva da 7.3 (CSU24 FA2): rota só do Admin digitada por outro perfil volta ao
// Painel e informa que o recurso é restrito a administradores.
describe("guardas de rota — rotas só do Admin avisam (CSU24 FA2)", () => {
  const toasts: ToastPayload[] = [];

  beforeEach(() => {
    localStorage.clear();
    setActivePinia(createPinia());
    toasts.length = 0;
    setToastHandler((payload) => toasts.push(payload));
  });

  it("Professor em /igreja volta ao Painel com um único aviso", async () => {
    entrarComo("Professor", false);
    const router = criarRouter();

    await router.push("/igreja");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/");
    expect(toasts).toHaveLength(1);
    expect(toasts[0]).toMatchObject({ severity: "warn", summary: "Sem permissão", detail: AVISO_RESTRITO_ADMIN });
  });

  it("Admin abre /igreja sem aviso", async () => {
    entrarComo("Admin", false);
    const router = criarRouter();

    await router.push("/igreja");
    await router.isReady();

    expect(router.currentRoute.value.path).toBe("/igreja");
    expect(toasts).toHaveLength(0);
  });
});
