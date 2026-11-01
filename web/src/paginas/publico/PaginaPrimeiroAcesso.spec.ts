import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import PrimeVue from "primevue/config";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaPrimeiroAcesso from "./PaginaPrimeiroAcesso.vue";
import {
  ativarPrimeiroAcessoApi,
  validarPrimeiroAcessoApi,
} from "../../aplicacao/servicos/authServico";

// RF40 / RNF 42.1 / CSU22 (aderência, Entrega A): o primeiro acesso valida o convite,
// exibe o termo com o aviso de aceite obrigatório enquanto a caixa está desmarcada e só
// conclui com senha confirmada e termo aceito.
vi.mock("../../aplicacao/servicos/authServico", () => ({
  validarPrimeiroAcessoApi: vi.fn(async () => ({ email: "maria@teste.com", nomePessoa: "Maria Souza" })),
  ativarPrimeiroAcessoApi: vi.fn(async () => ({})),
}));

vi.mock("../../aplicacao/servicos/termoServico", () => ({
  obterTermoVigente: vi.fn(async () => ({
    versao: "1.0",
    vigenteDesde: "2026-10-01T00:00:00Z",
    texto: "TERMO DE USO E SIGILO DO KOINONIAHUB\n\nTexto de teste.",
    hash: "abc",
    igreja: { nome: "Igreja de Teste", email: null, telefone: null },
  })),
}));

async function montar(consulta: string) {
  const pinia = createPinia();
  setActivePinia(pinia);

  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: "/primeiro-acesso", component: PaginaPrimeiroAcesso },
      { path: "/:pathMatch(.*)*", component: { template: "<div />" } },
    ],
  });
  await router.push(`/primeiro-acesso${consulta}`);
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    global: { plugins: [pinia, router, PrimeVue] },
  });
  await flushPromises();
  return wrapper;
}

describe("PaginaPrimeiroAcesso — convite, termo e senha (RF40, RNF 42.1)", () => {
  beforeEach(() => {
    vi.mocked(validarPrimeiroAcessoApi).mockClear();
    vi.mocked(ativarPrimeiroAcessoApi).mockClear();
  });

  it("sem token: informa link incompleto e não consulta a API", async () => {
    const wrapper = await montar("");

    expect(wrapper.text()).toContain("Link incompleto. Abra exatamente o link de convite que você recebeu.");
    expect(validarPrimeiroAcessoApi).not.toHaveBeenCalled();

    wrapper.unmount();
  });

  it("com token: mostra o e-mail do convite, o termo e o aviso de aceite obrigatório até marcar", async () => {
    const wrapper = await montar("?token=abc");

    expect(validarPrimeiroAcessoApi).toHaveBeenCalledWith("abc");
    expect((wrapper.find("input#primeiro-acesso-email").element as HTMLInputElement).value).toBe("maria@teste.com");
    expect(wrapper.find('[data-testid="termo-aceite-obrigatorio"]').text()).toBe(
      "O aceite do termo é obrigatório para concluir.",
    );
    expect(wrapper.find('[data-testid="primeiro-acesso-concluir"]').attributes("disabled")).toBeDefined();

    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    expect(wrapper.find('[data-testid="termo-aceite-obrigatorio"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="primeiro-acesso-concluir"]').attributes("disabled")).toBeUndefined();

    wrapper.unmount();
  });

  it("confirmação diferente é recusada; com senha confirmada envia token, senha e versão do termo", async () => {
    const wrapper = await montar("?token=abc");
    await wrapper.find('input[type="checkbox"]').setValue(true);

    await wrapper.find("input#primeiro-acesso-senha").setValue("segredo1");
    await wrapper.find("input#primeiro-acesso-confirmar").setValue("segredo2");
    await wrapper.find('[data-testid="primeiro-acesso-concluir"]').trigger("click");
    await flushPromises();

    expect(ativarPrimeiroAcessoApi).not.toHaveBeenCalled();
    expect(wrapper.text()).toContain("A confirmação da senha não confere.");

    await wrapper.find("input#primeiro-acesso-confirmar").setValue("segredo1");
    await wrapper.find('[data-testid="primeiro-acesso-concluir"]').trigger("click");
    await flushPromises();

    expect(ativarPrimeiroAcessoApi).toHaveBeenCalledWith({
      Token: "abc",
      NovaSenha: "segredo1",
      AceiteTermoVersao: "1.0",
    });
    expect(wrapper.find('[data-testid="primeiro-acesso-sucesso"]').exists()).toBe(true);

    wrapper.unmount();
  });
});
