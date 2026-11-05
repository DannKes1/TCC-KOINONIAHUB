import { flushPromises, mount } from "@vue/test-utils";
import PrimeVue from "primevue/config";
import Dropdown from "primevue/dropdown";
import Tooltip from "primevue/tooltip";
import { createMemoryHistory, createRouter } from "vue-router";

import PaginaPessoasLista from "./PaginaPessoasLista.vue";
import {
  atualizarPessoa,
  criarPessoa,
  listarPessoas,
  obterPessoa,
} from "../../../aplicacao/servicos/pessoasServico";
import { criarParentesco } from "../../../aplicacao/servicos/parentescosServico";

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
    atualizadoEm: null,
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

  // Corretiva G4: a validação local fica junto ao campo, sem toast.
  it("Nova pessoa sem nome: erro junto ao campo e a API não é chamada", async () => {
    const wrapper = await montar();

    await wrapper.find('button[aria-label="Nova Pessoa"]').trigger("click");
    await flushPromises();
    clicar("pessoa-dialogo-salvar");
    await flushPromises();

    expect(document.querySelector('[data-testid="pessoa-nome-erro"]')!.textContent).toContain("Informe o nome da pessoa.");
    expect(criarPessoa).not.toHaveBeenCalled();

    wrapper.unmount();
  });

  // Corretiva G4: o diálogo de Parentescos tem a própria instância para o Adicionar: o
  // erro de regra aparece dentro dele, nunca no topo da página, e some ao trocar o campo.
  it("Parentescos: parente obrigatório junto ao campo; 400 da API no topo do diálogo, nada na página", async () => {
    const JA_REGISTRADO = "Este vínculo já está registrado.";
    vi.mocked(listarPessoas).mockResolvedValueOnce([
      pessoa(),
      { ...pessoa(), id: 2, nome: "Beto Lima", email: null },
    ]);
    vi.mocked(criarParentesco).mockRejectedValue(rejeicao({ mensagem: JA_REGISTRADO }));
    const wrapper = await montar();

    wrapper.find("tbody .pi-share-alt").element.closest("button")!.click();
    await flushPromises();

    clicar("parentesco-dialogo-salvar");
    await flushPromises();
    expect(criarParentesco).not.toHaveBeenCalled();
    expect(document.querySelector('[data-testid="parentesco-parente-erro"]')!.textContent).toContain("Selecione o parente.");

    const parente = wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "pessoa-parentesco-parente");
    parente!.vm.$emit("update:modelValue", 2);
    const relacionamento = wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "pessoa-parentesco-relacionamento");
    relacionamento!.vm.$emit("update:modelValue", "Pai");
    await flushPromises();
    expect(document.querySelector('[data-testid="parentesco-parente-erro"]')).toBeNull();

    clicar("parentesco-dialogo-salvar");
    await flushPromises();
    expect(criarParentesco).toHaveBeenCalledTimes(1);
    expect(document.querySelector('[data-testid="parentesco-dialogo-erro"]')!.textContent).toContain(JA_REGISTRADO);
    expect(wrapper.text()).not.toContain(JA_REGISTRADO);

    relacionamento!.vm.$emit("update:modelValue", "Mãe");
    await flushPromises();
    expect(document.querySelector('[data-testid="parentesco-dialogo-erro"]')).toBeNull();

    wrapper.unmount();
  });
  // Corretiva G5: CEP gravado antes da máscara (10 dígitos). O InputMask mostra
  // "77777-777"; o formulário passa a guardar o mesmo valor, então a validação local não
  // barra o envio e a guarda da API (pessoa do único Admin) aparece no topo do diálogo.
  it("Editar pessoa com CEP antigo: envia o CEP mostrado e exibe a guarda do único Admin no topo", async () => {
    const UNICO_ADMIN = "Não é possível inativar a pessoa do único administrador ativo da igreja.";
    vi.mocked(obterPessoa).mockResolvedValueOnce({
      ...pessoa(),
      nome: "Daniel ADMIN",
      cep: "7777777777",
      possuiConta: true,
    });
    vi.mocked(atualizarPessoa).mockRejectedValue(rejeicao({ mensagem: UNICO_ADMIN }));
    const wrapper = await montar();

    clicar("pessoa-editar");
    await flushPromises();
    expect(document.querySelector<HTMLInputElement>("#pessoa-formulario-cep")!.value).toBe("77777-777");

    const situacao = wrapper.findAllComponents(Dropdown).find((d) => d.props("labelId") === "pessoa-formulario-situacao");
    situacao!.vm.$emit("update:modelValue", "Inativo");
    await flushPromises();

    clicar("pessoa-dialogo-salvar");
    await flushPromises();
    expect(atualizarPessoa).toHaveBeenCalledTimes(1);
    expect(vi.mocked(atualizarPessoa).mock.calls[0]?.[1]).toMatchObject({ CEP: "77777-777", Situacao: "Inativo" });
    expect(document.querySelector('[data-testid="pessoa-cep-erro"]')).toBeNull();
    expect(erroDoDialogo()!.textContent).toContain(UNICO_ADMIN);
    expect(wrapper.text()).not.toContain(UNICO_ADMIN);

    wrapper.unmount();
  });
});
