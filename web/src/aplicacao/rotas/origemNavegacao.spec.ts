import { createMemoryHistory, createRouter } from "vue-router";

import {
  esquecerOrigens,
  registrarOrigem,
  voltarDaMinhaFrequencia,
  voltarDaTurma,
} from "./origemNavegacao";

// Ajuste D1: o link de voltar leva à tela de origem. A origem da Página da Turma vale para
// as abas, a chamada e as presenças; sem origem conhecida (primeira navegação depois de
// carregar a página), vale a regra por perfil. Roteador real, com as mesmas formas de rota.
const Vazio = { template: "<div />" };

async function roteador(...caminhos: string[]) {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/", component: Vazio },
      { path: "/minhas-turmas", component: Vazio },
      { path: "/departamentos", component: Vazio },
      { path: "/pessoas", component: Vazio },
      {
        path: "/departamentos/:departamentoId",
        component: Vazio,
        children: [
          { path: "", redirect: (to) => ({ path: `/departamentos/${String(to.params.departamentoId)}/aulas` }) },
          { path: "matriculas", component: Vazio },
          { path: "aulas", component: Vazio },
        ],
      },
      { path: "/departamentos/:departamentoId/minha-frequencia", component: Vazio },
      { path: "/aulas/:aulaId/chamada", component: Vazio },
      { path: "/aulas/:aulaId/presencas", component: Vazio },
      { path: "/proibida", component: Vazio },
    ],
  });
  router.beforeEach((to) => to.path !== "/proibida");
  router.afterEach(registrarOrigem);
  for (const caminho of caminhos) await router.push(caminho);
  return router;
}

describe("origemNavegacao (ajuste D1)", () => {
  beforeEach(() => {
    esquecerOrigens();
  });

  it("sem origem conhecida, vale a regra por perfil", async () => {
    await roteador("/departamentos/7/aulas");

    expect(voltarDaTurma(7, true)).toEqual({ para: "/departamentos", rotulo: "Turmas EBD" });
    expect(voltarDaTurma(7, false)).toEqual({ para: "/minhas-turmas", rotulo: "Minhas Turmas" });
  });

  it("aberta por Minhas Turmas, volta para Minhas Turmas em qualquer perfil, depois de trocar de aba e de ir à chamada", async () => {
    // O redirecionamento de /departamentos/7 para a aba Aulas mantém a origem.
    const router = await roteador("/minhas-turmas", "/departamentos/7");
    expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");
    expect(voltarDaTurma(7, true).rotulo).toBe("Minhas Turmas");

    await router.push("/departamentos/7/matriculas");
    await router.push("/departamentos/7/aulas");
    await router.push("/aulas/30/chamada");
    await router.push("/departamentos/7/aulas");
    await router.push("/aulas/30/presencas");
    await router.push("/departamentos/7/aulas");

    expect(voltarDaTurma(7, true)).toEqual({ para: "/minhas-turmas", rotulo: "Minhas Turmas" });
    expect(voltarDaTurma(7, false).rotulo).toBe("Minhas Turmas");
  });

  it("aberta por Turmas EBD, volta para Turmas EBD, também para o Professor", async () => {
    await roteador("/departamentos", "/departamentos/7/matriculas", "/aulas/30/chamada", "/departamentos/7/aulas");

    expect(voltarDaTurma(7, false)).toEqual({ para: "/departamentos", rotulo: "Turmas EBD" });
    expect(voltarDaTurma(7, true).rotulo).toBe("Turmas EBD");
  });

  it("entrar na turma por outra tela, ou passar a outra turma, esquece a origem anterior", async () => {
    const router = await roteador("/minhas-turmas", "/departamentos/7/aulas", "/", "/departamentos/7/aulas");
    expect(voltarDaTurma(7, true).rotulo).toBe("Turmas EBD");

    // Painel → chamada → turma: origem desconhecida.
    await router.push("/minhas-turmas");
    await router.push("/departamentos/7/aulas");
    await router.push("/");
    await router.push("/aulas/30/chamada");
    await router.push("/departamentos/7/aulas");
    expect(voltarDaTurma(7, true).rotulo).toBe("Turmas EBD");

    // A origem anotada para a turma 7 não vale para a turma 9.
    await router.push("/minhas-turmas");
    await router.push("/departamentos/7/aulas");
    await router.push("/aulas/30/chamada");
    await router.push("/departamentos/9/aulas");
    expect(voltarDaTurma(9, true).rotulo).toBe("Turmas EBD");
    expect(voltarDaTurma(7, true).rotulo).toBe("Turmas EBD");
  });

  it("navegação recusada não muda a origem; esquecer as origens, como na recarga, volta à regra por perfil", async () => {
    const router = await roteador("/minhas-turmas", "/departamentos/7/aulas");
    await router.push("/proibida");
    expect(router.currentRoute.value.path).toBe("/departamentos/7/aulas");
    expect(voltarDaTurma(7, true).rotulo).toBe("Minhas Turmas");

    esquecerOrigens();
    expect(voltarDaTurma(7, true).rotulo).toBe("Turmas EBD");
    expect(voltarDaTurma(7, false).rotulo).toBe("Minhas Turmas");
  });

  it("reconhece a tela de origem com barra no fim e maiúsculas (favorito ou endereço digitado)", async () => {
    await roteador("/minhas-turmas/", "/departamentos/7");
    expect(voltarDaTurma(7, true).rotulo).toBe("Minhas Turmas");

    esquecerOrigens();
    await roteador("/Departamentos/", "/departamentos/7/aulas");
    expect(voltarDaTurma(7, false).rotulo).toBe("Turmas EBD");
  });

  it("Minha frequência: aberta pelo Painel volta para o Painel; pelas outras telas, para Minhas Turmas", async () => {
    // Primeira navegação (endereço digitado ou recarregado): Minhas Turmas, como antes.
    const router = await roteador("/departamentos/7/minha-frequencia");
    expect(voltarDaMinhaFrequencia()).toEqual({ para: "/minhas-turmas", rotulo: "Minhas Turmas" });

    await router.push("/");
    await router.push("/departamentos/7/minha-frequencia");
    expect(voltarDaMinhaFrequencia()).toEqual({ para: "/", rotulo: "Painel" });

    await router.push("/minhas-turmas");
    await router.push("/departamentos/7/minha-frequencia");
    expect(voltarDaMinhaFrequencia()).toEqual({ para: "/minhas-turmas", rotulo: "Minhas Turmas" });
  });
});
