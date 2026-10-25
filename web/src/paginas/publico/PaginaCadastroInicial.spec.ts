import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaCadastroInicial from "./PaginaCadastroInicial.vue";

// RF1 / CSU01 (correção do SonarCloud, Web:InputWithoutLabelCheck): cada campo do
// Cadastro Inicial tem um rótulo visível ligado ao input por for/id, e o placeholder
// continua como dica. Serviços da API simulados.
vi.mock("../../aplicacao/servicos/termoServico", () => ({
  obterTermoVigente: vi.fn(async () => ({
    versao: "1.0",
    vigenteDesde: "2026-10-01T00:00:00Z",
    texto: "TERMO DE USO E SIGILO DO KOINONIAHUB\n\nTexto de teste.",
    hash: "abc",
    igreja: null,
  })),
}));

vi.mock("../../aplicacao/servicos/authServico", () => ({
  registrarAdminApi: vi.fn(),
}));

const campos = [
  { id: "cadastro-nome-igreja", rotulo: "Nome da igreja *", placeholder: "Nome da Igreja" },
  { id: "cadastro-cidade", rotulo: "Cidade", placeholder: "Cidade (opcional)" },
  { id: "cadastro-estado", rotulo: "Estado", placeholder: "Estado (opcional)" },
  { id: "cadastro-email-igreja", rotulo: "E-mail da igreja", placeholder: "E-mail da igreja (opcional)" },
  { id: "cadastro-nome-admin", rotulo: "Nome do administrador *", placeholder: "Nome do Administrador" },
  { id: "cadastro-email-admin", rotulo: "E-mail do administrador *", placeholder: "E-mail do Administrador" },
  { id: "cadastro-senha-admin", rotulo: "Senha *", placeholder: "Senha" },
];

async function montar() {
  const pinia = createPinia();
  setActivePinia(pinia);

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/cadastro-inicial", component: PaginaCadastroInicial },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push("/cadastro-inicial");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    global: { plugins: [pinia, router, PrimeVue] },
  });
  await flushPromises();
  return wrapper;
}

describe("PaginaCadastroInicial (RF1, rótulos dos campos)", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("cada campo tem um <label for> ligado ao input pelo id, e o placeholder continua", async () => {
    const wrapper = await montar();

    for (const campo of campos) {
      const label = wrapper.find(`label[for="${campo.id}"]`);
      expect(label.exists(), `label de ${campo.id}`).toBe(true);
      expect(label.text()).toBe(campo.rotulo);

      const input = wrapper.find(`input#${campo.id}`);
      expect(input.exists(), `input de ${campo.id}`).toBe(true);
      expect(input.attributes("data-testid")).toBe(campo.id);
      expect(input.attributes("placeholder")).toBe(campo.placeholder);
    }

    // Um rótulo por campo, sem for apontando para id inexistente.
    const fors = wrapper
      .findAll("label[for]")
      .map((l) => l.attributes("for"))
      .filter((f) => f?.startsWith("cadastro-"));
    expect(fors).toEqual(campos.map((c) => c.id));

    wrapper.unmount();
  });
});
