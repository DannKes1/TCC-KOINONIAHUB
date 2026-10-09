import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import LayoutPrincipal from "./LayoutPrincipal.vue";
import { usarAutenticacaoStore } from "../../aplicacao/armazenamentos/autenticacaoStore";

// Etapa 6.3 (responsividade): até 768 px a barra lateral sai do fluxo e o mesmo
// MenuLateral abre num Drawer pelo botão da BarraTopo. O jsdom não aplica media
// queries, então o spec cobre o comportamento (abrir, navegar, fechar) e não o CSS.
async function montar() {
  const pinia = createPinia();
  setActivePinia(pinia);

  const autenticacao = usarAutenticacaoStore();
  autenticacao.entrar({
    ExpiraEm: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    UsuarioId: 1,
    EmailUsuario: "professor@teste.com",
    Perfil: "Professor",
    IgrejaId: 1,
    PessoaId: 1,
  });

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: "/",
        component: LayoutPrincipal,
        children: [
          { path: "", component: { template: "<div>painel</div>" } },
          {
            path: "minhas-turmas",
            component: { template: "<div>turmas</div>" },
          },
        ],
      },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push("/");
  await router.isReady();

  const wrapper = mount(LayoutPrincipal, {
    attachTo: document.body,
    global: { plugins: [pinia, router, PrimeVue] },
  });
  await flushPromises();

  return { wrapper, router };
}

function linksDoDrawer() {
  return Array.from(
    document.querySelectorAll(
      '[data-testid="menu-drawer"] a.sidebar-link, .menu-drawer a.sidebar-link',
    ),
  ).map((a) => a.textContent?.trim());
}

describe("LayoutPrincipal — menu em Drawer (Etapa 6.3)", () => {
  beforeEach(() => {
    localStorage.clear();
    document.body.innerHTML = "";
  });

  it("mantém o menu lateral de desktop no DOM e o Drawer fechado", async () => {
    const { wrapper } = await montar();

    expect(wrapper.find("aside.sidebar-desktop").exists()).toBe(true);
    expect(wrapper.find('[data-testid="botao-menu"]').exists()).toBe(true);
    expect(linksDoDrawer()).toEqual([]);

    wrapper.unmount();
  });

  it("o botão da barra abre o Drawer com os itens do perfil", async () => {
    const { wrapper } = await montar();

    await wrapper.find('[data-testid="botao-menu"]').trigger("click");
    await flushPromises();

    // Professor: Painel, Minhas Turmas, Meus Dados, Relatórios EBD (ver MenuLateral.spec).
    const links = linksDoDrawer();
    expect(links).toContain("Painel");
    expect(links).toContain("Minhas Turmas");
    expect(links).toContain("Meus Dados");
    expect(links).not.toContain("Usuários");

    wrapper.unmount();
  });

  it("navegar fecha o Drawer", async () => {
    const { wrapper, router } = await montar();

    await wrapper.find('[data-testid="botao-menu"]').trigger("click");
    await flushPromises();
    expect(linksDoDrawer().length).toBeGreaterThan(0);

    await router.push("/minhas-turmas");
    await flushPromises();
    // O Drawer é desmontado ao fechar (PrimeVue renderiza só enquanto visível).
    expect(linksDoDrawer()).toEqual([]);

    wrapper.unmount();
  });
});
