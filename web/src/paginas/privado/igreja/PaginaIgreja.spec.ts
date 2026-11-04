import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaIgreja from "./PaginaIgreja.vue";
import { atualizarIgreja } from "../../../aplicacao/servicos/igrejasServico";

// Corretiva G4 (RF44): formulário de página no mesmo padrão dos diálogos — o erro de regra
// da API aparece no topo do formulário (uma única vez na tela) e some ao alterar um
// campo; a validação local fica junto ao campo, sem toast.
vi.mock("../../../aplicacao/armazenamentos/autenticacaoStore", () => ({
  usarAutenticacaoStore: () => ({ igrejaId: 1, perfil: "Admin", isAdministrativo: true }),
}));

vi.mock("../../../aplicacao/servicos/igrejasServico", () => ({
  obterIgreja: vi.fn(async () => ({
    id: 1,
    nome: "Igreja de Teste",
    cidade: "Ji-Paraná",
    estado: "RO",
    email: "contato@teste.org",
    telefone: "69 99999-0000",
    atualizadoEm: null,
  })),
  atualizarIgreja: vi.fn(),
}));

vi.mock("../../../aplicacao/servicos/notificacoes", () => ({
  toastSuccess: vi.fn(),
  toastWarn: vi.fn(),
}));

const RECUSA = "Os dados da igreja não puderam ser alterados.";

async function montar() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/igreja", component: PaginaIgreja }],
  });
  await router.push("/igreja");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return wrapper;
}

function digitar(seletor: string, valor: string) {
  const campo = document.querySelector<HTMLInputElement>(seletor)!;
  campo.value = valor;
  campo.dispatchEvent(new Event("input"));
}

function salvar() {
  document.querySelector<HTMLButtonElement>('[data-testid="igreja-salvar"]')!.click();
}

describe("PaginaIgreja — erro no topo do formulário (Corretiva G4)", () => {
  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(atualizarIgreja).mockReset();
  });

  it("400 com mensagem: aviso no topo do formulário, uma única vez na tela, some ao alterar um campo", async () => {
    vi.mocked(atualizarIgreja).mockRejectedValue({ response: { status: 400, data: { mensagem: RECUSA } } });
    const wrapper = await montar();

    digitar('[data-testid="igreja-cidade"]', "Porto Velho");
    await flushPromises();
    salvar();
    await flushPromises();

    expect(atualizarIgreja).toHaveBeenCalledTimes(1);
    expect(wrapper.find('[data-testid="igreja-formulario-erro"]').text()).toContain(RECUSA);
    expect(wrapper.text().split(RECUSA)).toHaveLength(2);

    digitar('[data-testid="igreja-cidade"]', "Ariquemes");
    await flushPromises();
    expect(wrapper.find('[data-testid="igreja-formulario-erro"]').exists()).toBe(false);

    wrapper.unmount();
  });

  it("nome vazio: erro junto ao campo e a API não é chamada", async () => {
    const wrapper = await montar();

    digitar('[data-testid="igreja-nome"]', "");
    await flushPromises();
    salvar();
    await flushPromises();

    expect(wrapper.find('[data-testid="igreja-nome-erro"]').text()).toContain("Informe o nome da igreja.");
    expect(atualizarIgreja).not.toHaveBeenCalled();

    wrapper.unmount();
  });
});
