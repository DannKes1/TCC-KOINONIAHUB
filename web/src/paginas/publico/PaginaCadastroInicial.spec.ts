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

// Bloco do formulário (rótulo, input e mensagem) onde fica o input do campo.
function blocoDoCampo(wrapper: Awaited<ReturnType<typeof montar>>, id: string): Element {
  return wrapper.find(`input#${id}`).element.closest(".login-campo")!;
}

describe("PaginaCadastroInicial (RF1, rótulos dos campos)", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.mocked(registrarAdminApi).mockReset();
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

  // CSU01 FA1 (dados inválidos): a validação local mostra a mensagem junto a cada campo, no
  // mesmo bloco do input e uma única vez na tela, sem chamar a API; a linha acima do botão
  // pede a correção, tudo some ao alterar um campo, e o formulário corrigido é reenviado.
  it("campos obrigatórios vazios e senha curta: mensagem junto a cada campo sem chamar a API; corrigido, o formulário é reenviado", async () => {
    const wrapper = await montar();

    await wrapper.find('[data-testid="cadastro-cidade"]').setValue("Ji-Paraná");
    await wrapper.find('[data-testid="cadastro-senha-admin"]').setValue("abc");
    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    await wrapper.find('[data-testid="cadastro-concluir"]').trigger("click");
    await flushPromises();

    const esperados: [string, string][] = [
      ["cadastro-nome-igreja", "Informe o nome da igreja."],
      ["cadastro-nome-admin", "Informe o nome do administrador."],
      ["cadastro-email-admin", "Informe o e-mail do administrador."],
      ["cadastro-senha-admin", "A senha deve ter pelo menos 6 caracteres."],
    ];
    for (const [campo, mensagem] of esperados) {
      const erroNoBloco = blocoDoCampo(wrapper, campo).querySelector(`[data-testid="${campo}-erro"]`);
      expect(erroNoBloco?.textContent?.trim(), `erro de ${campo}`).toBe(mensagem);
      expect(wrapper.text().split(mensagem), `uma vez: ${mensagem}`).toHaveLength(2);
    }
    expect(wrapper.find('[data-testid="cadastro-cidade-erro"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="cadastro-erro"]').text()).toBe("Corrija os campos destacados.");
    expect(registrarAdminApi).not.toHaveBeenCalled();

    await wrapper.find('[data-testid="cadastro-nome-igreja"]').setValue("Igreja Central");
    await flushPromises();

    expect(wrapper.find('[data-testid="cadastro-nome-igreja-erro"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="cadastro-senha-admin-erro"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="cadastro-erro"]').exists()).toBe(false);

    await wrapper.find('[data-testid="cadastro-nome-admin"]').setValue("Daniel");
    await wrapper.find('[data-testid="cadastro-email-admin"]').setValue("daniel@teste.com");
    await wrapper.find('[data-testid="cadastro-senha-admin"]').setValue("segredo1");
    await flushPromises();
    await wrapper.find('[data-testid="cadastro-concluir"]').trigger("click");
    await flushPromises();

    expect(registrarAdminApi).toHaveBeenCalledTimes(1);
    expect(registrarAdminApi).toHaveBeenCalledWith({
      Igreja: { Nome: "Igreja Central", Cidade: "Ji-Paraná", Estado: null, Email: null },
      EmailAdmin: "daniel@teste.com",
      SenhaAdmin: "segredo1",
      NomeAdmin: "Daniel",
      AceiteTermoVersao: "1.0",
    });

    wrapper.unmount();
  });

  // CSU01 FA1, erro da API (400 por campo, com a chave do DTO aninhado): a mensagem fica junto
  // ao campo, e a linha acima do botão continua visível, porque o campo pode estar fora da
  // janela de quem clicou em Concluir.
  it("400 da API por campo: mensagem junto ao e-mail da igreja e linha geral acima do botão", async () => {
    vi.mocked(registrarAdminApi).mockRejectedValueOnce({
      response: { status: 400, data: { errors: { "Igreja.Email": ["Informe um e-mail válido."] } } },
    });
    const wrapper = await montar();

    await wrapper.find('[data-testid="cadastro-nome-igreja"]').setValue("Igreja Central");
    await wrapper.find('[data-testid="cadastro-email-igreja"]').setValue("contato@");
    await wrapper.find('[data-testid="cadastro-nome-admin"]').setValue("Daniel");
    await wrapper.find('[data-testid="cadastro-email-admin"]').setValue("daniel@teste.com");
    await wrapper.find('[data-testid="cadastro-senha-admin"]').setValue("segredo1");
    await wrapper.find('input[type="checkbox"]').setValue(true);
    await flushPromises();

    await wrapper.find('[data-testid="cadastro-concluir"]').trigger("click");
    await flushPromises();

    expect(registrarAdminApi).toHaveBeenCalledTimes(1);
    const erroNoBloco = blocoDoCampo(wrapper, "cadastro-email-igreja").querySelector(
      '[data-testid="cadastro-email-igreja-erro"]',
    );
    expect(erroNoBloco?.textContent?.trim()).toBe("Informe um e-mail válido.");
    expect(wrapper.text().split("Informe um e-mail válido.")).toHaveLength(2);
    expect(wrapper.find('[data-testid="cadastro-erro"]').text()).toBe(
      "Alguns campos enviados são inválidos. Verifique os dados e tente novamente.",
    );

    await wrapper.find('[data-testid="cadastro-email-igreja"]').setValue("contato@igreja.org");
    await flushPromises();

    expect(wrapper.find('[data-testid="cadastro-email-igreja-erro"]').exists()).toBe(false);
    expect(wrapper.find('[data-testid="cadastro-erro"]').exists()).toBe(false);

    wrapper.unmount();
  });
});
