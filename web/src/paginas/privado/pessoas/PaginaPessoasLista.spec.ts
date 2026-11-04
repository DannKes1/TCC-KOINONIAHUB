import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaPessoasLista from "./PaginaPessoasLista.vue";
import {
  atualizarPessoa,
  criarPessoa,
} from "../../../aplicacao/servicos/pessoasServico";

// Corretiva G3: o erro da API ao salvar Nova/Editar pessoa aparece dentro do diálogo —
// no topo quando a API devolve só a mensagem (400 `{ mensagem }`), junto ao campo quando
// devolve o campo (`errors` do ValidationProblemDetails) — nunca no topo da página, e
// some ao alterar um campo ou ao fechar o diálogo.
vi.mock("primevue/useconfirm", () => ({
  useConfirm: () => ({ require: vi.fn(), close: vi.fn() }),
}));

function pessoa() {
  return {
    id: 1,
    nome: "Ana Lima",
    situacao: "Ativo",
    email: "ana@teste.com",
    celular: "",
    dataNascimento: null,
    sexo: null,
    estadoCivil: null,
    endereco: "",
    bairro: "",
    cidade: "",
    estado: "",
    cep: "",
    criadoEm: "2026-01-01T00:00:00Z",
    dataInativacao: null,
    possuiConta: false,
  };
}

vi.mock("../../../aplicacao/servicos/pessoasServico", () => ({
  listarPessoas: vi.fn(async () => [pessoa()]),
  obterPessoa: vi.fn(async () => pessoa()),
  criarPessoa: vi.fn(),
  atualizarPessoa: vi.fn(),
  importarPessoas: vi.fn(),
}));

vi.mock("../../../aplicacao/servicos/parentescosServico", () => ({
  listarParentescosDaPessoa: vi.fn(async () => []),
  criarParentesco: vi.fn(),
  removerParentesco: vi.fn(),
}));

vi.mock("../../../aplicacao/servicos/presencasPessoaServico", () => ({
  listarHistoricoPresencasDaPessoa: vi.fn(async () => []),
}));

const EM_USO = "Este e-mail já está em uso por outra pessoa.";
const rejeicao = (data: unknown) => ({ response: { status: 400, data } });

// O PageHeader lê a rota; a página é montada dentro de um RouterView.
async function montar() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: "/pessoas", component: PaginaPessoasLista }],
  });
  await router.push("/pessoas");
  await router.isReady();

  const wrapper = mount({ template: "<RouterView />" }, {
    attachTo: document.body,
    global: { plugins: [router, PrimeVue], directives: { tooltip: Tooltip } },
  });
  await flushPromises();
  return wrapper;
}

function erroDoDialogo() {
  return document.querySelector('[data-testid="pessoa-dialogo-erro"]');
}

function clicar(testid: string) {
  document.querySelector<HTMLButtonElement>(`[data-testid="${testid}"]`)!.click();
}

function digitar(seletor: string, valor: string) {
  const campo = document.querySelector<HTMLInputElement>(seletor)!;
  campo.value = valor;
  campo.dispatchEvent(new Event("input"));
}

describe("PaginaPessoasLista — erro da API dentro do diálogo (Corretiva G3)", () => {
  // O Dropdown (Select) do PrimeVue consulta matchMedia; o jsdom não o implementa.
  beforeAll(() => {
    Object.defineProperty(window, "matchMedia", {
      configurable: true,
      writable: true,
      value: () => ({
        matches: false,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
      }),
    });
  });

  beforeEach(() => {
    document.body.innerHTML = "";
    vi.mocked(criarPessoa).mockReset();
    vi.mocked(atualizarPessoa).mockReset();
  });

  it("Nova pessoa: 400 com mensagem fica no topo do diálogo, some ao digitar e ao fechar", async () => {
    vi.mocked(criarPessoa).mockRejectedValue(rejeicao({ mensagem: EM_USO }));
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Nova Pessoa"]').trigger("click");
    await flushPromises();
    digitar("#pessoa-formulario-nome", "Bia Souza");
    await flushPromises();

    clicar("pessoa-dialogo-salvar");
    await flushPromises();
    expect(criarPessoa).toHaveBeenCalledTimes(1);
    expect(erroDoDialogo()!.textContent).toContain(EM_USO);
    // Nada no topo da página (o diálogo é teleportado para o body, fora do wrapper).
    expect(wrapper.text()).not.toContain(EM_USO);

    digitar("#pessoa-formulario-email", "outro@teste.com");
    await flushPromises();
    expect(erroDoDialogo()).toBeNull();

    clicar("pessoa-dialogo-salvar");
    await flushPromises();
    expect(erroDoDialogo()).not.toBeNull();

    clicar("pessoa-dialogo-cancelar");
    await flushPromises();
    await wrapper.find('button[aria-label="Nova Pessoa"]').trigger("click");
    await flushPromises();
    expect(erroDoDialogo()).toBeNull();

    wrapper.unmount();
  });

  it("Editar pessoa: 400 por campo aparece junto ao campo, sem aviso no topo do diálogo", async () => {
    vi.mocked(atualizarPessoa).mockRejectedValue(
      rejeicao({ errors: { Celular: ["Celular inválido. Informe DDD + número."] } }),
    );
    const wrapper = await montar();

    const editar = wrapper.find("tbody .pi-pencil").element.closest("button")!;
    editar.click();
    await flushPromises();
    expect(document.querySelector<HTMLInputElement>("#pessoa-formulario-nome")!.value).toBe("Ana Lima");

    clicar("pessoa-dialogo-salvar");
    await flushPromises();
    expect(atualizarPessoa).toHaveBeenCalledTimes(1);
    expect(document.querySelector('[data-testid="pessoa-celular-erro"]')!.textContent).toContain(
      "Celular inválido",
    );
    expect(erroDoDialogo()).toBeNull();
    expect(wrapper.text()).not.toContain("Alguns campos enviados são inválidos");

    wrapper.unmount();
  });
});
