import { createPinia, setActivePinia } from "pinia";
import { createMemoryHistory, createRouter } from "vue-router";

import { aplicarGuardas } from "./guardas";
import { usarAutenticacaoStore } from "../armazenamentos/autenticacaoStore";

// Plano 7.2 (RNF 2.5 / 13.4): com o aceite do Termo de Uso e Sigilo pendente, a
// única rota da sessão é /termo; o destino original volta pela query.
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
          { path: "meus-dados", component: Vazio },
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
