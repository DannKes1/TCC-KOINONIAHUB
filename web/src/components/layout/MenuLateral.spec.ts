import { mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createMemoryHistory, createRouter } from "vue-router";

import MenuLateral from "./MenuLateral.vue";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

// Registro do projeto, 7.2, "Menu lateral por perfil": Professor não vê Pessoas nem Usuários;
// Usuario vê só Painel, Minhas Turmas e Meus Dados. Etapa 6.2: "Igreja" (RF44) só
// para o Admin.
async function montarComPerfil(perfil: string) {
  const pinia = createPinia();
  setActivePinia(pinia);

  const autenticacao = usarAutenticacaoStore();
  autenticacao.entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: `${perfil.toLowerCase()}@teste.com`,
    Perfil: perfil,
    IgrejaId: 1,
    PessoaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/:pathMatch(.*)*", component: { template: "<div />" } }],
  });
  await router.push("/");
  await router.isReady();

  return mount(MenuLateral, { global: { plugins: [pinia, router] } });
}

function itensDoMenu(wrapper: ReturnType<typeof mount>) {
  return wrapper
    .findAll("a.sidebar-link")
    .map((a) => a.text().trim());
}

describe("MenuLateral por perfil", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("Admin vê todos os itens, inclusive Pessoas, Usuários e Igreja", async () => {
    const itens = itensDoMenu(await montarComPerfil("Admin"));

    expect(itens).toEqual([
      "Painel",
      "Minhas Turmas",
      "Meus Dados",
      "Pessoas",
      "Relatórios EBD",
      "Usuários",
      "Igreja",
      "Turmas EBD",
    ]);
  });

  it("Pastor e Superintendente veem Pessoas, mas não Usuários nem Igreja", async () => {
    for (const perfil of ["Pastor", "Superintendente"]) {
      const itens = itensDoMenu(await montarComPerfil(perfil));

      expect(itens).toContain("Pessoas");
      expect(itens).not.toContain("Usuários");
      expect(itens).not.toContain("Igreja");
      expect(itens).toContain("Relatórios EBD");
      expect(itens).toContain("Turmas EBD");
    }
  });

  it("Os perfis administrativos continuam com a seção EBD e Turmas EBD", async () => {
    for (const perfil of ["Admin", "Pastor", "Superintendente"]) {
      const wrapper = await montarComPerfil(perfil);

      expect(wrapper.findAll(".sidebar-secao").map((s) => s.text())).toContain("EBD");
      expect(itensDoMenu(wrapper)).toContain("Turmas EBD");
    }
  });

  // Ajuste B1: o Professor não vê Turmas EBD nem o título de seção EBD; abre as turmas
  // dele por Minhas Turmas ou pelo Painel (CSU17, FA1).
  it("Professor não vê Pessoas, Usuários, Igreja nem Turmas EBD", async () => {
    const wrapper = await montarComPerfil("Professor");

    expect(itensDoMenu(wrapper)).toEqual([
      "Painel",
      "Minhas Turmas",
      "Meus Dados",
      "Relatórios EBD",
    ]);
    expect(wrapper.findAll(".sidebar-secao").map((s) => s.text())).not.toContain("EBD");
  });

  it("Usuario vê só Painel, Minhas Turmas e Meus Dados", async () => {
    const itens = itensDoMenu(await montarComPerfil("Usuario"));

    expect(itens).toEqual(["Painel", "Minhas Turmas", "Meus Dados"]);
  });
});
