import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaCadastroInicial from "./PaginaCadastroInicial.vue";
import { registrarAdminApi } from "../../aplicacao/servicos/authServico";

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

  // RNF 42.1 / CSU01 FA2 (aderência, Entrega A): o aviso de aceite obrigatório aparece
  // até marcar a caixa; depois o Concluir habilita e envia o que foi digitado.
  it("aviso de aceite até marcar; Concluir envia igreja, administrador e versão do termo", async () => {
    vi.mocked(registrarAdminApi).mockResolvedValueOnce({} as never);
    const wrapper = await montar();

    expect(wrapper.find('[data-testid="termo-aceite-obrigatorio"]').text()).toBe(
      "O aceite do termo é obrigatório para concluir.",
    );
    expect(wrapper.find('[data-testid="cadastro-concluir"]').attributes("disabled")).toBeDefined();

    await wrapper.find('[data-testid="cadastro-nome-igreja"]').setValue("Igreja Central");
    await wrapper.find('[data-testid="cadastro-cidade"]').setValue("Ji-Paraná");
    await wrapper.find('[data-testid="cadastro-estado"]').setValue("RO");
    await wrapper.find('[data-testid="cadastro-email-igreja"]').setValue("");
    await wrapper.find('[data-testid="cadastro-nome-admin"]').setValue("Daniel");
    await wrapper.find('[data-testid="cadastro-email-admin"]').setValue("daniel@teste.com");
    await wrapper.find('[data-testid="cadastro-senha-admin"]').setValue("segredo1");
    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    expect(wrapper.find('[data-testid="termo-aceite-obrigatorio"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="cadastro-concluir"]').attributes("disabled")).toBeUndefined();

    await wrapper.find('[data-testid="cadastro-concluir"]').trigger("click");
    await flushPromises();

    expect(registrarAdminApi).toHaveBeenCalledWith({
      Igreja: { Nome: "Igreja Central", Cidade: "Ji-Paraná", Estado: "RO", Email: null },
      EmailAdmin: "daniel@teste.com",
      SenhaAdmin: "segredo1",
      NomeAdmin: "Daniel",
      AceiteTermoVersao: "1.0",
    });

    wrapper.unmount();
  });
});
